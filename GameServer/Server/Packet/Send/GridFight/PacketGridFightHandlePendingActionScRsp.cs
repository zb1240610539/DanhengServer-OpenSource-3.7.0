using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightHandlePendingActionScRsp : BasePacket
{
    public PacketGridFightHandlePendingActionScRsp(uint pos) : base(CmdIds.GridFightHandlePendingActionScRsp)
    {
        var proto = new GridFightHandlePendingActionScRsp
        {
            QueuePosition = pos
        };

        SetData(proto);
    }
}