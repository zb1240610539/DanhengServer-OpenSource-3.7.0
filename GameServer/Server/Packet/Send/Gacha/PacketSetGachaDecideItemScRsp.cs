using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Gacha;

public class PacketSetGachaDecideItemScRsp : BasePacket
{
    public PacketSetGachaDecideItemScRsp(uint gachaId, List<uint> order) : base(CmdIds.SetGachaDecideItemScRsp)
    {
        var proto = new SetGachaDecideItemScRsp
        {
            DecideItemInfo = new DecideItemInfo
            {
                DecideItemOrder = { order },
                CHDOIBFEHLP = 1,
                JIGONEALCPC = { 11 }
            }
        };

        SetData(proto);
    }
}