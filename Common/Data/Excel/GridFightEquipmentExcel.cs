using EggLink.DanhengServer.Enums.GridFight;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightEquipment.json")]
public class GridFightEquipmentExcel : ExcelResource
{
    public uint ID { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightEquipCategoryEnum EquipCategory { get; set; }


    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightEquipDressTypeEnum DressRule { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightEquipFuncTypeEnum EquipFunc { get; set; }

    public List<uint> EquipFuncParamList { get; set; } = [];
    public List<uint> DressRuleParamList { get; set; } = [];

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightEquipmentData.TryAdd(ID, this);
    }
}