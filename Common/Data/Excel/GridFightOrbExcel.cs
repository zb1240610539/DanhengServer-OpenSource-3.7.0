using EggLink.DanhengServer.Enums.GridFight;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightOrb.json")]
public class GridFightOrbExcel : ExcelResource
{
    public uint BonusID { get; set; }
    public uint OrbID { get; set; }
    public HashName OrbName { get; set; } = new();

    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightOrbTypeEnum Type { get; set; }

    public override int GetId()
    {
        return (int)OrbID;
    }

    public override void Loaded()
    {
        GameData.GridFightOrbData.TryAdd(OrbID, this);
    }
}