using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.Database.Friend;
using EggLink.DanhengServer.Enums.Mission;
using EggLink.DanhengServer.GameServer.Game.Battle;
using EggLink.DanhengServer.GameServer.Game.Challenge.Definitions;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Game.Scene.Entity;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Lineup;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Challenge.Instances;

public class ChallengePeakInstance(PlayerInstance player, ChallengeDataPb data) : BaseChallengeInstance(player, data)
{
    #region Setter & Getter

    public override Dictionary<int, List<ChallengeConfigExcel.ChallengeMonsterInfo>> GetStageMonsters()
    {
        if (!Data.Peak.IsHard || Config.BossExcel == null) return Config.ChallengeMonsters;

        Dictionary<int, List<ChallengeConfigExcel.ChallengeMonsterInfo>> monsters = [];

        var groupId = (int)GameConstants.CHALLENGE_PEAK_TARGET_ENTRY_ID[GameConstants.CHALLENGE_PEAK_CUR_GROUP_ID][1];
        monsters.Add(groupId, []);


        var curConfId = 200000;
        foreach (var eventId in Config.BossExcel.HardEventIDList)
        {
            // get from stage id
            if (!GameData.StageConfigData.TryGetValue(eventId, out var stage)) continue;

            var monsterId = stage.MonsterList.LastOrDefault()?.Monster0 ?? 0;
            if (!GameData.MonsterConfigData.TryGetValue(monsterId, out var monsterConf)) continue;
            if (!GameData.MonsterTemplateConfigData.TryGetValue(monsterConf.MonsterTemplateID, out var template)) continue;

            var npcMonsterId = template.NPCMonsterList.Take(2).LastOrDefault(0);
            if (!GameData.NpcMonsterDataData.ContainsKey(npcMonsterId)) continue;

            monsters[groupId].Add(new ChallengeConfigExcel.ChallengeMonsterInfo(++curConfId, npcMonsterId,
                    eventId));
        }

        return monsters;
    }

    #endregion

    #region Properties

    public ChallengePeakConfigExcel Config { get; } =
        GameData.ChallengePeakConfigData[(int)data.Peak.CurrentPeakLevelId];

    public List<int> AllBattleTargets { get; } = [];
    public bool IsWin { get; private set; }

    #endregion

    //#region Serialization

    //#endregion

    #region Handlers

    public override void OnBattleStart(BattleInstance battle)
    {
        base.OnBattleStart(battle);

        foreach (var peakBuff in Data.Peak.Buffs)
            battle.Buffs.Add(new MazeBuff((int)peakBuff, 1, -1)
            {
                WaveFlag = -1
            });

        if (Data.Peak.IsHard && Config.BossExcel != null)
        {
            var excel = GameData.BattleTargetConfigData.GetValueOrDefault(Config.BossExcel.HardTarget);
            if (excel != null)
            {
                battle.AddBattleTarget(5, excel.ID, 0, excel.TargetParam);
                AllBattleTargets.Add(excel.ID);
            }
        }
        else
        {
            foreach (var targetId in Config.NormalTargetList)
            {
                var excel = GameData.BattleTargetConfigData.GetValueOrDefault(targetId);
                if (excel != null)
                {
                    battle.AddBattleTarget(5, excel.ID, 0, excel.TargetParam);
                    AllBattleTargets.Add(excel.ID);
                }
            }
        }
    }

    public override async ValueTask OnBattleEnd(BattleInstance battle, PVEBattleResultCsReq req)
    {
        switch (req.EndStatus)
        {
            case BattleEndStatus.BattleEndWin:
                // Get monster count in stage
                long monsters = Player.SceneInstance!.Entities.Values.OfType<EntityMonster>().Count();

                if (monsters == 0)
                {
                    Data.Peak.CurStatus = (int)ChallengeStatus.ChallengeFinish;
                    var res = CalculateStars(req);
                    Data.Peak.Stars = res.Item1;
                    Data.Peak.RoundCnt = req.Stt.RoundCnt;
                    IsWin = true;

                    await Player.SendPacket(new PacketChallengePeakSettleScNotify(this, res.Item2));

                    // Call MissionManager
                    await Player.MissionManager!.HandleFinishType(MissionFinishTypeEnum.ChallengePeakBattleFinish,
                        this);

                    await Player.ChallengePeakManager!.SaveHistory(this, res.Item2);

                    // add development
                    Player.FriendRecordData!.AddAndRemoveOld(new FriendDevelopmentInfoPb
                    {
                        DevelopmentType = DevelopmentType.DevelopmentChallengePeak,
                        Params = { { "PeakLevelId", (uint)Config.ID } }
                    });
                }

                // Set saved technique points (This will be restored if the player resets the challenge)
                Data.Peak.SavedMp = (uint)Player.LineupManager!.GetCurLineup()!.Mp;
                break;
            case BattleEndStatus.BattleEndQuit:
                // Reset technique points and move back to start position
                var lineup = Player.LineupManager!.GetCurLineup()!;
                lineup.Mp = (int)Data.Peak.SavedMp;
                if (Data.Peak.StartPos != null && Data.Peak.StartRot != null)
                    await Player.MoveTo(Data.Peak.StartPos.ToPosition(), Data.Peak.StartRot.ToPosition());
                await Player.SendPacket(new PacketSyncLineupNotify(lineup));
                break;
            default:
                // Determine challenge result
                // Fail challenge
                Data.Peak.CurStatus = (int)ChallengeStatus.ChallengeFailed;

                // Send challenge result data
                await Player.SendPacket(new PacketChallengePeakSettleScNotify(this, []));

                break;
        }
    }

    public (uint, List<uint>) CalculateStars(PVEBattleResultCsReq req)
    {
        var targets = AllBattleTargets;
        var stars = 0u;

        List<uint> finishedIds = [];
        foreach (var targetId in targets)
        {
            var target = req.Stt.BattleTargetInfo[5].BattleTargetList_.FirstOrDefault(x => x.Id == targetId);
            if (target == null) continue;
            var excel = GameData.BattleTargetConfigData.GetValueOrDefault(targetId);
            if (excel == null) continue;

            if (target.Progress <= excel.TargetParam)
            {
                stars += 1u;
                finishedIds.Add((uint)targetId);
            }
        }

        if (Data.Peak.IsHard && Config.BossExcel != null) stars = 3;

        return (Math.Min(stars, 3), finishedIds);
    }

    #endregion
}