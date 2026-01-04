namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightTalent.json")]
public class GridFightTalentExcel : ExcelResource
{
    public uint ID { get; set; }
    public uint Cost { get; set; }
    public List<uint> PreTalentIDList { get; set; } = [];

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightTalentData.TryAdd(ID, this);
    }
}