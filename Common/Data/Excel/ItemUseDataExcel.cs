namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("ItemUseData.json")]
public class ItemUseDataExcel : ExcelResource
{
    public int UseDataID { get; set; }
    public int UseMultipleMax { get; set; }
    public List<int> UseParam { get; set; } = [];
    public bool IsAutoUse { get; set; }

    public override int GetId()
    {
        return UseDataID;
    }

    public override void Loaded()
    {
        GameData.ItemUseDataData.TryAdd(UseDataID, this);
    }
}