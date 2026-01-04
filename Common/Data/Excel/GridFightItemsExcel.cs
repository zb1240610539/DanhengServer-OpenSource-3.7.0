using System.Collections.Generic;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightItems.json")]
public class GridFightItemsExcel : ExcelResource
{
    public uint ID { get; set; }
    public HashName ItemName { get; set; } = new();

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightItemsData.TryAdd(ID, this);
    }
}