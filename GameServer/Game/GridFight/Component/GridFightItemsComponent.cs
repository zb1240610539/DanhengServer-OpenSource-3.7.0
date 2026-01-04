using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.Enums.GridFight;
using EggLink.DanhengServer.GameServer.Game.GridFight.PendingAction;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Component;

public class GridFightItemsComponent(GridFightInstance inst) : BaseGridFightComponent(inst)
{
    public GridFightItemsInfoPb Data { get; set; } = new();

    #region Add & Remove & Craft

    public async ValueTask<(GridFightEquipmentItemPb?, List<BaseGridFightSyncData>)> AddEquipment(uint equipmentId, GridFightSrc src = GridFightSrc.KGridFightSrcNone, bool sendPacket = true, uint groupId = 0, params uint[] param)
    {
        if (!GameData.GridFightEquipmentData.ContainsKey(equipmentId))
            return (null, []);

        var roleComp = Inst.GetComponent<GridFightRoleComponent>();
        var info = new GridFightEquipmentItemPb
        {
            ItemId = equipmentId,
            UniqueId = ++roleComp.Data.CurUniqueId
        };

        Data.EquipmentItems.Add(info);
        var syncData = new GridFightAddGameItemSyncData(src, [info], [], groupId, param);

        if (sendPacket)
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncData));

        return (info, [syncData]);
    }

    public async ValueTask<List<BaseGridFightSyncData>> UpdateConsumable(uint consumableId, int count, GridFightSrc src = GridFightSrc.KGridFightSrcNone, bool sendPacket = true, uint groupId = 0, params uint[] param)
    {
        if (!GameData.GridFightConsumablesData.ContainsKey(consumableId) || count == 0)
            return [];

        var existingItem = Data.ConsumableItems.FirstOrDefault(x => x.ItemId == consumableId);

        var isRemove = false;
        var isUpdate = false;

        if (existingItem != null)
        {
            isUpdate = true;
            if (count < 0)
            {
                count = (int)-Math.Min(existingItem.Count, -count);
                existingItem.Count -= (uint)-count;
                if (existingItem.Count == 0)
                {
                    Data.ConsumableItems.Remove(existingItem);
                    isRemove = true;
                }
            }
            else
            {
                existingItem.Count += (uint)count;
            }
        }
        else
        {
            if (count < 0) return [];

            var info = new GridFightConsumableItemPb
            {
                ItemId = consumableId,
                Count = (uint)count  // safe
            };

            Data.ConsumableItems.Add(info);
            existingItem = info;
        }

        BaseGridFightSyncData syncData = isRemove
            ? new GridFightRemoveGameItemSyncData(src, [], [existingItem.ToUpdateInfo(count)], groupId, param)
            : isUpdate
                ? new GridFightUpdateGameItemSyncData(src, [], [existingItem.ToUpdateInfo(count)], groupId, param)
                : new GridFightAddGameItemSyncData(src, [], [existingItem.ToUpdateInfo(count)], groupId, param);

        if (sendPacket)
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncData));

        return [syncData];
    }

    public async ValueTask<List<BaseGridFightSyncData>> RemoveEquipment(uint uniqueId, GridFightSrc src = GridFightSrc.KGridFightSrcNone, bool sendPacket = true, params uint[] param)
    {
        var existingItem = Data.EquipmentItems.FirstOrDefault(x => x.UniqueId == uniqueId);

        if (existingItem == null)
            return [];

        Data.EquipmentItems.Remove(existingItem);

        List<BaseGridFightSyncData> syncDatas =
        [
            new GridFightRemoveGameItemSyncData(src, [existingItem], [], 0, param)
        ];

        // if equipped
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();
        foreach (var role in roleComp.Data.Roles)
        {
            if (!role.EquipmentIds.Contains(existingItem.ItemId)) continue;

            role.EquipmentIds.Remove(existingItem.ItemId);
            syncDatas.Add(new GridFightRoleUpdateSyncData(src, role.Clone(), 0, param));
        }

        if (sendPacket)
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncDatas));

        return syncDatas;
    }

    public async ValueTask<List<BaseGridFightSyncData>> CraftEquipment(uint targetEquipId, List<uint> materials)
    {
        List<BaseGridFightSyncData> syncDatas = [];

        // remove materials
        foreach (var matId in materials)
        {
            syncDatas.AddRange(await RemoveEquipment(matId, GridFightSrc.KGridFightSrcCraftEquip, false));
        }

        // add crafted equipment
        var addEquipDatas = await AddEquipment(targetEquipId, GridFightSrc.KGridFightSrcCraftEquip, false);
        syncDatas.AddRange(addEquipDatas.Item2);

        // if auto equip to a role
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();
        var roleUnique =
            (syncDatas.FirstOrDefault(x => x is GridFightRoleUpdateSyncData) as GridFightRoleUpdateSyncData)?.Role
            .UniqueId ?? 0;

        if (roleUnique != 0 && addEquipDatas.Item1 != null)
        {
            syncDatas.AddRange(await roleComp.DressRole(roleUnique, addEquipDatas.Item1.UniqueId, GridFightSrc.KGridFightSrcCraftEquip, false));
        }

        // sync
        if (syncDatas.Count > 0)
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncDatas));

        return syncDatas;
    }

    #endregion

    #region Rewards

    public async ValueTask<List<BaseGridFightSyncData>> TakeDrop(List<GridFightDropItemInfo> drops,
        bool sendPacket = false, GridFightSrc src = GridFightSrc.KGridFightSrcNone, uint groupId = 0,
        params uint[] param)
    {
        var syncs = new List<BaseGridFightSyncData>();
        var basicComp = Inst.GetComponent<GridFightBasicComponent>();
        var shopComp = Inst.GetComponent<GridFightShopComponent>();
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();
        var orbComp = Inst.GetComponent<GridFightOrbComponent>();

        foreach (var item in drops)
        {
            // take drop
            switch (item.DropType)
            {
                case GridFightDropType.Coin:
                {
                    await basicComp.UpdateGoldNum((int)item.Num, false);
                    syncs.Add(new GridFightGoldSyncData(src, basicComp.Data, groupId, param));
                    break;
                }
                case GridFightDropType.Exp:
                {
                    await basicComp.AddLevelExp(item.Num, false);
                    syncs.Add(new GridFightPlayerLevelSyncData(src, basicComp.Data, groupId, param));
                    break;
                }
                case GridFightDropType.Refresh:
                {
                    shopComp.Data.FreeRefreshCount += item.Num;
                    syncs.Add(new GridFightShopSyncData(src, shopComp.Data, basicComp.Data.CurLevel, groupId, param));
                    break;
                }
                case GridFightDropType.Role:
                {
                    syncs.AddRange(await roleComp.AddAvatar(item.DropItemId, item.DisplayValue.Tier, false, true,
                        src, groupId, 0, null, param));
                    break;
                }
                case GridFightDropType.Item:
                {
                    // consumable or equipment or forge
                    if (GameData.GridFightConsumablesData.ContainsKey(item.DropItemId))
                    {
                        syncs.AddRange(await UpdateConsumable(item.DropItemId, (int)item.Num, src, false, groupId,
                            param));
                    }
                    else if (GameData.GridFightEquipmentData.ContainsKey(item.DropItemId))
                    {
                        for (uint i = 0; i < item.Num; i++)
                        {
                            syncs.AddRange((await AddEquipment(item.DropItemId, src, false, groupId, param)).Item2);
                        }
                    }
                    else if (GameData.GridFightForgeData.ContainsKey(item.DropItemId))
                    {
                        for (uint i = 0; i < item.Num; i++)
                        {
                            syncs.AddRange(await roleComp.AddForgeItem(item.DropItemId, false, src, groupId, 0, param));
                        }
                    }

                    break;
                }
                case GridFightDropType.Orb:
                {
                    // add orbs
                    syncs.AddRange(await orbComp.AddOrb(item.DropItemId, src, false, groupId, param));
                    break;
                }
            }
        }

        if (sendPacket && syncs.Count > 0)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
        }

        return syncs;
    }

    public async ValueTask<(List<BaseGridFightSyncData>, List<GridFightDropItemInfo>)> TakeBasicBonusItems(
        List<GridFightBasicBonusPoolV2Excel> bonuses, GridFightSrc src = GridFightSrc.KGridFightSrcNone, uint groupId = 0, bool sendPacket = true)
    {
        var basicComp = Inst.GetComponent<GridFightBasicComponent>();
        var shopComp = Inst.GetComponent<GridFightShopComponent>();
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();
        var orbComp = Inst.GetComponent<GridFightOrbComponent>();

        List<BaseGridFightSyncData> syncs = [];
        List<GridFightDropItemInfo> drops = [];

        foreach (var bonus in bonuses)
        {
            // get drop
            switch (bonus.BonusType)
            {
                case GridFightBonusTypeEnum.Gold:
                {
                    // add gold
                    await basicComp.UpdateGoldNum((int)bonus.Value, false);
                    syncs.Add(new GridFightGoldSyncData(src, basicComp.Data, groupId));

                    drops.Add(new GridFightDropItemInfo
                    {
                        DropType = GridFightDropType.Coin,
                        Num = bonus.Value
                    });
                    break;
                }
                case GridFightBonusTypeEnum.Refresh:
                {
                    // add refresh count
                    shopComp.Data.FreeRefreshCount += bonus.Value;
                    syncs.Add(new GridFightShopSyncData(src, shopComp.Data.Clone(), basicComp.Data.CurLevel, groupId));

                    drops.Add(new GridFightDropItemInfo
                    {
                        DropType = GridFightDropType.Refresh,
                        Num = bonus.Value
                    });
                    break;
                }
                case GridFightBonusTypeEnum.SpecificAvatar:
                {
                    // add role
                    var roleId = bonus.BonusTypeParamList[0];
                    var tier = bonus.BonusTypeParamList[1];

                    syncs.AddRange(await roleComp.AddAvatar(roleId, tier, false, true, src, groupId));

                    drops.Add(new GridFightDropItemInfo
                    {
                        DisplayValue = new GridDropItemDisplayInfo
                        {
                            Tier = tier
                        },
                        DropItemId = roleId,
                        DropType = GridFightDropType.Role,
                        Num = 1
                    });
                    break;
                }
                case GridFightBonusTypeEnum.RandomAvatar:
                {
                    // add random role
                    var rarity = bonus.BonusTypeParamList[0];
                    var tier = bonus.BonusTypeParamList[1];

                    var role = GameData.GridFightRoleBasicInfoData.Values.Where(x => x.Rarity == rarity && x.IsInPool)
                        .ToList()
                        .RandomElement();

                    syncs.AddRange(await roleComp.AddAvatar(role.ID, tier, false, true, src, groupId));

                    drops.Add(new GridFightDropItemInfo
                    {
                        DisplayValue = new GridDropItemDisplayInfo
                        {
                            Tier = tier
                        },
                        DropItemId = role.ID,
                        DropType = GridFightDropType.Role,
                        Num = 1
                    });
                    break;
                }
                case GridFightBonusTypeEnum.Item:
                {
                    // consumable or equipment
                    var itemId = bonus.BonusTypeParamList[0];

                    drops.Add(new GridFightDropItemInfo
                    {
                        DropItemId = itemId,
                        Num = 1,
                        DropType = GridFightDropType.Item
                    });

                    // check if consumable or equipment or forge
                    if (GameData.GridFightEquipmentData.ContainsKey(itemId))
                    {
                        syncs.AddRange((await AddEquipment(itemId, src, false, groupId)).Item2);

                    }
                    else if (GameData.GridFightConsumablesData.ContainsKey(itemId))
                    {
                        syncs.AddRange(await UpdateConsumable(itemId, 1, src, false, groupId));
                    }
                    else if (GameData.GridFightForgeData.ContainsKey(itemId))
                    {
                        syncs.AddRange(await roleComp.AddForgeItem(itemId, false, src, groupId));
                    }

                    break;
                }
                case GridFightBonusTypeEnum.Exp:
                {
                    // add exp
                    await basicComp.AddLevelExp(bonus.Value, false);
                    syncs.Add(new GridFightPlayerLevelSyncData(src, basicComp.Data, groupId));

                    drops.Add(new GridFightDropItemInfo
                    {
                        DropType = GridFightDropType.Exp,
                        Num = bonus.Value
                    });
                    break;
                }
                case GridFightBonusTypeEnum.Orb:
                {
                    // add orbs
                    var orbId = bonus.BonusTypeParamList[0];
                    syncs.AddRange(await orbComp.AddOrb(orbId, src, false, groupId));

                    drops.Add(new GridFightDropItemInfo
                    {
                        DropItemId = orbId,
                        DropType = GridFightDropType.Orb,
                        Num = bonus.Value
                    });
                    break;
                }
                case GridFightBonusTypeEnum.RandomEquipByCategory:
                {
                    var category = bonus.BonusTypeParamList[0];

                    var equip = GameData.GridFightEquipmentData.Values.Where(x => (int)x.EquipCategory == category)
                        .ToList().RandomElement();

                    syncs.AddRange((await AddEquipment(equip.ID, src, false, groupId)).Item2);

                    drops.Add(new GridFightDropItemInfo
                    {
                        DropItemId = equip.ID,
                        DropType = GridFightDropType.Item,
                        Num = 1
                    });
                    break;
                }
                case GridFightBonusTypeEnum.RandomEquipByFunc:
                {
                    var func = bonus.BonusTypeParamList[0];

                    var equip = GameData.GridFightEquipmentData.Values.Where(x => (int)x.EquipFunc == func)
                        .ToList().RandomElement();

                    syncs.AddRange((await AddEquipment(equip.ID, src, false, groupId)).Item2);

                    drops.Add(new GridFightDropItemInfo
                    {
                        DropItemId = equip.ID,
                        DropType = GridFightDropType.Item,
                        Num = 1
                    });
                    break;
                }
                case GridFightBonusTypeEnum.RandomSameAvatar:
                {
                    // TODO (not used in official server)
                    break;
                }
            }
        }

        if (syncs.Count > 0 && sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
        }

        return (syncs, drops);
    }

    #endregion

    #region Consumables

    public async ValueTask<Retcode> UseConsumable(uint itemId, GridFightConsumableTargetInfo target)
    {
        if (!GameData.GridFightConsumablesData.TryGetValue(itemId, out var consumablesExcel)) return Retcode.RetGridFightConfMiss;

        // if owned
        var item = Data.ConsumableItems.FirstOrDefault(x => x.ItemId == itemId);
        if (item == null || item.Count == 0) return Retcode.RetGridFightItemNotEnough;

        List<BaseGridFightSyncData> syncs = [];
        if (consumablesExcel.IfConsume)
        {
            syncs.AddRange(await UpdateConsumable(itemId, -1, GridFightSrc.KGridFightSrcUseConsumable, false, 0, item.ItemId));
        }

        (Retcode, List<BaseGridFightSyncData>) res = consumablesExcel.ConsumableRule switch
        {
            GridFightConsumeTypeEnum.Remove => await HandleRemoveConsumable(target),
            GridFightConsumeTypeEnum.Roll => await HandleRollConsumable(target),
            GridFightConsumeTypeEnum.Upgrade => await HandleUpgradeConsumable(target),
            GridFightConsumeTypeEnum.Copy => await HandleCopyConsumable(target, consumablesExcel.ConsumableParamList),
            GridFightConsumeTypeEnum.GainRecommendEquip => await HandleGainRecommendEquipConsumable(target, consumablesExcel.ConsumableParamList),
            _ => (Retcode.RetGridFightConfMiss, [])
        };
        syncs.AddRange(res.Item2);

        if (syncs.Count > 0)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
        }

        return res.Item1;
    }

    private async ValueTask<(Retcode, List<BaseGridFightSyncData>)> HandleRemoveConsumable(GridFightConsumableTargetInfo target)
    {
        List<BaseGridFightSyncData> syncs = [];

        if (target.RemoveTypeTargetInfo == null) return (Retcode.RetReqParaInvalid, syncs);

        var roleComp = Inst.GetComponent<GridFightRoleComponent>();
        var traitComp = Inst.GetComponent<GridFightTraitComponent>();
        var role = roleComp.Data.Roles.FirstOrDefault(x => x.UniqueId == target.RemoveTypeTargetInfo.DressRoleUniqueId);
        if (role == null) return (Retcode.RetGridFightRoleNotExist, syncs);

        // unequip
        foreach (var roleEquipmentId in role.EquipmentIds.Clone())
        {
            role.EquipmentIds.Remove(roleEquipmentId);  // safety

            syncs.AddRange(await OnEquipmentUnEquipped(roleEquipmentId, role));  // check action
        }

        // sync
        syncs.Add(new GridFightRoleUpdateSyncData(GridFightSrc.KGridFightSrcUseConsumable, role.Clone()));

        // check trait
        await traitComp.CheckTrait();

        return (Retcode.RetSucc, syncs);
    }

    private async ValueTask<(Retcode, List<BaseGridFightSyncData>)> HandleRollConsumable(GridFightConsumableTargetInfo target)
    {
        List<BaseGridFightSyncData> syncs = [];

        if (target.RollTypeTargetInfo == null) return (Retcode.RetReqParaInvalid, syncs);

        var roleComp = Inst.GetComponent<GridFightRoleComponent>();
        var equipment = Data.EquipmentItems.FirstOrDefault(x => x.UniqueId == target.RollTypeTargetInfo.DressEquipmentUniqueId);
        var role = roleComp.Data.Roles.FirstOrDefault(x => x.UniqueId == target.RollTypeTargetInfo.DressRoleUniqueId);
        if (role == null && equipment == null) return (Retcode.RetGridFightRoleNotExist, syncs);

        if (role != null)
        {
            // unequip old equipment
            foreach (var equipmentUid in role.EquipmentIds)
            {
                syncs.AddRange(await RollEquipment(equipmentUid));
            }

            role.EquipmentIds.Clear();

            // sync
            syncs.Add(new GridFightRoleUpdateSyncData(GridFightSrc.KGridFightSrcUseConsumable, role.Clone()));
        }

        if (equipment != null)
        {
            syncs.AddRange(await RollEquipment(equipment.UniqueId));
        }

        return (Retcode.RetSucc, syncs);
    }

    private async ValueTask<List<BaseGridFightSyncData>> RollEquipment(uint uniqueId)
    {
        List<BaseGridFightSyncData> syncs = [];
        var equipment = Data.EquipmentItems.FirstOrDefault(x => x.UniqueId == uniqueId);
        if (equipment == null) return syncs;

        // remove old equipment
        syncs.AddRange(await RemoveEquipment(equipment.UniqueId, GridFightSrc.KGridFightSrcUseConsumable, false));

        // add new equipment
        var equipConf = GameData.GridFightEquipmentData[equipment.ItemId];
        var newEquip = GameData.GridFightEquipmentData.Values
            .Where(x => x.EquipCategory == equipConf.EquipCategory && x.ID != equipConf.ID).ToList()
            .RandomElement();

        syncs.AddRange((await AddEquipment(newEquip.ID, GridFightSrc.KGridFightSrcUseConsumable, false))
            .Item2);
        return syncs;
    }

    private async ValueTask<(Retcode, List<BaseGridFightSyncData>)> HandleUpgradeConsumable(GridFightConsumableTargetInfo target)
    {
        List<BaseGridFightSyncData> syncs = [];

        if (target.UpgradeTypeTargetInfo == null) return (Retcode.RetReqParaInvalid, syncs);

        var targetEquip = Data.EquipmentItems.FirstOrDefault(x => x.UniqueId == target.UpgradeTypeTargetInfo.DressEquipmentUniqueId);
        if (targetEquip == null) return (Retcode.RetGridFightEquipNotExist, syncs);

        var upgradeId = GameData.GridFightEquipUpgradeData.GetValueOrDefault(targetEquip.ItemId)?.UpgradeID;
        if (upgradeId == null) return (Retcode.RetGridFightConfMiss, syncs);

        // remove old equipment
        syncs.AddRange(await RemoveEquipment(targetEquip.UniqueId, GridFightSrc.KGridFightSrcUseConsumable, false));

        // add new equipment
        syncs.AddRange((await AddEquipment(upgradeId.Value, GridFightSrc.KGridFightSrcUseConsumable, false))
            .Item2);

        return (Retcode.RetSucc, syncs);
    }

    private async ValueTask<(Retcode, List<BaseGridFightSyncData>)> HandleCopyConsumable(GridFightConsumableTargetInfo target, List<uint> param)
    {
        List<BaseGridFightSyncData> syncs = [];
        if (target.CopyTypeTargetInfo == null) return (Retcode.RetReqParaInvalid, syncs);

        var maxRarity = param[0];
        var count = param[1];

        var roleComp = Inst.GetComponent<GridFightRoleComponent>();
        var targetRole = roleComp.Data.Roles.FirstOrDefault(x => x.UniqueId == target.CopyTypeTargetInfo.DressRoleUniqueId);

        if (targetRole == null) return (Retcode.RetGridFightRoleNotExist, syncs);
        // check if can copy
        if (!GameData.GridFightRoleBasicInfoData.TryGetValue(targetRole.RoleId, out var roleConf))
            return (Retcode.RetGridFightConfMiss, syncs);

        if (roleConf.Rarity > maxRarity) return (Retcode.RetGridFightHighRariyForCopy, syncs);

        // check if enough space
        var canAddCount = roleComp.GetEmptyPosCount();
        if (canAddCount < count) return (Retcode.RetGridFightNoPosCanPlace, syncs);

        // copy role
        for (uint i = 0; i < count; i++)
        {
            syncs.AddRange(await roleComp.AddAvatar(targetRole.RoleId, 1, false, true,
                GridFightSrc.KGridFightSrcUseConsumable));
        }

        return (Retcode.RetSucc, syncs);
    }

    private async ValueTask<(Retcode, List<BaseGridFightSyncData>)> HandleGainRecommendEquipConsumable(
        GridFightConsumableTargetInfo target, List<uint> param)
    {
        List<BaseGridFightSyncData> syncs = [];
        if (target.GainRecommendEquipTypeTargetInfo == null) return (Retcode.RetReqParaInvalid, syncs);

        var roleComp = Inst.GetComponent<GridFightRoleComponent>();
        var targetRole = roleComp.Data.Roles.FirstOrDefault(x => x.UniqueId == target.GainRecommendEquipTypeTargetInfo.DressRoleUniqueId);
        if (targetRole == null) return (Retcode.RetGridFightRoleNotExist, syncs);

        // get recommend equipment
        if (!GameData.GridFightRoleRecommendEquipData.TryGetValue(targetRole.RoleId, out var recommendConf)) 
            return (Retcode.RetGridFightConfMiss, syncs);

        var recommendList = recommendConf.FirstRecommendEquipList.Concat(recommendConf.SecondRecommendEquipList).ToList();

        var recommendCount = Math.Min(param[0], (uint)recommendList.Count);
        var recommends = recommendList.OrderBy(_ => Guid.NewGuid()).Take((int)recommendCount).ToList();

        syncs.AddRange(
            await Inst.CreatePendingAction<GridFightRecommendEquipmentPendingAction>(
                GridFightSrc.KGridFightSrcUseConsumable, false, recommends));

        return (Retcode.RetSucc, syncs);
    }

    #endregion

    #region Equipment Func

    public async ValueTask<List<BaseGridFightSyncData>> OnEquipmentEquipped(uint uniqueId, GridFightRoleInfoPb role)
    {
        List<BaseGridFightSyncData> syncData = [];
        var basicComp = Inst.GetComponent<GridFightBasicComponent>();
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();

        // get info
        var equipment = Data.EquipmentItems.FirstOrDefault(x => x.UniqueId == uniqueId);
        if (equipment == null || !GameData.GridFightEquipmentData.TryGetValue(equipment.ItemId, out var itemConf)) return syncData;

        switch (itemConf.EquipFunc)
        {
            case GridFightEquipFuncTypeEnum.AvatarMaxNumberAdd:
            {
                // add max avatar num
                basicComp.Data.MaxAvatarNum++;
                basicComp.Data.OffFieldAvatarNum++;

                // sync
                syncData.Add(new GridFightMaxAvatarNumSyncData(GridFightSrc.KGridFightSrcEquipmentFunc, basicComp.Data, 0, uniqueId));
                syncData.Add(new GridFightMaxOffFieldCountSyncData(GridFightSrc.KGridFightSrcEquipmentFunc, basicComp.Data, 0, uniqueId));
                break;
            }
            case GridFightEquipFuncTypeEnum.CraftableThiefGlove:
            {
                // add random 2 craftable equipments to role
                var equipPool = GameData.GridFightEquipmentData.Values.Where(x =>
                    x.EquipCategory == GridFightEquipCategoryEnum.Craftable && x.ID != equipment.ItemId).ToList();

                for (var i = 0; i < 2; i++)
                {
                    var equip = equipPool.RandomElement();
                    // add equipment
                    var res = await AddEquipment(equip.ID, GridFightSrc.KGridFightSrcEquipmentFunc, false, 0, uniqueId);
                    if (res.Item1 == null) continue;

                    // sync
                    syncData.AddRange(res.Item2);
                    syncData.AddRange(await roleComp.DressRole(role.UniqueId, res.Item1.UniqueId,
                        GridFightSrc.KGridFightSrcEquipmentFunc, false, 0, uniqueId));
                }

                break;
            }
            case GridFightEquipFuncTypeEnum.RadiantThiefGlove:
            {
                // add random 2 radiant equipments to role
                var equipPool = GameData.GridFightEquipmentData.Values.Where(x =>
                    x.EquipCategory == GridFightEquipCategoryEnum.Radiant && x.ID != equipment.ItemId).ToList();

                for (var i = 0; i < 2; i++)
                {
                    var equip = equipPool.RandomElement();
                    // add equipment
                    var res = await AddEquipment(equip.ID, GridFightSrc.KGridFightSrcEquipmentFunc, false, 0, uniqueId);
                    if (res.Item1 == null) continue;

                    // sync
                    syncData.AddRange(res.Item2);
                    syncData.AddRange(await roleComp.DressRole(role.UniqueId, res.Item1.UniqueId,
                        GridFightSrc.KGridFightSrcEquipmentFunc, false, 0, uniqueId));
                }

                break;
            }
        }

        return syncData;
    }

    public async ValueTask<List<BaseGridFightSyncData>> OnEnterSection()
    {
        List<BaseGridFightSyncData> syncData = [];
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();

        // get info
        foreach (var role in roleComp.Data.Roles)
        {
            foreach (var uniqueId in role.EquipmentIds)
            {
                var equipment = Data.EquipmentItems.FirstOrDefault(x => x.UniqueId == uniqueId);
                if (equipment == null ||
                    !GameData.GridFightEquipmentData.TryGetValue(equipment.ItemId, out var itemConf)) continue;

                switch (itemConf.EquipFunc)
                {
                    case GridFightEquipFuncTypeEnum.CraftableThiefGlove:
                    {
                        // remove old
                        foreach (var uid in role.EquipmentIds.Clone())
                        {
                            if (uid == uniqueId) continue; // skip self
                            var res = await RemoveEquipment(uid, GridFightSrc.KGridFightSrcEquipmentFunc, false,
                                uniqueId);
                            syncData.AddRange(res);
                        }

                        // add random 2 craftable equipments to role
                        var equipPool = GameData.GridFightEquipmentData.Values.Where(x =>
                            x.EquipCategory == GridFightEquipCategoryEnum.Craftable && x.ID != equipment.ItemId).ToList();

                        for (var i = 0; i < 2; i++)
                        {
                            var equip = equipPool.RandomElement();
                            // add equipment
                            var res = await AddEquipment(equip.ID, GridFightSrc.KGridFightSrcEquipmentFunc, false, 0,
                                uniqueId);
                            if (res.Item1 == null) continue;

                            // sync
                            syncData.AddRange(res.Item2);
                            syncData.AddRange(await roleComp.DressRole(role.UniqueId, res.Item1.UniqueId,
                                GridFightSrc.KGridFightSrcEquipmentFunc, false, 0, uniqueId));
                        }

                        break;
                    }
                    case GridFightEquipFuncTypeEnum.RadiantThiefGlove:
                    {
                        // remove old
                        foreach (var uid in role.EquipmentIds.Clone())
                        {
                            if (uid == uniqueId) continue; // skip self
                            var res = await RemoveEquipment(uid, GridFightSrc.KGridFightSrcEquipmentFunc, false,
                                uniqueId);
                            syncData.AddRange(res);
                        }

                        // add random 2 radiant equipments to role
                        var equipPool = GameData.GridFightEquipmentData.Values.Where(x =>
                            x.EquipCategory == GridFightEquipCategoryEnum.Radiant && x.ID != equipment.ItemId).ToList();

                        for (var i = 0; i < 2; i++)
                        {
                            var equip = equipPool.RandomElement();
                            // add equipment
                            var res = await AddEquipment(equip.ID, GridFightSrc.KGridFightSrcEquipmentFunc, false, 0,
                                uniqueId);
                            if (res.Item1 == null) continue;

                            // sync
                            syncData.AddRange(res.Item2);
                            syncData.AddRange(await roleComp.DressRole(role.UniqueId, res.Item1.UniqueId,
                                GridFightSrc.KGridFightSrcEquipmentFunc, false, 0, uniqueId));
                        }

                        break;
                    }
                }
            }
        }

        return syncData;
    }

    public async ValueTask<List<BaseGridFightSyncData>> OnEquipmentUnEquipped(uint uniqueId, GridFightRoleInfoPb role)
    {
        List<BaseGridFightSyncData> syncData = [];
        var basicComp = Inst.GetComponent<GridFightBasicComponent>();
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();

        // get info
        var equipment = Data.EquipmentItems.FirstOrDefault(x => x.UniqueId == uniqueId);
        if (equipment == null || !GameData.GridFightEquipmentData.TryGetValue(equipment.ItemId, out var itemConf))
            return syncData;

        switch (itemConf.EquipFunc)
        {
            case GridFightEquipFuncTypeEnum.AvatarMaxNumberAdd:
            {
                // decrease max avatar num
                basicComp.Data.MaxAvatarNum--;
                basicComp.Data.OffFieldAvatarNum--;

                // if remove pos has avatar
                if (basicComp.Data.MaxAvatarNum < 13)
                {
                    // check
                    var targetRole = roleComp.Data.Roles.FirstOrDefault(x => x.Pos == basicComp.Data.MaxAvatarNum + 1);
                    if (targetRole != null)
                    {
                        targetRole.Pos = roleComp.GetEmptyPos();
                        syncData.Add(new GridFightRoleUpdateSyncData(GridFightSrc.KGridFightSrcNone, targetRole));
                    }
                }

                // sync
                syncData.Add(new GridFightMaxAvatarNumSyncData(GridFightSrc.KGridFightSrcEquipmentFunc, basicComp.Data,
                    0, uniqueId));
                syncData.Add(new GridFightMaxOffFieldCountSyncData(GridFightSrc.KGridFightSrcEquipmentFunc,
                    basicComp.Data, 0, uniqueId));
                break;
            }
            case GridFightEquipFuncTypeEnum.CraftableThiefGlove:
            case GridFightEquipFuncTypeEnum.RadiantThiefGlove:
            {
                // remove other equipments
                foreach (var uid in role.EquipmentIds.Clone())
                {
                    if (uid == uniqueId) continue;  // skip self

                    var res = await RemoveEquipment(uid, GridFightSrc.KGridFightSrcEquipmentFunc, false, uniqueId);
                    syncData.AddRange(res);
                }

                break;
            }
        }

        return syncData;
    }

    #endregion

    #region Serialization

    public override GridFightGameInfo ToProto()
    {
        return new GridFightGameInfo
        {
            GridItemsInfo = new GridFightGameItemsInfo
            {
                GridFightEquipmentList = { Data.EquipmentItems.Select(x => x.ToProto()) },
                GridFightConsumableList = { Data.ConsumableItems.Select(x => x.ToProto()) }
            }
        };
    }

    #endregion
}

public static class GridFightItemsComponentExtensions
{
    public static GridFightEquipmentInfo ToProto(this GridFightEquipmentItemPb equipment)
    {
        return new GridFightEquipmentInfo
        {
            GridFightEquipmentId = equipment.ItemId,
            UniqueId = equipment.UniqueId
        };
    }

    public static GridFightConsumableInfo ToProto(this GridFightConsumableItemPb consumable)
    {
        return new GridFightConsumableInfo
        {
            ItemId = consumable.ItemId,
            Num = consumable.Count
        };
    }

    public static GridFightConsumableUpdateInfo ToUpdateInfo(this GridFightConsumableItemPb consumable, int updateCount)
    {
        return new GridFightConsumableUpdateInfo
        {
            ItemId = consumable.ItemId,
            Num = consumable.Count,
            ItemStackCount = updateCount
        };
    }

    public static BattleGridFightEquipmentInfo ToBattleInfo(this GridFightEquipmentItemPb equipment)
    {
        return new BattleGridFightEquipmentInfo
        {
            GridFightEquipmentId = equipment.ItemId,
            UniqueId = equipment.UniqueId
        };
    }
}