using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public class GridFightEliteBranchSyncData(GridFightSrc src, GridFightGameSectionInfo section, uint groupId = 0, params uint[] syncParams) : BaseGridFightSyncData(src, groupId, syncParams)
{
    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            EliteBranchSyncInfo = new GridFightEliteBranchSyncInfo
            {
                EliteBranchId = section.BranchId
            }
        };
    }
}