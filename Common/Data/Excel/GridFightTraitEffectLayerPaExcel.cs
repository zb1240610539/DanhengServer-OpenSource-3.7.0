using EggLink.DanhengServer.Data.Config;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("GridFightTraitEffectLayerPa.json")]
public class GridFightTraitEffectLayerPaExcel : ExcelResource
{
    public uint ID { get; set; }
    public uint Layer { get; set; }
    public List<FixedValueInfo<double>> EffectParamList { get; set; } = [];

    public override int GetId()
    {
        return (int)ID;
    }

    public override void Loaded()
    {
        GameData.GridFightTraitEffectLayerPaData.TryAdd(ID, []);
        GameData.GridFightTraitEffectLayerPaData[ID][Layer] = this;
    }
}