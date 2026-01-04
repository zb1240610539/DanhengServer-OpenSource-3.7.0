namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightDivisionStage.json")]
public class GridFightDivisionStageExcel : ExcelResource
{
    public uint DivisionID { get; set; }
    public uint EnemyDifficultyLevel { get; set; }
    public List<uint> AffixChooseNumList { get; set; } = [];
    public uint ExpModify { get; set; }
    public uint SeasonID { get; set; }

    public override int GetId()
    {
        return (int)DivisionID;
    }

    public override void Loaded()
    {
        GameData.GridFightDivisionStageData.TryAdd(DivisionID, this);
    }
}