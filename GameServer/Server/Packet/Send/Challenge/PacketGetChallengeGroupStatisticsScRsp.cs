using EggLink.DanhengServer.Database.Friend;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Challenge;

public class PacketGetChallengeGroupStatisticsScRsp : BasePacket
{
    public PacketGetChallengeGroupStatisticsScRsp(uint groupId, ChallengeGroupStatisticsPb? data) : base(
        CmdIds.GetChallengeGroupStatisticsScRsp)
    {
        var proto = new GetChallengeGroupStatisticsScRsp
        {
            GroupId = groupId
        };

        var maxMemory = data?.MemoryGroupStatistics?.Values.MaxBy(x => x.Level);
        if (maxMemory != null) proto.MemoryGroup = maxMemory.ToProto();

        var maxStory = data?.StoryGroupStatistics?.Values.MaxBy(x => x.Level);
        if (maxStory != null) proto.StoryGroup = maxStory.ToProto();

        var maxBoss = data?.BossGroupStatistics?.Values.MaxBy(x => x.Level);
        if (maxBoss != null) proto.BossGroup = maxBoss.ToProto();

        SetData(proto);
    }
}