using EggLink.DanhengServer.Enums.GridFight;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightConsumables.json")]
public class GridFightConsumablesExcel : ExcelResource
{
    public uint ID { get; set; }
    public bool IfStack { get; set; }
    public bool IfConsume { get; set; }
    public List<uint> ConsumableParamList { get; set; } = [];

    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightConsumeTypeEnum ConsumableRule { get; set; }

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightConsumablesData.TryAdd(ID, this);
    }
}