using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Gacha;
using EggLink.DanhengServer.Database.Inventory;
using EggLink.DanhengServer.Enums;
using EggLink.DanhengServer.Enums.Item;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.PlayerSync;
using EggLink.DanhengServer.Proto;
using GachaInfo = EggLink.DanhengServer.Database.Gacha.GachaInfo;
using System.Linq;
using EggLink.DanhengServer.Util;
namespace EggLink.DanhengServer.GameServer.Game.Gacha;

public class GachaManager : BasePlayerManager
{
    public GachaManager(PlayerInstance player) : base(player)
    {
        GachaData = DatabaseHelper.Instance!.GetInstanceOrCreateNew<GachaData>(player.Uid);

        if (GachaData.GachaHistory.Count >= 50)
            GachaData.GachaHistory.RemoveRange(0, GachaData.GachaHistory.Count - 50);

        foreach (var order in GameData.DecideAvatarOrderData.Values.ToList().OrderBy(x => -x.Order))
        {
            if (GachaData.GachaDecideOrder.Contains(order.ItemID)) continue;
            GachaData.GachaDecideOrder.Add(order.ItemID);
        }
    }

    public GachaData GachaData { get; }

    // --- 基础池获取方法 (保持不变) ---
    public List<int> GetPurpleAvatars()
    {
        var purpleAvatars = new List<int>();
        foreach (var avatar in GameData.AvatarConfigData.Values)
            if (avatar.Rarity == RarityEnum.CombatPowerAvatarRarityType4 &&
                !(GameData.MultiplePathAvatarConfigData.ContainsKey(avatar.AvatarID) &&
                  GameData.MultiplePathAvatarConfigData[avatar.AvatarID].BaseAvatarID != avatar.AvatarID) &&
                avatar.MaxRank > 0)
                purpleAvatars.Add(avatar.AvatarID);
        return purpleAvatars;
    }

    public List<int> GetGoldAvatars() => [1003, 1004, 1101, 1107, 1104, 1209, 1211];

    public List<int> GetAllGoldAvatars()
    {
        var avatars = new List<int>();
        foreach (var avatar in GameData.AvatarConfigData.Values)
            if (avatar.Rarity == RarityEnum.CombatPowerAvatarRarityType5)
                avatars.Add(avatar.AvatarID);
        return avatars;
    }

    public List<int> GetBlueWeapons()
    {
        var blueWeapons = new List<int>();
        foreach (var weapon in GameData.EquipmentConfigData.Values)
            if (weapon.Rarity == RarityEnum.CombatPowerLightconeRarity3)
                blueWeapons.Add(weapon.EquipmentID);
        return blueWeapons;
    }

    public List<int> GetPurpleWeapons()
    {
        var purpleWeapons = new List<int>();
        foreach (var weapon in GameData.EquipmentConfigData.Values)
            if (weapon.Rarity == RarityEnum.CombatPowerLightconeRarity4)
                purpleWeapons.Add(weapon.EquipmentID);
        return purpleWeapons;
    }

    public List<int> GetGoldWeapons() => [23000, 23002, 23003, 23004, 23005, 23012, 23013];

    public List<int> GetAllGoldWeapons()
    {
        var weapons = new List<int>();
        foreach (var weapon in GameData.EquipmentConfigData.Values)
            if (weapon.Rarity == RarityEnum.CombatPowerLightconeRarity5)
                weapons.Add(weapon.EquipmentID);
        return weapons;
    }

    public int GetRarity(int itemId)
    {
        if (GetAllGoldAvatars().Contains(itemId) || GetAllGoldWeapons().Contains(itemId)) return 5;
        if (GetPurpleAvatars().Contains(itemId) || GetPurpleWeapons().Contains(itemId)) return 4;
        if (GetBlueWeapons().Contains(itemId)) return 3;
        return 0;
    }

    public int GetType(int itemId)
    {
        if (GetAllGoldAvatars().Contains(itemId) || GetPurpleAvatars().Contains(itemId)) return 1;
        if (GetAllGoldWeapons().Contains(itemId) || GetPurpleWeapons().Contains(itemId) ||
            GetBlueWeapons().Contains(itemId)) return 2;
        return 0;
    }

public async ValueTask<DoGachaScRsp?> DoGacha(int bannerId, int times)
    {
        // 1. 日志与卡池验证
        Console.WriteLine($"\n[GACHA_DEBUG] 收到抽卡请求 -> UID: {Player.Uid} | Banner: {bannerId} | Times: {times}");

        var banner = GameData.BannersConfig.Banners.Find(x => x.GachaId == bannerId);
        if (banner == null) return null;

        if (bannerId == 4001 && (times != 10 || GachaData.NewbieGachaCount > 50))
        {
            Console.WriteLine("[GACHA_WARNING] 新手池请求非法（非10连或已抽满）");
            return null;
        }

        // 2. 补票与扣费逻辑
        int actualCost = (bannerId == 4001 && times == 10) ? 8 : times;
        int ticketId = (int)banner.GachaType.GetCostItemId();

        if (Player.InventoryManager!.GetItemCount(ticketId) < actualCost)
        {
            int deficit = actualCost - Player.InventoryManager.GetItemCount(ticketId);
            var costHcoin = await Player.InventoryManager.RemoveItem(1, deficit * 160, sync: false);
            if (costHcoin == null) return null;
            await Player.InventoryManager.AddItem(ticketId, deficit, sync: false);
        }
        await Player.InventoryManager.RemoveItem(ticketId, actualCost, sync: false);

        // 3. 执行抽卡内核
        var decideItem = GachaData.GachaDecideOrder.Count >= 7 ? GachaData.GachaDecideOrder.GetRange(0, 7) : GachaData.GachaDecideOrder;
        var items = new List<int>();
        for (var i = 0; i < times; i++)
        {
            var item = banner.DoGacha(decideItem, GetPurpleAvatars(), GetPurpleWeapons(), GetGoldWeapons(),
                GetBlueWeapons(), GachaData, Player.Uid);
            if (item == 0) break;
            items.Add(item);
        }

        // 4. 【核心补全】物品发放、副产物计算与同步包构造
        var gachaItems = new List<GachaItem>();
        var syncItems = new List<ItemData> { 
            new ItemData { ItemId = ticketId, Count = (int)Player.InventoryManager!.GetItemCount(ticketId) },
            new ItemData { ItemId = 1, Count = (int)Player.Data.Hcoin }
        };

        foreach (var item in items)
        {
            var rarity = GetRarity(item);
            var gachaItem = new GachaItem { GachaItem_ = new Item { ItemId = (uint)item, Num = 1, Level = 1, Rank = 1 } };
            gachaItem.TransferItemList = new ItemList();

            int star = 0, dirt = 0;
            if (rarity == 5) star = 20; 
            else if (rarity == 4) star = 8;
            else dirt = 20;

            GachaData.GachaHistory.Add(new GachaInfo { GachaId = bannerId, ItemId = item, Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });

            // --- 【补全逻辑】处理物品入库与转换 ---
            if (GameData.ItemConfigData[item].ItemMainType == ItemMainTypeEnum.AvatarCard)
            {
                var avatar = Player.AvatarManager?.GetFormalAvatar(item);
                if (avatar == null)
                {
                    // 初次获得角色
                    await Player.AvatarManager!.AddAvatar(item, isGacha: true);
                    // 发放头像 (200000 偏移)
                    await Player.InventoryManager.AddItem(200000 + item, 1, false, sync: false);
                    UpdateSyncList(syncItems, 200000 + item);
                }
                else
                {
                    // 重复获得角色 -> 转化为星魂
                    var rankUpItemId = item + 10000;
                    var rankUpItemCount = Player.InventoryManager!.GetItemCount(rankUpItemId);
                    
                    // 检查是否达到 6 魂 (星魂数 + 当前命座 >= 6)
                    bool isFullRank = avatar.PathInfos[item].Rank + rankUpItemCount >= 6;

                    if (isFullRank)
                    {
                        // 满魂转化：增加额外星芒奖励
                        star += (rarity == 5) ? 60 : 12;
                        // 2. 【核心修复】：只有 5 星满魂才额外给 281
						if (rarity == 5)
						{
						await Player.InventoryManager.AddItem(281, 1, false, sync: false);
						UpdateSyncList(syncItems, 281);
						gachaItem.TransferItemList.ItemList_.Add(new Item { ItemId = 281, Num = 1 });
						}
                    }
                    else
                    {
                        // 未满魂：发放星魂入库
                        await Player.InventoryManager.AddItem(rankUpItemId, 1, false, sync: false);
                        UpdateSyncList(syncItems, rankUpItemId);
                        gachaItem.TransferItemList.ItemList_.Add(new Item { ItemId = (uint)rankUpItemId, Num = 1 });
                    }
                }
            }
            else
            {
                // 武器直接发放
                await Player.InventoryManager.AddItem(item, 1, false, sync: false);
            }

            // D. 发放副产物并构造同步列表
            if (star > 0)
            {
                await Player.InventoryManager.AddItem(252, star, false, sync: false);
                gachaItem.TokenItem ??= new ItemList();
                gachaItem.TokenItem.ItemList_.Add(new Item { ItemId = 252, Num = (uint)star });
                UpdateSyncList(syncItems, 252);
            }
            if (dirt > 0)
            {
                await Player.InventoryManager.AddItem(251, dirt, false, sync: false);
                gachaItem.TokenItem ??= new ItemList();
                gachaItem.TokenItem.ItemList_.Add(new Item { ItemId = 251, Num = (uint)dirt });
                UpdateSyncList(syncItems, 251);
            }

            UpdateSyncList(syncItems, item);
            gachaItems.Add(gachaItem);
			Console.WriteLine($"[GACHA_DEBUG] 抽卡后数据库状态 -> NewbieGachaCount: {GachaData.NewbieGachaCount}");
        }

        // 5. 统一同步包
        await Player.SendPacket(new PacketPlayerSyncScNotify(syncItems));

        // 6. 构造回包
        var proto = new DoGachaScRsp  
        {  
            GachaId = (uint)bannerId,  
            GachaNum = (uint)times,
            Retcode = 0,
            GDIFAAHIFBH = (uint)GachaData.NewbieGachaCount,
            CeilingNum = (uint)GachaData.StandardCumulativeCount,
			PENILHGLHHM = (uint)GachaData.StandardCumulativeCount,	
            //KMNJNMJFGBG = (uint)GachaData.NewbieGachaCount
        };
        proto.GachaItemList.AddRange(gachaItems);

        //if (bannerId == 1001 || bannerId == 4001)
        //{
        //    await Player.SendPacket(new EggLink.DanhengServer.GameServer.Server.Packet.Send.Gacha.PacketGetGachaInfoScRsp(Player));
        //}

        return proto;
    }

    // 辅助方法：更新同步列表中的总量
    private void UpdateSyncList(List<ItemData> list, int itemId)
    {
        var existing = list.Find(x => x.ItemId == itemId);
        int count = (int)Player.InventoryManager!.GetItemCount(itemId);
        if (existing == null) list.Add(new ItemData { ItemId = itemId, Count = count });
        else existing.Count = count;
    }

  
    

    public GetGachaInfoScRsp ToProto()
    {
        var proto = new GetGachaInfoScRsp { GachaRandom = (uint)Random.Shared.Next(1000, 1999) };
        foreach (var banner in GameData.BannersConfig.Banners)
        {
            if (banner.GachaId == 4001 && GachaData.NewbieGachaCount > 50) continue;
            proto.GachaInfoList.Add(banner.ToInfo(GetGoldAvatars(), Player.Uid, GachaData));
        }
        return proto;
    }
}