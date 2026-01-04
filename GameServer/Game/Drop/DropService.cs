using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database.Inventory;
using EggLink.DanhengServer.Enums.Scene;

namespace EggLink.DanhengServer.GameServer.Game.Drop;

public class DropService
{
    // --- 遗器池完整定义 ---
    // 6开头：5星 (金色)
    private static readonly int[] Relic6Star = { 63015, 63016, 63025, 63026, 63035, 63036, 63045, 63046, 63055, 63056, 63065, 63066, 63075, 63076, 63085, 63086, 63095, 63096, 63105, 63106, 61131, 61132, 61133, 61134, 61141, 61142, 61143, 61144, 61151, 61152, 61153, 61154, 61161, 61162, 61163, 61164, 63115, 63116, 63125, 63126, 61171, 61172, 61173, 61174, 61181, 61182, 61183, 61184, 63135, 63136, 63145, 63146, 61191, 61192, 61193, 61194, 61201, 61202, 61203, 61204, 63155, 63156, 63165, 63166, 63175, 63176, 63185, 63186, 61211, 61212, 61213, 61214, 61221, 61222, 61223, 61224, 61231, 61232, 61233, 61234, 61241, 61242, 61243, 61244, 63195, 63196, 63205, 63206, 61251, 61252, 61253, 61254, 61261, 61262, 61263, 61264, 63215, 63216, 63225, 63226, 61271, 61272, 61273, 61274, 61281, 61282, 61283, 61284, 63235, 63236, 63245, 63246 };
    
    // 5开头：4星 (紫色)
    private static readonly int[] Relic5Star = { 53015, 53016, 53025, 53026, 53035, 53036, 53045, 53046, 53055, 53056, 53065, 53066, 53075, 53076, 53085, 53086, 53095, 53096, 53105, 53106, 51131, 51132, 51133, 51134, 51141, 51142, 51143, 51144, 51151, 51152, 51153, 51154, 51161, 51162, 51163, 51164, 53115, 53116, 53125, 53126, 51171, 51172, 51173, 51174, 51181, 51182, 51183, 51184, 53135, 53136, 53145, 53146, 51191, 51192, 51193, 51194, 51201, 51202, 51203, 51204, 53155, 53156, 53165, 53166, 53175, 53176, 53185, 53186, 51211, 51212, 51213, 51214, 51221, 51222, 51223, 51224, 51231, 51232, 51233, 51234, 51241, 51242, 51243, 51244, 53195, 53196, 53205, 53206, 51251, 51252, 51253, 51254, 51261, 51262, 51263, 51264, 53215, 53216, 53225, 53226, 51271, 51272, 51273, 51274, 51281, 51282, 51283, 51284, 53235, 53236, 53245, 53246, 55001, 55002, 55003, 55004, 55005, 55006 };
    
    // 4开头：3星
    private static readonly int[] Relic4Star = { 43015, 43016, 43025, 43026, 43035, 43036, 43045, 43046, 43055, 43056, 43065, 43066, 43075, 43076, 43085, 43086, 43095, 43096, 43105, 43106, 41131, 41132, 41133, 41134, 41141, 41142, 41143, 41144, 41151, 41152, 41153, 41154, 41161, 41162, 41163, 41164, 43115, 43116, 43125, 43126, 41171, 41172, 41173, 41174, 41181, 41182, 41183, 41184, 43135, 43136, 43145, 43146, 41191, 41192, 41193, 41194, 41201, 41202, 41203, 41204, 43155, 43156, 43165, 43166, 43175, 43176, 43185, 43186, 41211, 41212, 41213, 41214, 41221, 41222, 41223, 41224, 41231, 41232, 41233, 41234, 41241, 41242, 41243, 41244, 43195, 43196, 43205, 43206, 41251, 41252, 41253, 41254, 41261, 41262, 41263, 41264, 43215, 43216, 43225, 43226, 41271, 41272, 41273, 41274, 41281, 41282, 41283, 41284, 43235, 43236, 43245, 43246 };
    
    // 3开头：2星
    private static readonly int[] Relic2Star = { 33015, 33016, 33025, 33026, 33035, 33036, 33045, 33046, 33055, 33056, 33065, 33066, 33075, 33076, 33085, 33086, 33095, 33096, 33105, 33106, 31131, 31132, 31133, 31134, 31141, 31142, 31143, 31144, 31151, 31152, 31153, 31154, 31161, 31162, 31163, 31164, 33115, 33116, 33125, 33126, 31171, 31172, 31173, 31174, 31181, 31182, 31183, 31184, 33135, 33136, 33145, 33146, 31191, 31192, 31193, 31194, 31201, 31202, 31203, 31204, 33155, 33156, 33165, 33166, 33175, 33176, 33185, 33186, 31211, 31212, 31213, 31214, 31221, 31222, 31223, 31224, 31231, 31232, 31233, 31234, 31241, 31242, 31243, 31244, 33195, 33196, 33205, 33206, 31251, 31252, 31253, 31254, 31261, 31262, 31263, 31264, 33215, 33216, 33225, 33226, 31271, 31272, 31273, 31274, 31281, 31282, 31283, 31284, 33235, 33236, 33245, 33246 };

    public static List<ItemData> CalculateDropsFromProp(int chestId)
    {
        var items = new List<ItemData>();
        var chest = GameData.MazeChestData.GetValueOrDefault(chestId);
        if (chest == null) return items;

        var level = ChestTypeEnum.CHEST_NONE;

        if (chest.ChestType.Contains(ChestTypeEnum.CHEST_HIGH_LEVEL))
            level = ChestTypeEnum.CHEST_HIGH_LEVEL;
        else if (chest.ChestType.Contains(ChestTypeEnum.CHEST_MIDDLE_LEVEL))
            level = ChestTypeEnum.CHEST_MIDDLE_LEVEL;
        else if (chest.ChestType.Contains(ChestTypeEnum.CHEST_LOW_LEVEL))
            level = ChestTypeEnum.CHEST_LOW_LEVEL;

        var world = ChestTypeEnum.CHEST_NONE;
        if (chest.ChestType.Contains(ChestTypeEnum.CHEST_WORLD_ONE))
            world = ChestTypeEnum.CHEST_WORLD_ONE;
        else if (chest.ChestType.Contains(ChestTypeEnum.CHEST_WORLD_TWO))
            world = ChestTypeEnum.CHEST_WORLD_TWO;
        else if (chest.ChestType.Contains(ChestTypeEnum.CHEST_WORLD_THREE))
            world = ChestTypeEnum.CHEST_WORLD_THREE;
        else if (chest.ChestType.Contains(ChestTypeEnum.CHEST_WORLD_ZERO))
            world = ChestTypeEnum.CHEST_WORLD_ZERO;
        else if (chest.ChestType.Contains(ChestTypeEnum.CHEST_WORLD_FOUR))
            world = ChestTypeEnum.CHEST_WORLD_FOUR;

        // --- 1. 星穹 (修改后的数值 1000/2000/4000) ---
        items.Add(new ItemData
        {
            ItemId = 1,
            Count = level switch
            {
                ChestTypeEnum.CHEST_LOW_LEVEL => 1000,
                ChestTypeEnum.CHEST_MIDDLE_LEVEL => 2000,
                ChestTypeEnum.CHEST_HIGH_LEVEL => 4000,
                _ => 1000
            }
        });

        // --- 2. 基础材料 (原本的逻辑) ---
        items.Add(new ItemData { ItemId = 212, Count = Random.Shared.Next(3, 6) });
        items.Add(new ItemData { ItemId = 222, Count = Random.Shared.Next(3, 6) });
        items.Add(new ItemData { ItemId = 232, Count = Random.Shared.Next(3, 6) });

        // --- 3. 地区货币 (原本的完整判断) ---
        switch (world)
        {
            case ChestTypeEnum.CHEST_WORLD_ONE:
                items.Add(new ItemData { ItemId = 120001, Count = level switch { ChestTypeEnum.CHEST_LOW_LEVEL => 20, ChestTypeEnum.CHEST_MIDDLE_LEVEL => 40, ChestTypeEnum.CHEST_HIGH_LEVEL => 60, _ => 20 } });
                break;
            case ChestTypeEnum.CHEST_WORLD_TWO:
                items.Add(new ItemData { ItemId = 120002, Count = level switch { ChestTypeEnum.CHEST_LOW_LEVEL => 5, ChestTypeEnum.CHEST_MIDDLE_LEVEL => 10, ChestTypeEnum.CHEST_HIGH_LEVEL => 20, _ => 5 } });
                break;
            case ChestTypeEnum.CHEST_WORLD_THREE:
                items.Add(new ItemData { ItemId = 120003, Count = level switch { ChestTypeEnum.CHEST_LOW_LEVEL => 60, ChestTypeEnum.CHEST_MIDDLE_LEVEL => 90, ChestTypeEnum.CHEST_HIGH_LEVEL => 120, _ => 60 } });
                break;
            case ChestTypeEnum.CHEST_WORLD_ZERO:
                items.Add(new ItemData { ItemId = 120000, Count = level switch { ChestTypeEnum.CHEST_LOW_LEVEL => 10, ChestTypeEnum.CHEST_MIDDLE_LEVEL => 20, ChestTypeEnum.CHEST_HIGH_LEVEL => 50, _ => 10 } });
                break;
            case ChestTypeEnum.CHEST_WORLD_FOUR:
                items.Add(new ItemData { ItemId = 120004, Count = level switch { ChestTypeEnum.CHEST_LOW_LEVEL => 60, ChestTypeEnum.CHEST_MIDDLE_LEVEL => 90, ChestTypeEnum.CHEST_HIGH_LEVEL => 120, _ => 60 } });
                break;
        }

        // --- 4. 遗器掉落逻辑 (3箱1金, 2箱1紫, 1箱1蓝) ---
        int attempts = level switch
        {
            ChestTypeEnum.CHEST_LOW_LEVEL => 1,
            ChestTypeEnum.CHEST_MIDDLE_LEVEL => 2,
            ChestTypeEnum.CHEST_HIGH_LEVEL => 3,
            _ => 1
        };

        for (int i = 0; i < attempts; i++)
        {
            // 金色 (33% 概率)
            if (Random.Shared.Next(0, 100) < 33) 
                items.Add(new ItemData { ItemId = Relic6Star[Random.Shared.Next(Relic6Star.Length)], Count = 1 });

            // 紫色 (50% 概率)
            if (Random.Shared.Next(0, 100) < 50) 
                items.Add(new ItemData { ItemId = Relic5Star[Random.Shared.Next(Relic5Star.Length)], Count = 1 });

            // 蓝色保底 (100% 概率，在3星和2星间随机)
            int blueId = Random.Shared.Next(0, 2) == 0 
                ? Relic4Star[Random.Shared.Next(Relic4Star.Length)] 
                : Relic2Star[Random.Shared.Next(Relic2Star.Length)];
            items.Add(new ItemData { ItemId = blueId, Count = 1 });
        }

        // --- 5. 信用点 (原本的逻辑) ---
        items.Add(new ItemData
        {
            ItemId = 2,
            Count = level switch
            {
                ChestTypeEnum.CHEST_LOW_LEVEL => 750,
                ChestTypeEnum.CHEST_MIDDLE_LEVEL => 3700,
                ChestTypeEnum.CHEST_HIGH_LEVEL => 6000,
                _ => 750
            }
        });

        return items;
    }
}