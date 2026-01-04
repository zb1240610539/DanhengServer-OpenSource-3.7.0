using EggLink.DanhengServer.Enums.Avatar;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("UpgradeAvatarSubType.json")]
public class UpgradeAvatarSubTypeExcel : ExcelResource
{
    [JsonProperty("DJPCAIKIONP")]
    public uint AvatarId { get; set; }

    [JsonProperty("COACLFEBBDA")]
    [JsonConverter(typeof(StringEnumConverter))]
    public UpgradeAvatarSubRelicTypeEnum SubType { get; set; }

    public override int GetId()
    {
        if (AvatarId == 0)
        {
            throw new KeyNotFoundException("Upgrade Avatar Should Be Updated!");
        }

        return (int)AvatarId;
    }

    public override void Loaded()
    {
        GameData.UpgradeAvatarSubTypeData.TryAdd(AvatarId, this);
    }
}