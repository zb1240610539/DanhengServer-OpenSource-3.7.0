namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightSeasonTalent.json")]
public class GridFightSeasonTalentExcel : ExcelResource
{
    public uint ID { get; set; }
    public uint Cost { get; set; }
    public uint SeasonID { get; set; }
    public List<uint> PreTalentIDList { get; set; } = [];

    public override int GetId()
    {
        return (int)ID;
    }
    public override void Loaded()
    {
        GameData.GridFightSeasonTalentData.TryAdd(ID, this);
    }
}