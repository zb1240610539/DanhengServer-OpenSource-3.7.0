using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public abstract class BaseGridFightSyncData(GridFightSrc src, uint groupId = 0, params uint[] syncParams)
{
    public GridFightSrc Src { get; set; } = src;
    public uint GroupId { get; set; } = groupId;
    public uint[] SyncParams { get; set; } = syncParams;
    public abstract GridFightSyncData ToProto();
}