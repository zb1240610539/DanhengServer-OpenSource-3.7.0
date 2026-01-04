using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightUseConsumableScRsp : BasePacket
{
    public PacketGridFightUseConsumableScRsp(Retcode ret) : base(CmdIds.GridFightUseConsumableScRsp)
    {
        var proto = new GridFightUseConsumableScRsp
        {
            Retcode = (uint)ret
        };

        SetData(proto);
    }
}