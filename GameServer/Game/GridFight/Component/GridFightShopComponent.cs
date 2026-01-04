using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Component;

public class GridFightShopComponent(GridFightInstance inst) : BaseGridFightComponent(inst)
{
    public GridFightShopInfoPb Data { get; set; } = new()
    {
        RefreshCost = 2,
        FreeRefreshCount = 1
    };

    public static uint GetGoodsPrice(uint rarity, uint tier)
    {
        return GameData.GridFightShopPriceData.GetValueOrDefault(rarity)
            ?.BuyGoldList[(int)(tier - 1)] ?? 1;
    }

    public async ValueTask LockGoods(bool locked, bool sendPacket = true)
    {
        var curLevel = Inst.GetComponent<GridFightBasicComponent>().Data.CurLevel;
        Data.ShopLocked = locked;

        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(new GridFightShopSyncData(GridFightSrc.KGridFightSrcNone, Data, curLevel)));
        }
    }

    public async ValueTask<Retcode> BuyGoods(List<uint> indexes, bool sendPacket = true)
    {
        var avatarComp = Inst.GetComponent<GridFightRoleComponent>();

        // check pos
        //if (!avatarComp.HasAnyEmptyPos()) return Retcode.RetGridFightNoEmptyPos;

        var curLevel = Inst.GetComponent<GridFightBasicComponent>().Data.CurLevel;

        var targetGoods = indexes
            .Where(x => x < Data.ShopItems.Count)
            .Select(x => Data.ShopItems[(int)x])
            .ToList();

        var totalCost = (uint)targetGoods.Select(x => x.Cost).Sum(x => x);

        // COST
        var code = await Inst.GetComponent<GridFightBasicComponent>().UpdateGoldNum((int)-totalCost, false, GridFightSrc.KGridFightSrcBuyGoods);
        if (code != Retcode.RetSucc)
        {
            return code;
        }

        // GIVE ITEMS
        List<BaseGridFightSyncData> syncs = [];
        foreach (var item in targetGoods)
        {
            if (item.ItemTypeCase == GridFightShopItemPb.ItemTypeOneofCase.RoleItem)
            {
                syncs.AddRange(await avatarComp.AddAvatar(item.RoleItem.RoleId, item.RoleItem.Tier, false,
                    param: (uint)Data.ShopItems.IndexOf(item)));
            }
            else
            {
                // TODO other item types
            }
        }

        // REMOVE ITEMS FROM SHOP
        foreach (var index in indexes)
        {
            Data.ShopItems[(int)index].SoldOut = true;
        }

        if (sendPacket)
        {
            syncs.Insert(0, new GridFightGoldSyncData(GridFightSrc.KGridFightSrcBuyGoods, Inst.GetComponent<GridFightBasicComponent>().Data));
            syncs.Add(new GridFightShopSyncData(GridFightSrc.KGridFightSrcBuyGoods, Data, curLevel));

            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
        }

        return Retcode.RetSucc;
    }

    public void AddGoods(uint num, uint curLevel)
    {
        var rules = GameData.GridFightPlayerLevelData.GetValueOrDefault(curLevel)?.RarityWeights ??
                    [100, 0, 0, 0, 0];
        // generate items
        for (var i = 0; i < num; i++)
        {
            // select rarity
            var rand = (uint)Random.Shared.Next(1, 101);
            var targetRarity = 0;
            for (var j = 0; j < 5; j++)
            {
                if (rand <= rules[j])
                {
                    targetRarity = j + 1;
                    break;
                }
                rand -= rules[j];
            }

            // get item pool
            var pool = GameData.GridFightRoleBasicInfoData.Values
                .Where(x => x.IsInPool && x.Rarity == targetRarity).ToList();

            var target = pool.RandomElement();

            var tier = 1u;

            var cost = GetGoodsPrice(target.Rarity, tier);

            Data.ShopItems.Add(new GridFightShopItemPb
            {
                Cost = cost,
                RoleItem = new GridFightShopRoleItemPb
                {
                    RoleId = target.ID,
                    Tier = tier
                }
            });
        }
    }

    public async ValueTask<Retcode> RefreshShop(bool isEnterSection, bool sendPacket = true)
    {
        if (!isEnterSection)
        {
            if (Data.FreeRefreshCount > 0)
                Data.FreeRefreshCount--;
            else
            {
                // cost
                var code = await Inst.GetComponent<GridFightBasicComponent>().UpdateGoldNum((int)-Data.RefreshCost, src:GridFightSrc.KGridFightSrcManualRefreshGoods);
                if (code != Retcode.RetSucc)
                {
                    return code;
                }

                Data.RefreshCost = 2;
            }
        }
        else
        {
            Data.RefreshCost = 2;
            Data.FreeRefreshCount++;
            if (Data.ShopLocked)
            {
                await LockGoods(false, false);
                return Retcode.RetGridFightShopLocked;
            }
        }

        // refresh
        var curLevel = Inst.GetComponent<GridFightBasicComponent>().Data.CurLevel;
        Data.ShopItems.Clear();

        AddGoods(5, curLevel);

        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(new GridFightShopSyncData(GridFightSrc.KGridFightSrcManualRefreshGoods, Data, curLevel)));
        }

        return Retcode.RetSucc;
    }

    public override GridFightGameInfo ToProto()
    {
        var rarity = 1u;
        var rules = GameData.GridFightPlayerLevelData
                        .GetValueOrDefault(Inst.GetComponent<GridFightBasicComponent>().Data.CurLevel)?.RarityWeights ??
                    [100, 0, 0, 0, 0];

        return new GridFightGameInfo
        {
            GridShopInfo = new GridFightGameShopInfo
            {
                ShopIsLocked = Data.ShopLocked,
                GridFightShopRandomRule = new GridFightShopRandomRuleInfo
                {
                    GridFightShopRuleList = { rules.Select(x => new GridFightShopRandomRule
                    {
                        ShopItemWeight = x,
                        ShopItemRarity = rarity++
                    }) }
                },
                ShopGoodsList = { Data.ShopItems.Select(x => x.ToProto()) },
                ShopFreeRefreshCount = Data.FreeRefreshCount,
                ShopRefreshCost = Data.RefreshCost
            }
        };
    }
}

public static class GridFightShopInfoPbExtensions
{
    public static GridFightShopGoodsInfo ToProto(this GridFightShopItemPb info)
    {
        var proto = new GridFightShopGoodsInfo
        {
            IsSoldOut = info.SoldOut,
            ShopGoodsPrice = info.Cost
        };

        if (info.ItemTypeCase == GridFightShopItemPb.ItemTypeOneofCase.RoleItem)
        {
            proto.RoleGoodsInfo = new GridFightRoleGoodsInfo
            {
                RoleBasicId = info.RoleItem.RoleId,
                Tier = info.RoleItem.Tier
            };
        }
        else
        {
            // TODO
        }

        return proto;
    }

    public static GridFightShopSyncInfo ToSyncInfo(this GridFightShopInfoPb info, uint level)
    {
        var rarity = 1u;
        var rules = GameData.GridFightPlayerLevelData
                        .GetValueOrDefault(level)?.RarityWeights ??
                    [100, 0, 0, 0, 0];

        return new GridFightShopSyncInfo
        {
            ShopIsLocked = info.ShopLocked,
            GridFightShopRandomRule = new GridFightShopRandomRuleInfo
            {
                GridFightShopRuleList =
                {
                    rules.Select(x => new GridFightShopRandomRule
                    {
                        ShopItemWeight = x,
                        ShopItemRarity = rarity++
                    })
                }
            },
            ShopGoodsList = { info.ShopItems.Select(x => x.ToProto()) },
            ShopFreeRefreshCount = info.FreeRefreshCount,
            ShopRefreshCost = info.RefreshCost
        };
    }
}