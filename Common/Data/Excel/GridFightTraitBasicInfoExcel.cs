namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightTraitBasicInfo.json")]
public class GridFightTraitBasicInfoExcel : ExcelResource
{
    public uint ID { get; set; }
    public uint SeasonID { get; set; }
    public List<uint> TraitEffectList { get; set; } = [];
    public List<uint> BEIDList { get; set; } = [];

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightTraitBasicInfoData.TryAdd(ID, this);
    }
}