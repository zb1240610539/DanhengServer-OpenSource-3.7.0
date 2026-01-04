namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("RogueTournDivision.json")]
public class RogueTournDivisionExcel : ExcelResource
{
    public int DivisionLevel { get; set; }
    public int DivisionProgress { get; set; }

    public override int GetId()
    {
        return DivisionLevel;
    }

    public override void Loaded()
    {
        GameData.RogueTournDivisionData.Add(DivisionLevel, this);
    }
}