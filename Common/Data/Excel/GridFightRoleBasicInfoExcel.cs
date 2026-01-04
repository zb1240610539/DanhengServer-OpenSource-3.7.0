namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightRoleBasicInfo.json")]
public class GridFightRoleBasicInfoExcel : ExcelResource
{
    public uint ID { get; set; }
    public uint SpecialAvatarID { get; set; }
    public uint AvatarID { get; set; }
    public uint SeasonID { get; set; }
    public uint Rarity { get; set; }
    public List<uint> TraitList { get; set; } = [];
    public List<string> RoleSavedValueList { get; set; } = [];
    public bool IsInPool { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightRoleBasicInfoData.TryAdd(ID, this);
    }
}