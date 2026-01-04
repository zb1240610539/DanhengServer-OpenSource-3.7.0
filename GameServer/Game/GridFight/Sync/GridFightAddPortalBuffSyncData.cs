using EggLink.DanhengServer.Proto;
using GridFightPortalBuffInfo = EggLink.DanhengServer.GameServer.Game.GridFight.Component.GridFightPortalBuffInfo;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public class GridFightAddPortalBuffSyncData(GridFightSrc src, GridFightPortalBuffInfo info, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            PortalBuffSyncInfo = info.ToSyncInfo()
        };
    }
}