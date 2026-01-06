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
        var banner = GameData.BannersConfig.Banners.Find(x => x.GachaId == bannerId);
        if (banner == null) return null;

        Player.InventoryManager?.RemoveItem(banner.GachaType.GetCostItemId(), times);
        var decideItem = GachaData.GachaDecideOrder.Count >= 7 ? GachaData.GachaDecideOrder.GetRange(0, 7) : GachaData.GachaDecideOrder;
        
        var items = new List<int>();
        for (var i = 0; i < times; i++)
        {
            var item = banner.DoGacha(decideItem, GetPurpleAvatars(), GetPurpleWeapons(), GetGoldWeapons(),
                GetBlueWeapons(), GachaData, Player.Uid);
            items.Add(item);
        }

        var gachaItems = new List<GachaItem>();
        var syncItems = new List<ItemData>();

        foreach (var item in items)
        {
            var dirt = 0;
            var star = 0;
            var rarity = GetRarity(item);

            GachaData.GachaHistory.Add(new GachaInfo
            {
                GachaId = bannerId,
                ItemId = item,
                Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            var gachaItem = new GachaItem();

            if (rarity == 5)
            {
                var type = GetType(item);
                if (type == 1)
                {
                    var avatar = Player.AvatarManager?.GetFormalAvatar(item);
                    if (avatar != null)
                    {
                        star += 40;
                        var rankUpItemId = item + 10000;
                        var rankUpItem = Player.InventoryManager!.GetItem(rankUpItemId);
                        if (avatar.PathInfos[item].Rank + (rankUpItem?.Count ?? 0) >= 6)
                        {
                            star += 60;
                            // 满魂发放 281
                            var item281 = await Player.InventoryManager!.AddItem(281, 1, false, sync: false, returnRaw: true);
                            if (item281 != null)
                            {
                                var old = syncItems.Find(x => x.ItemId == 281);
                                if (old == null) syncItems.Add(item281);
                                else old.Count = item281.Count;
                            }
                            var extraTransfer = new ItemList();
                            extraTransfer.ItemList_.Add(new Item { ItemId = 281, Num = 1 });
                            gachaItem.TransferItemList = extraTransfer;
                        }
                        else
                        {
                            var dupeItem = new ItemList();
                            dupeItem.ItemList_.Add(new Item { ItemId = (uint)rankUpItemId, Num = 1 });
                            gachaItem.TransferItemList = dupeItem;
                        }
                    }
                }
                else star += 20;
            }
            else if (rarity == 4)
            {
                var type = GetType(item);
                if (type == 1)
                {
                    var avatar = Player.AvatarManager?.GetFormalAvatar(item);
                    if (avatar != null)
                    {
                        star += 8;
                        var rankUpItemId = item + 10000;
                        var rankUpItem = Player.InventoryManager!.GetItem(rankUpItemId);
                        if (avatar.PathInfos[item].Rank + (rankUpItem?.Count ?? 0) >= 6) star += 12;
                        else
                        {
                            var dupeItem = new ItemList();
                            dupeItem.ItemList_.Add(new Item { ItemId = (uint)rankUpItemId, Num = 1 });
                            gachaItem.TransferItemList = dupeItem;
                        }
                    }
                }
                else star += 8;
            }
            else dirt += 20;

            // 发放抽到的物品
            if (GameData.ItemConfigData[item].ItemMainType == ItemMainTypeEnum.AvatarCard &&
                Player.AvatarManager!.GetFormalAvatar(item) == null)
            {   // 1. 发放角色本身
                await Player.AvatarManager!.AddAvatar(item, isGacha: true);
				// 2. 发放对应的头像 (规律: 200000 + 角色ID)
                int headIconId = 200000 + item;
    
               // 检查该头像是否存在（可以根据配置校验，或者直接尝试添加）
               // 假设头像在 InventoryManager 中作为一种 Item 处理
               var headIconItem = await Player.InventoryManager!.AddItem(headIconId, 1, false, sync: false, returnRaw: true);
               if (headIconItem != null)
               {
               syncItems.Add(headIconItem);
               }
            }
            else
            {
                var i = await Player.InventoryManager!.AddItem(item, 1, false, sync: false, returnRaw: true);
                if (i != null) syncItems.Add(i);
            }

            // --- 直接处理 251 同步 ---
            if (dirt > 0)
            {
                var it = await Player.InventoryManager!.AddItem(251, dirt, false, sync: false, returnRaw: true);
                if (it != null)
                {
                    var old = syncItems.Find(x => x.ItemId == 251);
                    if (old == null) syncItems.Add(it);
                    else old.Count = it.Count;
                }
                gachaItem.TokenItem ??= new ItemList();
                gachaItem.TokenItem.ItemList_.Add(new Item { ItemId = 251, Num = (uint)dirt });
            }

            // --- 直接处理 252 同步 ---
            if (star > 0)
            {
                var it = await Player.InventoryManager!.AddItem(252, star, false, sync: false, returnRaw: true);
                if (it != null)
                {
                    var old = syncItems.Find(x => x.ItemId == 252);
                    if (old == null) syncItems.Add(it);
                    else old.Count = it.Count;
                }
                gachaItem.TokenItem ??= new ItemList();
                gachaItem.TokenItem.ItemList_.Add(new Item { ItemId = 252, Num = (uint)star });
            }

            gachaItem.GachaItem_ = new Item { ItemId = (uint)item, Num = 1, Level = 1, Rank = 1 };
            gachaItem.TransferItemList ??= new ItemList();
            gachaItems.Add(gachaItem);
        }

        await Player.SendPacket(new PacketPlayerSyncScNotify(syncItems));
        var proto = new DoGachaScRsp { GachaId = (uint)bannerId, GachaNum = (uint)times };
        proto.GachaItemList.AddRange(gachaItems);

        return proto;
    }

    public GetGachaInfoScRsp ToProto()
    {
        var proto = new GetGachaInfoScRsp { GachaRandom = (uint)Random.Shared.Next(1000, 1999) };
        foreach (var banner in GameData.BannersConfig.Banners)
        {
            proto.GachaInfoList.Add(banner.ToInfo(GetGoldAvatars(), Player.Uid));
        }
        return proto;
    }
}