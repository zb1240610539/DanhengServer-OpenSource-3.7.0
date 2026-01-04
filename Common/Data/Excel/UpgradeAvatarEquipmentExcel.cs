using EggLink.DanhengServer.Enums.Avatar;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("UpgradeAvatarEquipment.json")]
public class UpgradeAvatarEquipmentExcel : ExcelResource
{
    [JsonProperty("ELADHEIOONA")] public uint EquipmentId { get; set; }

    [JsonProperty("FJIFJPGEJID")]
    [JsonConverter(typeof(StringEnumConverter))]
    public AvatarBaseTypeEnum BaseType { get; set; }

    public override int GetId()
    {
        return (int)BaseType;
    }

    public override void Loaded()
    {
        GameData.UpgradeAvatarEquipmentData.TryAdd(BaseType, this);
    }
}