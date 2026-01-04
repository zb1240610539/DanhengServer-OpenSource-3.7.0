using EggLink.DanhengServer.Enums.GridFight;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightBasicBonusPoolV2.json")]
public class GridFightBasicBonusPoolV2Excel : ExcelResource
{
    public uint BonusID { get; set; }
    public uint Value { get; set; }
    public uint BonusTypeParam { get; set; }
    public List<uint> BonusTypeParamList { get; set; } = [];

    [JsonConverter(typeof(StringEnumConverter))]
    public GridFightBonusTypeEnum BonusType { get; set; }

    public override int GetId()
    {
        return (int)BonusID;
    }

    public override void Loaded()
    {
        GameData.GridFightBasicBonusPoolV2Data.TryAdd(BonusID, this);
    }
}