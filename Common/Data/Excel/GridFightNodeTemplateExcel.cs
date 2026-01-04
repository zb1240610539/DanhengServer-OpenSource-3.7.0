namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightNodeTemplate.json")]
public class GridFightNodeTemplateExcel : ExcelResource
{
    public uint NodeTemplateID { get; set; }
    public uint PenaltyBonusRuleID { get; set; }
    public uint IsAugment { get; set; }
    public uint BasicGoldRewardNum { get; set; }

    public override int GetId()
    {
        return (int)NodeTemplateID;
    }

    public override void Loaded()
    {
        GameData.GridFightNodeTemplateData.TryAdd(NodeTemplateID, this);
    }
}