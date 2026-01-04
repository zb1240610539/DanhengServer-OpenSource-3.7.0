using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public class GridFightFinishPendingActionSyncData(GridFightSrc src, uint queuePosition, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            FinishPendingActionPos = queuePosition
        };
    }
}