using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.Enums.GridFight;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;
using EggLink.DanhengServer.Util;
using System.Collections.Generic;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Component;

public class GridFightOrbComponent(GridFightInstance inst) : BaseGridFightComponent(inst)
{
    public static List<GridFightBasicBonusPoolV2Excel> ExtractCombinationBonus(uint combineBonusId)
    {
        List<GridFightBasicBonusPoolV2Excel> bonusPools = [];

        if (!GameData.GridFightCombinationBonusData.TryGetValue(combineBonusId, out var comboBonusInfo))
            return bonusPools;

        for (var i = 0; i < comboBonusInfo.CombinationBonusList.Count; i++)
        {
            var bonusId = comboBonusInfo.CombinationBonusList[i];
            var bonusNum = comboBonusInfo.BonusNumberList[i];

            if (bonusId == 1)
                bonusPools.Add(new GridFightBasicBonusPoolV2Excel
                {
                    BonusType = GridFightBonusTypeEnum.Gold,
                    Value = bonusNum / 10000
                });
            else if (GameData.GridFightBasicBonusPoolV2Data.TryGetValue(bonusId, out var bonusPool))
            {
                bonusPools.AddRange(Enumerable.Repeat(bonusPool, (int)(bonusNum / 10000)));
            }
        }

        return bonusPools;
    }

    public GridFightOrbInfoPb Data { get; set; } = new();

    public async ValueTask<List<BaseGridFightSyncData>> AddOrb(uint orbItemId, GridFightSrc src = GridFightSrc.KGridFightSrcNone, bool sendPacket = true, uint groupId = 0, params uint[] param)
    {
        if (!GameData.GridFightOrbData.ContainsKey(orbItemId))  // sanity check
            return [];

        var roleComp = Inst.GetComponent<GridFightRoleComponent>();

        var info = new GridFightGameOrbPb
        {
            OrbItemId = orbItemId,
            UniqueId = ++roleComp.Data.CurUniqueId
        };
        
        Data.Orbs.Add(info);

        var syncData = new GridFightOrbSyncData(src, info, groupId, param);
        if (sendPacket)
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncData));

        return [syncData];
    }

    public async ValueTask UseOrb(List<uint> uniqueIds)
    {
        List<BaseGridFightSyncData> syncDatas = [];

        foreach (var uniqueId in uniqueIds)
        {
            var orb = Data.Orbs.FirstOrDefault(x => x.UniqueId == uniqueId);
            if (orb == null) continue;

            if (!GameData.GridFightOrbData.TryGetValue(orb.OrbItemId, out var excel)) continue;

            Data.Orbs.Remove(orb);

            syncDatas.Add(new GridFightRemoveOrbSyncData(GridFightSrc.KGridFightSrcUseOrb, orb, uniqueId, uniqueId, orb.OrbItemId));

            // open orb effect
            var res = await TakeOrbEffect(excel, uniqueId);
            syncDatas.AddRange(res.Item1);

            await Inst.Player.SendPacket(new PacketGridFightUseOrbNotify(uniqueId, res.Item2));
        }

        if (syncDatas.Count > 0)
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncDatas));
    }

    private async ValueTask<(List<BaseGridFightSyncData>, List<GridFightDropItemInfo>)> TakeOrbEffect(
        GridFightOrbExcel excel, uint groupId)
    {
        List<GridFightBasicBonusPoolV2Excel> bonusPools = [];

        // check file bonus
        if (GameData.GridFightBasicOrbRewardsConfig.OrbRewards.TryGetValue(excel.OrbID, out var fileBonusInfo)) 
        {
            bonusPools.AddRange(fileBonusInfo.Rewards.Values.ToList().RandomElement());
        }

        // check combination bonus
        if (bonusPools.Count == 0)
        {
            bonusPools.AddRange(ExtractCombinationBonus(excel.BonusID));
        }

        // check basic bonus
        if (bonusPools.Count == 0 && GameData.GridFightBasicBonusPoolV2Data.TryGetValue(excel.BonusID, out var basicBonusInfo))
        {
            bonusPools.Add(basicBonusInfo);
        }

        // execute
        if (bonusPools.Count > 0)
        {
            var itemsComp = Inst.GetComponent<GridFightItemsComponent>();

            return await itemsComp.TakeBasicBonusItems(bonusPools, GridFightSrc.KGridFightSrcUseOrb, groupId, false);
        }

        // default
        return await TakeOrbDefaultEffect(excel.Type, groupId);
    }

    private async ValueTask<(List<BaseGridFightSyncData>, List<GridFightDropItemInfo>)> TakeOrbDefaultEffect(GridFightOrbTypeEnum type, uint groupId)
    {
        List<BaseGridFightSyncData> syncDatas = [];
        List<GridFightDropItemInfo> dropItems = [];

        var basicComp = Inst.GetComponent<GridFightBasicComponent>();
        var itemsComp = Inst.GetComponent<GridFightItemsComponent>();
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();

        switch (type)
        {
            case GridFightOrbTypeEnum.White:
            {
                // 2 coin or 3 exp or 1 consumable
                var ran = Random.Shared.Next(3);
                if (ran == 0)
                {
                    await basicComp.UpdateGoldNum(2, false, GridFightSrc.KGridFightSrcUseOrb);
                    syncDatas.Add(new GridFightGoldSyncData(GridFightSrc.KGridFightSrcUseOrb, basicComp.Data.Clone(),
                        groupId));

                    dropItems.Add(new GridFightDropItemInfo
                    {
                        DropType = GridFightDropType.Coin,
                        Num = 2
                    });
                }
                else if (ran == 1)
                {
                    await basicComp.AddLevelExp(3, false);
                    syncDatas.Add(new GridFightPlayerLevelSyncData(GridFightSrc.KGridFightSrcUseOrb,
                        basicComp.Data.Clone(), groupId));

                    dropItems.Add(new GridFightDropItemInfo
                    {
                        DropType = GridFightDropType.Exp,
                        Num = 3
                    });
                }
                else
                {
                    // random consumable
                    var consumable =
                        GameData.GridFightConsumablesData.Values.Where(x =>
                            x.ConsumableRule != GridFightConsumeTypeEnum.Remove).ToList().RandomElement();

                    var res = await itemsComp.UpdateConsumable(consumable.ID, 1, GridFightSrc.KGridFightSrcUseOrb, false, groupId);
                    syncDatas.AddRange(res);

                    dropItems.Add(new GridFightDropItemInfo
                    {
                        DropItemId = consumable.ID,
                        DropType = GridFightDropType.Item,
                        Num = 1
                    });
                }

                break;
            }
            case GridFightOrbTypeEnum.Blue:
            {
                // 1*1-tier 1~2-rarity role or 1*equipment
                if (Random.Shared.Next(2) == 0)
                {
                    var role = GameData.GridFightRoleBasicInfoData.Values.Where(x => x.Rarity <= 2).ToList()
                        .RandomElement();

                    // add role
                    syncDatas.AddRange(await roleComp.AddAvatar(role.ID, 1, false, true,
                        GridFightSrc.KGridFightSrcUseOrb, groupId));

                    dropItems.Add(new GridFightDropItemInfo
                    {
                        DropType = GridFightDropType.Role,
                        Num = 1,
                        DisplayValue = new GridDropItemDisplayInfo
                        {
                            Tier = 1
                        },
                        DropItemId = role.ID
                    });
                }
                else
                {
                    // random equipment
                    var basicEquipment = GameData.GridFightEquipmentData.Values.Where(x =>
                        x.EquipCategory == GridFightEquipCategoryEnum.Basic).ToList().RandomElement();

                    // add equipment
                    syncDatas.AddRange((await itemsComp.AddEquipment(basicEquipment.ID,
                        GridFightSrc.KGridFightSrcUseOrb, false, groupId)).Item2);

                    dropItems.Add(new GridFightDropItemInfo
                    {
                        DropType = GridFightDropType.Item,
                        Num = 1,
                        DropItemId = basicEquipment.ID
                    });
                }

                break;
            }
            case GridFightOrbTypeEnum.Glod:
            {
                // 1*1~2-tier 3~4-rarity role or 2*equipment
                if (Random.Shared.Next(2) == 0)
                {
                    var role = GameData.GridFightRoleBasicInfoData.Values.Where(x => x.Rarity is >= 3 and <= 4).ToList()
                        .RandomElement();

                    // add role
                    var tier = (uint)Random.Shared.Next(1, 3);
                    syncDatas.AddRange(await roleComp.AddAvatar(role.ID, tier, false, true,
                        GridFightSrc.KGridFightSrcUseOrb, groupId));

                    dropItems.Add(new GridFightDropItemInfo
                    {
                        DropType = GridFightDropType.Role,
                        Num = 1,
                        DisplayValue = new GridDropItemDisplayInfo
                        {
                            Tier = tier
                        },
                        DropItemId = role.ID
                    });
                }
                else
                {
                    // random 2 equipment
                    var basicEquipment = GameData.GridFightEquipmentData.Values.Where(x =>
                        x.EquipCategory == GridFightEquipCategoryEnum.Basic).ToList();

                    for (var i = 0; i < 2; i++)
                    {
                        // add equipment
                        var equip = basicEquipment.RandomElement();
                        syncDatas.AddRange((await itemsComp.AddEquipment(equip.ID,
                            GridFightSrc.KGridFightSrcUseOrb, false, groupId)).Item2);

                        dropItems.Add(new GridFightDropItemInfo
                        {
                            DropType = GridFightDropType.Item,
                            Num = 1,
                            DropItemId = equip.ID
                        });
                    }
                }

                break;
            }
            case GridFightOrbTypeEnum.Colorful:
            {
                // 1*2-tier 4-rarity role
                var role = GameData.GridFightRoleBasicInfoData.Values.Where(x => x.Rarity == 4).ToList()
                    .RandomElement();

                // add role
                syncDatas.AddRange(await roleComp.AddAvatar(role.ID, 2, false, true,
                    GridFightSrc.KGridFightSrcUseOrb, groupId));

                dropItems.Add(new GridFightDropItemInfo
                {
                    DropType = GridFightDropType.Role,
                    Num = 1,
                    DisplayValue = new GridDropItemDisplayInfo
                    {
                        Tier = 2
                    },
                    DropItemId = role.ID
                });

                break;
            }
            case GridFightOrbTypeEnum.BigColorful:
            {
                // 1*2-tier 5-rarity role
                var role = GameData.GridFightRoleBasicInfoData.Values.Where(x => x.Rarity == 5).ToList()
                    .RandomElement();

                // add role
                syncDatas.AddRange(await roleComp.AddAvatar(role.ID, 2, false, true, GridFightSrc.KGridFightSrcUseOrb,
                    groupId));

                dropItems.Add(new GridFightDropItemInfo
                {
                    DropType = GridFightDropType.Role,
                    Num = 1,
                    DisplayValue = new GridDropItemDisplayInfo
                    {
                        Tier = 2
                    },
                    DropItemId = role.ID
                });

                break;
            }
            case GridFightOrbTypeEnum.GoldenEgg:
            {
                // 1*3-tier 5-rarity role
                var role = GameData.GridFightRoleBasicInfoData.Values.Where(x => x.Rarity == 5).ToList()
                    .RandomElement();

                // add role
                syncDatas.AddRange(await roleComp.AddAvatar(role.ID, 3, false, true,
                    GridFightSrc.KGridFightSrcUseOrb, groupId));

                dropItems.Add(new GridFightDropItemInfo
                {
                    DropType = GridFightDropType.Role,
                    Num = 1,
                    DisplayValue = new GridDropItemDisplayInfo
                    {
                        Tier = 3
                    },
                    DropItemId = role.ID
                });

                break;
            }
        }

        return (syncDatas, dropItems);
    }

    public override GridFightGameInfo ToProto()
    {
        return new GridFightGameInfo
        {
            GridOrbInfo = new GridFightGameOrbInfo
            {
                GridGameOrbList = { Data.Orbs.Select(x => x.ToProto()) }
            }
        };
    }
}

public static class GridFightOrbComponentExtensions
{
    public static GridGameOrbInfo ToProto(this GridFightGameOrbPb orb)
    {
        return new GridGameOrbInfo
        {
            OrbItemId = orb.OrbItemId,
            UniqueId = orb.UniqueId
        };
    }

    public static GridFightOrbSyncInfo ToSyncInfo(this GridFightGameOrbPb orb)
    {
        return new GridFightOrbSyncInfo
        {
            OrbItemId = orb.OrbItemId,
            UniqueId = orb.UniqueId
        };
    }
}