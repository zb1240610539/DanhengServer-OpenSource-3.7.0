using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.Database.Friend;
using EggLink.DanhengServer.Enums.Mission;
using EggLink.DanhengServer.GameServer.Game.Battle;
using EggLink.DanhengServer.GameServer.Game.Challenge.Definitions;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Game.Scene.Entity;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Challenge;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Lineup;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Challenge.Instances;

public class ChallengeStoryInstance(PlayerInstance player, ChallengeDataPb data)
    : BaseLegacyChallengeInstance(player, data)
{
    #region Properties

    public override ChallengeConfigExcel Config { get; } =
        GameData.ChallengeConfigData[(int)data.Story.ChallengeMazeId];

    #endregion

    #region Serialization

    public override CurChallenge ToProto()
    {
        return new CurChallenge
        {
            ChallengeId = Data.Story.ChallengeMazeId,
            ExtraLineupType = (ExtraLineupType)Data.Story.CurrentExtraLineup,
            Status = (ChallengeStatus)Data.Story.CurStatus,
            StageInfo = new ChallengeCurBuffInfo
            {
                CurStoryBuffs = new ChallengeStoryBuffList
                {
                    BuffList = { Data.Story.Buffs }
                }
            },
            RoundCount = (uint)Config.ChallengeCountDown,
            ScoreId = Data.Story.ScoreStage1,
            ScoreTwo = Data.Story.ScoreStage2
        };
    }

    #endregion

    #region Setter & Getter

    public override uint GetStars()
    {
        return Data.Story.Stars;
    }

    public override uint GetScore1()
    {
        return Data.Story.ScoreStage1;
    }

    public override uint GetScore2()
    {
        return Data.Story.ScoreStage2;
    }

    public void SetCurrentExtraLineup(ExtraLineupType type)
    {
        Data.Story.CurrentExtraLineup = (ChallengeLineupTypePb)type;
    }

    public int GetTotalScore()
    {
        return (int)(Data.Story.ScoreStage1 + Data.Story.ScoreStage2);
    }

    public override int GetCurrentExtraLineupType()
    {
        return (int)Data.Story.CurrentExtraLineup;
    }

    public override void SetStartPos(Position pos)
    {
        Data.Story.StartPos = pos.ToVector3Pb();
    }

    public override void SetStartRot(Position rot)
    {
        Data.Story.StartRot = rot.ToVector3Pb();
    }

    public override void SetSavedMp(int mp)
    {
        Data.Story.SavedMp = (uint)mp;
    }

    public override Dictionary<int, List<ChallengeConfigExcel.ChallengeMonsterInfo>> GetStageMonsters()
    {
        return Data.Story.CurrentStage == 1
            ? Config.ChallengeMonsters1
            : Config.ChallengeMonsters2;
    }

    #endregion

    #region Handlers

    public override void OnBattleStart(BattleInstance battle)
    {
        base.OnBattleStart(battle);

        battle.RoundLimit = Config.ChallengeCountDown;

        battle.Buffs.Add(new MazeBuff(Config.MazeBuffID, 1, -1)
        {
            WaveFlag = -1
        });

        if (Config.StoryExcel == null) return;
        battle.AddBattleTarget(1, 10002, GetTotalScore());

        foreach (var id in Config.StoryExcel.BattleTargetID!) battle.AddBattleTarget(5, id, GetTotalScore());

        if (Data.Story.Buffs.Count < Data.Story.CurrentStage) return;
        var buffId = Data.Story.Buffs[(int)(Data.Story.CurrentStage - 1)];
        battle.Buffs.Add(new MazeBuff((int)buffId, 1, -1)
        {
            WaveFlag = -1
        });
    }

    public override async ValueTask OnBattleEnd(BattleInstance battle, PVEBattleResultCsReq req)
    {
        // Calculate score for current stage
        var stageScore = (int)req.Stt.ChallengeScore - GetTotalScore();

        // Set score
        if (Data.Story.CurrentStage == 1)
            Data.Story.ScoreStage1 = (uint)stageScore;
        else
            Data.Story.ScoreStage2 = (uint)stageScore;

        switch (req.EndStatus)
        {
            case BattleEndStatus.BattleEndWin:
                // Get monster count in stage
                long monsters = Player.SceneInstance!.Entities.Values.OfType<EntityMonster>().Count();

                if (monsters == 0) await AdvanceStage();

                // Set saved technique points (This will be restored if the player resets the challenge)
                Data.Story.SavedMp = (uint)Player.LineupManager!.GetCurLineup()!.Mp;
                break;
            case BattleEndStatus.BattleEndQuit:
                // Reset technique points and move back to start position
                var lineup = Player.LineupManager!.GetCurLineup()!;
                lineup.Mp = (int)Data.Story.SavedMp;
                await Player.MoveTo(Data.Story.StartPos.ToPosition(), Data.Story.StartRot.ToPosition());
                await Player.SendPacket(new PacketSyncLineupNotify(lineup));
                break;
            default:
                // Determine challenge result
                if (req.Stt.EndReason == BattleEndReason.TurnLimit)
                {
                    await AdvanceStage();
                }
                else
                {
                    // Fail challenge
                    Data.Story.CurStatus = (int)ChallengeStatus.ChallengeFailed;

                    // Send challenge result data
                    await Player.SendPacket(new PacketChallengeSettleNotify(this));
                }

                break;
        }
    }

    public uint CalculateStars()
    {
        var targets = Config.ChallengeTargetID!;
        var stars = 0u;

        for (var i = 0; i < targets.Count; i++)
        {
            if (!GameData.ChallengeTargetData.ContainsKey(targets[i])) continue;

            var target = GameData.ChallengeTargetData[targets[i]];

            switch (target.ChallengeTargetType)
            {
                case ChallengeTargetExcel.ChallengeType.TOTAL_SCORE:
                    if (GetTotalScore() >= target.ChallengeTargetParam1) stars += 1u << i;
                    break;
            }
        }

        return Math.Min(stars, 7);
    }

    private async ValueTask AdvanceStage()
    {
        if (Data.Story.CurrentStage >= Config.StageNum)
        {
            // Last stage
            Data.Story.CurStatus = (int)ChallengeStatus.ChallengeFinish;
            Data.Story.Stars = CalculateStars();

            // Save history
            Player.ChallengeManager!.AddHistory((int)Data.Story.ChallengeMazeId, (int)GetStars(), GetTotalScore());

            // Send challenge result data
            await Player.SendPacket(new PacketChallengeSettleNotify(this));

            // Call MissionManager
            await Player.MissionManager!.HandleFinishType(MissionFinishTypeEnum.ChallengeFinish, this);

            // save
            Player.ChallengeManager.SaveBattleRecord(this);

            // add development
            Player.FriendRecordData!.AddAndRemoveOld(new FriendDevelopmentInfoPb
            {
                DevelopmentType = DevelopmentType.DevelopmentStoryChallenge,
                Params = { { "ChallengeId", (uint)Config.ID } }
            });
        }
        else
        {
            // Increment and reset stage
            Data.Story.CurrentStage++;
            // Unload scene group for stage 1
            await Player.SceneInstance!.EntityLoader!.UnloadGroup(Config.MazeGroupID1);

            // Load scene group for stage 2
            await Player.SceneInstance!.EntityLoader!.LoadGroup(Config.MazeGroupID2);

            // Change player line up
            SetCurrentExtraLineup(ExtraLineupType.LineupChallenge2);
            await Player.LineupManager!.SetExtraLineup((ExtraLineupType)GetCurrentExtraLineupType());
            await Player.SendPacket(new PacketChallengeLineupNotify((ExtraLineupType)Data.Story.CurrentExtraLineup));
            await Player.SceneInstance!.SyncLineup();

            Data.Story.SavedMp = (uint)Player.LineupManager.GetCurLineup()!.Mp;

            // Move player
            if (Config.MapEntranceID2 != 0 && Config.MapEntranceID2 != Config.MapEntranceID)
            {
                await Player.EnterScene(Config.MapEntranceID2, 0, true);
                Data.Story.StartPos = Player.Data.Pos!.ToVector3Pb();
                Data.Story.StartRot = Player.Data.Rot!.ToVector3Pb();
                await Player.SceneInstance!.EntityLoader!.UnloadGroup(Config.MazeGroupID1);
                await Player.SceneInstance!.EntityLoader!.LoadGroup(Config.MazeGroupID2);
            }
            else
            {
                await Player.MoveTo(Data.Story.StartPos.ToPosition(), Data.Story.StartRot.ToPosition());
            }

            Player.ChallengeManager!.SaveInstance(this);
        }
    }

    #endregion
}