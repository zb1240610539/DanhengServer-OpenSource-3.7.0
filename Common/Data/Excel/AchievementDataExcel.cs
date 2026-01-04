namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("AchievementData.json")]
public class AchievementDataExcel : ExcelResource
{
    public int QuestID { get; set; }
    public int AchievementID { get; set; }

    public override int GetId()
    {
        return AchievementID;
    }

    public override void Loaded()
    {
        GameData.AchievementDataData.TryAdd(AchievementID, this);
    }
}