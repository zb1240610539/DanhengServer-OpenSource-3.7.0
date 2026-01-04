using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public class GridFightBuyExpCostSyncData(GridFightSrc src, GridFightBasicInfoPb info, params uint[] param) : BaseGridFightSyncData(src, 0, param)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            GridFightLevelCost = info.BuyLevelCost
        };
    }
}