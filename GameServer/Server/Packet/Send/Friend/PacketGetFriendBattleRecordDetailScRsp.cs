using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database.Avatar;
using EggLink.DanhengServer.Database.Challenge;
using EggLink.DanhengServer.Database.Friend;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;

public class PacketGetFriendBattleRecordDetailScRsp : BasePacket
{
    public PacketGetFriendBattleRecordDetailScRsp(FriendRecordData recordData, ChallengeData challengeData,
        AvatarData avatarData) : base(
        CmdIds.GetFriendBattleRecordDetailScRsp)
    {
        var proto = new GetFriendBattleRecordDetailScRsp
        {
            Uid = (uint)recordData.Uid,
            ChallengeRecord = { recordData.ChallengeGroupStatistics.Values.Select(x => x.ToProto()) },
            RogueRecord = new RogueStatistics()
        };

        var data = GameData.ChallengePeakGroupConfigData.GetValueOrDefault((int)GameConstants
            .CHALLENGE_PEAK_CUR_GROUP_ID);

        if (data != null)
        {
            var peakRec = new ChallengePeakGroupStatistics
            {
                GroupId = GameConstants.CHALLENGE_PEAK_CUR_GROUP_ID,
                BossLevelStt = new BossLevelStatistics()
            };

            foreach (var preId in data.PreLevelIDList)
            {
                var stt = new PreLevelStatistics
                {
                    PeakLevelId = (uint)preId
                };

                var rec = challengeData.PeakLevelDatas.GetValueOrDefault(preId);
                if (rec != null)
                {
                    var index = 0u;
                    stt.Lineup = new ChallengeLineupList
                    {
                        AvatarList =
                        {
                            rec.BaseAvatarList.Select(x => new ChallengeAvatarInfo
                            {
                                Index = index++,
                                Id = x,
                                AvatarType = AvatarType.AvatarFormalType,
                                Level =
                                    (uint)(avatarData.FormalAvatars.Find(avatar => avatar.BaseAvatarId == x)?.Level ??
                                           1)
                            })
                        }
                    };

                    stt.PeakRoundsCount = rec.RoundCnt;
                }

                peakRec.PreLevelSttList.Add(stt);
            }

            var bossRec = challengeData.PeakBossLevelDatas.GetValueOrDefault((data.BossLevelID << 2) | 1);
            bossRec ??= challengeData.PeakBossLevelDatas.GetValueOrDefault((data.BossLevelID << 2) | 0);
            if (bossRec != null)
                peakRec.BossLevelStt = new BossLevelStatistics
                {
                    PeakLevelId = (uint)bossRec.LevelId,
                    BuffId = bossRec.BuffId,
                    Lineup = new ChallengeLineupList
                    {
                        AvatarList =
                        {
                            bossRec.BaseAvatarList.Select((x, index) => new ChallengeAvatarInfo
                            {
                                Index = (uint)index,
                                Id = x,
                                AvatarType = AvatarType.AvatarFormalType,
                                Level =
                                    (uint)(avatarData.FormalAvatars.Find(avatar => avatar.BaseAvatarId == x)?.Level ??
                                           1)
                            })
                        }
                    },
                    LeastRoundsCount = bossRec.RoundCnt
                };

            proto.PeakRecord.Add(peakRec);
        }

        SetData(proto);
    }

    public PacketGetFriendBattleRecordDetailScRsp(Retcode code) : base(CmdIds.GetFriendBattleRecordDetailScRsp)
    {
        var proto = new GetFriendBattleRecordDetailScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }
}