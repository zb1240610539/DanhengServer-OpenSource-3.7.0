using EggLink.DanhengServer.GameServer.Game.Rogue.Scene;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueMapRoomScNotify : BasePacket
{
    public PacketSyncRogueMapRoomScNotify(RogueRoomInstance room, int mapId) : base(CmdIds.SyncRogueMapRoomScNotify)
    {
        var proto = new SyncRogueMapRoomScNotify
        {   roomProto.BEEEBOIOJIF = roomProto.CurStatus;
        	// 猜测：IMIMGFAAGHM (Tag 5) 可能需要对应房间的位置索引或 SiteId
        	if (roomProto.IMIMGFAAGHM == 0) {
            roomProto.IMIMGFAAGHM = roomProto.SiteId;
        	}
            CurRoom = room.ToProto(),
            MapId = (uint)mapId
        };

        SetData(proto);
    }
}
