using EggLink.DanhengServer.Database.Inventory;
using EggLink.DanhengServer.Enums.Item;
using EggLink.DanhengServer.Util;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("MappingInfo.json")]
public class MappingInfoExcel : ExcelResource
{
    public int ID { get; set; }
    public int WorldLevel { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public FarmTypeEnum FarmType { get; set; } = FarmTypeEnum.None; // is enum

    public List<MappingInfoItem> DisplayItemList { get; set; } = [];

    [JsonIgnore] public List<MappingInfoItem> DropItemList { get; set; } = [];

    [JsonIgnore] public List<MappingInfoItem> DropRelicItemList { get; set; } = [];

    public override int GetId()
    {
        return ID * 10 + WorldLevel;
    }
	public override void Loaded()
    {
        // 1. 获取当前 Key 并存入全局字典
        // 使用索引器赋值防止重复加载导致的 Key 冲突报错
        GameData.MappingInfoData[GetId()] = this;

        // 2. 【核心修复逻辑】：针对 ELEMENT (进阶材料) 数据缺失的“影子补全”
        // 如果当前副本列表为空（常见于 WL 0, 1, 6），则尝试从其他均衡等级借用 ID 模板
        if (this.FarmType == FarmTypeEnum.ELEMENT && this.DisplayItemList.Count == 0)
        {
            // 轮询 WL 2, 3, 4, 5，直到找到一个含有材料 ID (如虚幻铸铁 110406) 的模板
            for (int i = 2; i <= 5; i++)
            {
                int templateKey = this.ID * 10 + i;
                if (GameData.MappingInfoData.TryGetValue(templateKey, out var template) && template.DisplayItemList.Count > 0)
                {
                    // 使用 new List 进行深拷贝，确保修改当前对象的 DropItemList 不会干扰到种子模板
                    this.DisplayItemList = new List<MappingInfoItem>(template.DisplayItemList);
                    break; 
                }
            }
        }

        // 3. 原有的空检查。现在 ELEMENT 类型即便是 WL 0/1/6 也已经有了借来的 ID 列表。
        if (DisplayItemList.Count == 0) return;

        List<int> equipDrop = [];
        Dictionary<int, List<int>> relicDrop = [];

        foreach (var item in DisplayItemList)
        {
            // 数量大于 0 的直接添加 (通常是开拓经验)
            if (item.ItemNum > 0)
            {
                DropItemList.Add(item);
                continue;
            }

            // 信用点动态计算
            if (item.ItemID == 2)
            {
                DropItemList.Add(new MappingInfoItem()
                {
                    ItemID = 2,
                    MinCount = (50 + WorldLevel * 10) * (int)FarmType,
                    MaxCount = (100 + WorldLevel * 10) * (int)FarmType
                });
                continue;
            }

            GameData.ItemConfigData.TryGetValue(item.ItemID, out var excel);
            if (excel == null) continue;

            // 遗器展示逻辑
            if (excel.ItemSubType == ItemSubTypeEnum.RelicSetShowOnly)
            {
                var baseRelicId = item.ItemID / 10 % 1000;
                var baseRarity = item.ItemID % 10;
                var relicStart = 20001 + baseRarity * 10000 + baseRelicId * 10;
                var relicEnd = relicStart + 3;
                for (; relicStart <= relicEnd; relicStart++)
                {
                    GameData.ItemConfigData.TryGetValue(relicStart, out var relicExcel);
                    if (relicExcel == null) break;

                    if (!relicDrop.TryGetValue(baseRarity, out _))
                    {
                        var value = new List<int>();
                        relicDrop[baseRarity] = value;
                    }
                    relicDrop[baseRarity].Add(relicStart);
                }
            }
            // 材料类计算
            else if (excel.ItemMainType == ItemMainTypeEnum.Material)
            {
                MappingInfoItem? drop;
               switch (excel.PurposeType)
	{
    case 1: // 角色经验 (书)
        // 官服逻辑：经验书通常是必掉的，但数量随等级提升
        var expAmount = excel.Rarity switch
        {
            ItemRarityEnum.NotNormal => WorldLevel < 3 ? 3 : 4, // 蓝色
            ItemRarityEnum.Rare => WorldLevel < 3 ? 0 : WorldLevel - 2, // 紫色
            _ => 1
        };
        drop = new MappingInfoItem(excel.ID, (int)expAmount) { Chance = 100 };
        break;

    case 2: // 晋阶材料 (大世界BOSS/虚幻铸铁等)
        // 官服逻辑：必掉，数量 2-3 或 4-5
        int bossCount = WorldLevel >= 4 ? 5 : (WorldLevel >= 2 ? 3 : 2);
        drop = new MappingInfoItem(excel.ID, bossCount)
        {
            Chance = 100, 
            MinCount = bossCount,
            MaxCount = (WorldLevel >= 3) ? bossCount + 1 : bossCount
        };
        break;

    case 3: // 行迹材料 (花萼赤)
        // 【核心随机点】官服最典型的随机：绿必掉，蓝高概率，紫低概率
        int traceChance = excel.Rarity switch
        {
            ItemRarityEnum.Normal => 100,                     // 绿色：100%
            ItemRarityEnum.NotNormal => 30 + (WorldLevel * 10), // 蓝色：WL3(60%) -> WL6(90%)
            ItemRarityEnum.Rare => 5 + (WorldLevel * 4),      // 紫色：WL3(17%) -> WL6(29%)
            _ => 100
        };
        drop = new MappingInfoItem(excel.ID, 1) { Chance = traceChance };
        break;

    case 5: // 光锥经验 (提纯以太)
        // 数量略有随机
        var lcExpCount = excel.Rarity switch
        {
            ItemRarityEnum.NotNormal => 3,
            ItemRarityEnum.Rare => WorldLevel >= 3 ? 1 : 0,
            _ => 2
        };
        drop = new MappingInfoItem(excel.ID, (int)lcExpCount) 
        { 
            Chance = (excel.Rarity == ItemRarityEnum.Rare) ? (WorldLevel * 15) : 100 
        };
        break;

    case 11: // 遗器合成材料 (残骸)
        drop = new MappingInfoItem(excel.ID, 10) { Chance = 100 };
        break;

    default:
        drop = new MappingInfoItem(excel.ID, 1) { Chance = 100 };
        break;
	}

                if (drop != null) DropItemList.Add(drop);
            }
            else if (excel.ItemMainType == ItemMainTypeEnum.Equipment)
            {
                equipDrop.Add(excel.ID);
            }
        }

        if (equipDrop.Count > 0)
		{
    foreach (var dropId in equipDrop)
    {
        // 官服逻辑：均衡等级越高，掉狗粮的概率稍微提升一点
        // WL0: 15% | WL3: 21% | WL6: 27%
        int lcChance = 15 + (WorldLevel * 2); 

        MappingInfoItem d = new(dropId, 1) 
        { 
        Chance = lcChance 
        };
        DropItemList.Add(d);
    }
		}

        // 处理遗器具体掉落数量
        if (relicDrop.Count > 0)
        {
            foreach (var entry in relicDrop)
            {
                foreach (var value in entry.Value)
                {
                    MappingInfoItem d = new(value, 1);
                    var relicAmount = entry.Key switch
                    {
                        4 => WorldLevel * 0.5 - 0.5,
                        3 => WorldLevel * 0.5 + (WorldLevel == 2 ? 1.0 : 0),
                        2 => 6 - WorldLevel + 0.5 - (WorldLevel == 1 ? 3.75 : 0),
                        _ => WorldLevel == 1 ? 6 : 2
                    };
                    if (relicAmount > 0)
                    {
                        d.ItemNum = (int)relicAmount;
                        DropRelicItemList.Add(d);
                    }
                }
            }
        }
    }
    

    public List<ItemData> GenerateRelicDrops()
    {
        var relicsMap = new Dictionary<int, List<MappingInfoItem>>();
        foreach (var relic in DropRelicItemList)
        {
            GameData.ItemConfigData.TryGetValue(relic.ItemID, out var itemData);
            if (itemData == null) continue;
            switch (itemData.Rarity)
            {
                case ItemRarityEnum.NotNormal:
                    AddRelicToMap(relic, 2, relicsMap);
                    break;
                case ItemRarityEnum.Rare:
                    AddRelicToMap(relic, 3, relicsMap);
                    break;
                case ItemRarityEnum.VeryRare:
                    AddRelicToMap(relic, 4, relicsMap);
                    break;
                case ItemRarityEnum.SuperRare:
                    AddRelicToMap(relic, 5, relicsMap);
                    break;
                default:
                    continue;
            }
        }

        List<ItemData> drops = [];
        // Add higher rarity relics first
        for (var rarity = 5; rarity >= 2; rarity--)
        {
            var count = GetRelicCountByWorldLevel(rarity) *
                        ConfigManager.Config.ServerOption.ValidFarmingDropRate();
            if (count <= 0) continue;
            if (!relicsMap.TryGetValue(rarity, out var value)) continue;
            if (value.IsNullOrEmpty()) continue;
            while (count > 0)
            {
                var relic = value.RandomElement();
                drops.Add(new ItemData
                {
                    ItemId = relic.ItemID,
                    Count = 1
                });
                count--;
            }
        }

        return drops;
    }

    private void AddRelicToMap(MappingInfoItem relic, int rarity, Dictionary<int, List<MappingInfoItem>> relicsMap)
    {
        if (relicsMap.TryGetValue(rarity, out var value))
            value.Add(relic);
        else
            relicsMap.Add(rarity, [relic]);
    }

    private int GetRelicCountByWorldLevel(int rarity)
    {
        return WorldLevel switch
        {
            1 => rarity switch
            {
                2 => 6,
                3 => 3,
                4 => 1,
                5 => 0,
                _ => 0
            },
            2 => rarity switch
            {
                2 => 2,
                3 => 4,
                4 => 2 + LuckyRelicDropped(),
                5 => 0,
                _ => 0
            },
            3 => rarity switch
            {
                2 => 0,
                3 => 4,
                4 => 2,
                5 => 1,
                _ => 0
            },
            4 => rarity switch
            {
                2 => 0,
                3 => 3,
                4 => 2 + LuckyRelicDropped(),
                5 => 1 + LuckyRelicDropped(),
                _ => 0
            },
            5 => rarity switch
            {
                2 => 0,
                3 => 1 + LuckyRelicDropped(),
                4 => 3,
                5 => 2,
                _ => 0
            },
            6 => rarity switch
            {
                2 => 0,
                3 => 0,
                4 => 5,
                5 => 2 + LuckyRelicDropped(),
                _ => 0
            },
            _ => 0
        };
    }

    private int LuckyRelicDropped()
    {
        return Random.Shared.Next(100) < 25 ? 1 : 0;
    }
}

public class MappingInfoItem
{
    public MappingInfoItem()
    {
    }

    public MappingInfoItem(int itemId, int itemNum)
    {
        ItemID = itemId;
        ItemNum = itemNum;
    }

    public int ItemID { get; set; }
    public int ItemNum { get; set; }

    public int MinCount { get; set; }
    public int MaxCount { get; set; }
    public int Chance { get; set; } = 100;
}
