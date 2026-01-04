using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Sync;

public class GridFightAddGameItemSyncData(GridFightSrc src, List<GridFightEquipmentItemPb> equipment, List<GridFightConsumableUpdateInfo> consumables, uint groupId = 0, params uint[] syncParams) : BaseGridFightSyncData(src, groupId, syncParams)
{
    public List<GridFightEquipmentItemPb> Equipment { get; } = equipment;
    public List<GridFightConsumableUpdateInfo> Consumables { get; } = consumables;

    public override GridFightSyncData ToProto()
    {
        return new GridFightSyncData
        {
            AddGameItemInfo = new GridFightGameItemSyncInfo
            {
                GridFightEquipmentList = { Equipment.Select(x => x.ToProto()) },
                UpdateGridFightConsumableList = { Consumables }
            }
        };
    }
}