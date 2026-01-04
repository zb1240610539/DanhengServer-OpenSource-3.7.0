using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.Sync.Player;

public class PlayerBoardSync(PlayerInstance player) : BaseSyncData
{
    public override void SyncData(in PlayerSyncScNotify notify)
    {
        notify.PlayerboardModuleSync = new PlayerBoardModuleSync
        {
            Signature = player.Data.Signature,
            HeadFrame = player.Data.HeadFrame.ToProto()
        };
    }
}