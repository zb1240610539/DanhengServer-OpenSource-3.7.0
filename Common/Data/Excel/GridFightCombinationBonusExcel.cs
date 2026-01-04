namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightCombinationBonus.json")]
public class GridFightCombinationBonusExcel : ExcelResource
{
    public uint BonusID { get; set; }
    public List<uint> CombinationBonusList { get; set; } = [];
    public List<uint> BonusNumberList { get; set; } = [];

    public override int GetId()
    {
        return (int)BonusID;
    }

    public override void Loaded()
    {
        GameData.GridFightCombinationBonusData.TryAdd(BonusID, this);
    }
}