using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightBuyGoodsScRsp : BasePacket
{
    public PacketGridFightBuyGoodsScRsp(Retcode retcode) : base(CmdIds.GridFightBuyGoodsScRsp)
    {
        var rsp = new GridFightBuyGoodsScRsp
        {
            Retcode = (uint)retcode
        };

        SetData(rsp);
    }
}