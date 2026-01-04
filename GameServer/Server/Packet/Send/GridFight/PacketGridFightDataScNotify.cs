using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightDataScNotify : BasePacket
{
    public PacketGridFightDataScNotify(GridFightSrc src, List<BaseGridFightSyncData> data) : base(CmdIds.GridFightDataScNotify)
    {
        var proto = new GridFightDataScNotify
        {
            GridUpdateSrc = src,
            UpdateDynamicList = { data.Select(x => x.ToProto()) }
        };

        SetData(proto);
    }

    public PacketGridFightDataScNotify(GridFightSrc src, params BaseGridFightSyncData[] data) : this(src, data.ToList())
    {
    }
}