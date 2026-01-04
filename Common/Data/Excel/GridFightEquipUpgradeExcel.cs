namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightEquipUpgrade.json")]
public class GridFightEquipUpgradeExcel : ExcelResource
{
    public uint PreID { get; set; }
    public uint UpgradeID { get; set; }

    public override int GetId()
    {
        return (int)PreID;
    }

    public override void Loaded()
    {
        GameData.GridFightEquipUpgradeData.TryAdd(PreID, this);
    }
}