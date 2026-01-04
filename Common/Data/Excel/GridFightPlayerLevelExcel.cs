using Newtonsoft.Json;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightPlayerLevel.json")]
public class GridFightPlayerLevelExcel : ExcelResource
{
    public uint PlayerLevel { get; set; }
    public uint LevelUpExp { get; set; }
    public uint AvatarMaxNumber { get; set; }
    public uint Rarity1Weight { get; set; }
    public uint Rarity2Weight { get; set; }
    public uint Rarity3Weight { get; set; }
    public uint Rarity4Weight { get; set; }
    public uint Rarity5Weight { get; set; }

    [JsonIgnore] public List<uint> RarityWeights =>
        [Rarity1Weight, Rarity2Weight, Rarity3Weight, Rarity4Weight, Rarity5Weight];

    public override int GetId()
    {
        return (int)PlayerLevel;
    }

    public override void Loaded()
    {
        GameData.GridFightPlayerLevelData.TryAdd(PlayerLevel, this);
    }
}