using EggLink.DanhengServer.Database.Friend;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;

public class PacketGetFriendDevelopmentInfoScRsp : BasePacket
{
    public PacketGetFriendDevelopmentInfoScRsp(Retcode code) : base(CmdIds.GetFriendDevelopmentInfoScRsp)
    {
        var proto = new GetFriendDevelopmentInfoScRsp
        {
            Retcode = (uint)code
        };

        SetData(proto);
    }

    public PacketGetFriendDevelopmentInfoScRsp(FriendRecordData data) : base(CmdIds.GetFriendDevelopmentInfoScRsp)
    {
        foreach (var friendDevelopmentInfoPb in data.DevelopmentInfos.ToArray())
            if (Extensions.GetUnixSec() - friendDevelopmentInfoPb.Time >=
                TimeSpan.TicksPerDay * 7 / TimeSpan.TicksPerSecond)
                data.DevelopmentInfos.Remove(friendDevelopmentInfoPb);

        var proto = new GetFriendDevelopmentInfoScRsp
        {
            DevelopmentList = { data.DevelopmentInfos.Select(x => x.ToProto()) },
            Uid = (uint)data.Uid
        };

        SetData(proto);
    }
}