using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public class GridFightLevelSyncData(
    GridFightSrc src,
    GridFightLevelComponent level,
    uint groupId = 0,
    params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            LevelSyncInfo = new GridFightLevelSyncInfo
            {
                SectionId = level.CurrentSection.SectionId,
                ChapterId = level.CurrentSection.ChapterId,
                GridFightLayerInfo = new GridFightLayerInfo
                {
                    RouteInfo = level.CurrentSection.ToRouteInfo(),
                    RouteIsPending = level.CurrentSection.Excel.IsAugment == 1
                }
            }
        };
    }
}