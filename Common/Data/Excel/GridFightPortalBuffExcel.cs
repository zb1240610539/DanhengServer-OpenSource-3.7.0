namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightPortalBuff.json")]
public class GridFightPortalBuffExcel : ExcelResource
{
    public uint ID { get; set; }
    public bool IfInBook { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightPortalBuffData.TryAdd(ID, this);
    }
}