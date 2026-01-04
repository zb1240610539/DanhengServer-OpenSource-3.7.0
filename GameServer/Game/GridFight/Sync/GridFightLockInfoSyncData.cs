using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public class GridFightLockInfoSyncData(GridFightSrc src, GridFightBasicInfoPb info, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            SyncLockInfo = new GridFightLockInfo
            {
                LockType = (GridFightLockType)info.LockType,
                LockReason = (GridFightLockReason)info.LockReason
            }
        };
    }
}