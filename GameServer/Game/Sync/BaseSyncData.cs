using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.Sync;

public abstract class BaseSyncData
{
    public abstract void SyncData(in PlayerSyncScNotify notify);
}