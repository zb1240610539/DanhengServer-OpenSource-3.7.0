using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.PendingAction;

public class GridFightRecommendEquipmentPendingAction(GridFightInstance inst, List<uint> equipmentList) : BaseGridFightPendingAction(inst)
{
    public List<uint> EquipmentList { get; } = equipmentList;

    public override GridFightPendingAction ToProto()
    {
        return new GridFightPendingAction
        {
            RecommendEquipmentAction = new GridFightRecommendEquipmentActionInfo
            {
                AvailableEquipmentList = { EquipmentList }
            },
            QueuePosition = QueuePosition
        };
    }
}