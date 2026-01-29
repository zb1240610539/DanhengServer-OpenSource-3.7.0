using EggLink.DanhengServer.GameServer.Game.Rogue.Scene;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Rogue;

public class PacketSyncRogueMapRoomScNotify : BasePacket
{
    // 模仿 LunarCore 的逻辑，传入 Rogue 实例数据和房间数据
    public PacketSyncRogueMapRoomScNotify(uint levelId, uint mapId, RogueRoom roomProto) : base(CmdIds.SyncRogueMapRoomScNotify)
    {
        // 处理老版本混淆字段
        // 猜测：在老版本中，room_info 内部的 BEEEBOIOJIF (Tag 8) 必须同步 CurStatus
        // 否则客户端地图不会更新房间的“已完成”视觉状态
        roomProto.BEEEBOIOJIF = roomProto.CurStatus; 

        // 猜测：IMIMGFAAGHM (Tag 5) 可能需要对应房间的位置索引或 SiteId
        if (roomProto.IMIMGFAAGHM == 0) {
            roomProto.IMIMGFAAGHM = roomProto.SiteId;
        }

        var proto = new SyncRogueMapRoomScNotify
        {
            LevelId = levelId, // Tag 1
            MapId = mapId,     // Tag 2
            RoomInfo = roomProto // Tag 3 (对应老版本的 room_info)
        };

        SetData(proto);
    }
}
