using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightUseOrbNotify : BasePacket
{
    public PacketGridFightUseOrbNotify(uint uniqueId, List<GridFightDropItemInfo> drops) : base(CmdIds.GridFightUseOrbNotify)
    {
        var proto = new GridFightUseOrbNotify
        {
            DropItemList = new GridFightDropInfo
            {
                DropItemList = { drops }
            },
            OrbUniqueId = uniqueId
        };

        SetData(proto);
    }
}