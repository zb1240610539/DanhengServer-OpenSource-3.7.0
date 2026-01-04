using EggLink.DanhengServer.Enums.Avatar;
using EggLink.DanhengServer.Enums.Item;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("UpgradeAvatarSubRelic.json")]
public class UpgradeAvatarSubRelicExcel : ExcelResource
{
    [JsonProperty("GOBEGPKDLLF")]
    public uint RelicLevel { get; set; }

    [JsonProperty("COACLFEBBDA")]
    [JsonConverter(typeof(StringEnumConverter))]
    public UpgradeAvatarSubRelicTypeEnum SubType { get; set; }

    [JsonProperty("JLLLGAGBEMF")]
    [JsonConverter(typeof(StringEnumConverter))]
    public RarityEnum Rarity { get; set; }


    [JsonProperty("GEEKCAGBGMN")]
    [JsonConverter(typeof(StringEnumConverter))]
    public RelicTypeEnum Type { get; set; }

    [JsonProperty("PINMGEKOAKM")]
    public List<UpgradeAvatarSubAffixInfo> SubAffixes { get; set; } = [];

    public override int GetId()
    {
        return (int)SubType;
    }

    public override void Loaded()
    {
        GameData.UpgradeAvatarSubRelicData.TryAdd(SubType, []);
        GameData.UpgradeAvatarSubRelicData[SubType].TryAdd(Rarity, []);
        GameData.UpgradeAvatarSubRelicData[SubType][Rarity].TryAdd(RelicLevel, []);
        GameData.UpgradeAvatarSubRelicData[SubType][Rarity][RelicLevel].TryAdd(Type, this);
    }
}

public class UpgradeAvatarSubAffixInfo
{
    [JsonProperty("KHADHNNCFLH")]
    public uint AffixCount { get; set; }

    [JsonProperty("LKOEFDPJGKD")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AvatarPropertyTypeEnum AffixProperty { get; set; }
}