using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public class GridFightRoleUpdateSyncData(GridFightSrc src, GridFightRoleInfoPb role, uint groupId = 0, params uint[] param) : BaseGridFightSyncData(src, groupId, param)
{
    public GridFightRoleInfoPb Role { get; set; } = role;

    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            UpdateRoleInfo = Role.ToProto()
        };
    }
}