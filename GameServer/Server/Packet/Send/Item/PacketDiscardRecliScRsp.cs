using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Item;

public class PacketDiscardRelicScRsp : BasePacket
{
    public PacketDiscardRelicScRsp(bool success, bool isDiscard) : base(CmdIds.DiscardRelicScRsp)
    {
        DiscardRelicScRsp proto = new();

        if (success) proto.IsDiscard = isDiscard;
        else proto.Retcode = (uint)Retcode.RetFail;

        SetData(proto);
    }
}