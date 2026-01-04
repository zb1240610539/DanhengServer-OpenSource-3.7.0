using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public class GridFightRoleTrackEquipmentSyncData(
    GridFightSrc src,
    GridFightBasicComponent basicComp,
    uint groupId = 0,
    params uint[] syncParams) : BaseGridFightSyncData(src, groupId, syncParams)
{
    public override GridFightSyncData ToProto()
    {
        var roleComp = basicComp.Inst.GetComponent<GridFightRoleComponent>();
        var itemsComp = basicComp.Inst.GetComponent<GridFightItemsComponent>();

        return new GridFightSyncData
        {
            EquipmentTrackSyncInfo = new RoleTrackEquipmentSyncInfo
            {
                RoleTrackEquipmentList =
                    { basicComp.Data.TrackingEquipments.Select(x => x.ToProto(roleComp, itemsComp)) }
            }
        };
    }
}