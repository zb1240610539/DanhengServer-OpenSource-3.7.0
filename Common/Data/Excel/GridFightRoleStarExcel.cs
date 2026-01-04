namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightRoleStar.json")]
public class GridFightRoleStarExcel : ExcelResource
{
    public uint ID { get; set; }
    public uint Star { get; set; }
    public uint BEID { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightRoleStarData.TryAdd(ID << 4 | Star, this);
    }
}