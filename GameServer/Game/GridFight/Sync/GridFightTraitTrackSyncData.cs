using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public class GridFightTraitTrackSyncData(GridFightSrc src, GridFightBasicInfoPb info, uint groupId = 0, params uint[] syncParams) : BaseGridFightSyncData(src, groupId, syncParams)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            TraitTrackSyncInfo = new GridFightTraitTrackSyncInfo
            {
                TrackTraitIdList = { info.TrackingTraits }
            }
        };
    }
}