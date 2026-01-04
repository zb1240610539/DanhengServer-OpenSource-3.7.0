using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Enums.GridFight;
using EggLink.DanhengServer.GameServer.Game.GridFight.PendingAction;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;
using System.Data.OscarClient;
using EggLink.DanhengServer.Data.Excel;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Component;

public class GridFightTraitComponent(GridFightInstance inst) : BaseGridFightComponent(inst)
{
    public GridFightTraitInfoPb Data { get; set; } = new();

    public List<GridFightRoleInfoPb> GetTraitRoles(uint traitId)
    {
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();

        var onGroundRoles = roleComp.Data.Roles.Where(x => x.Pos <= GridFightRoleComponent.PrepareAreaPos).ToList();

        var traitRoles = onGroundRoles.Where(x => GameData.GridFightRoleBasicInfoData
            .GetValueOrDefault(x.RoleId)?.TraitList.Contains(traitId) == true).ToList();

        // check equipment traits
        foreach (var role in onGroundRoles.Except(traitRoles))
        {
            var roleExcel = GameData.GridFightRoleBasicInfoData.GetValueOrDefault(role.RoleId);
            if (roleExcel == null) continue;

            foreach (var equipmentUid in role.EquipmentIds)
            {
                // get item
                var equipmentItem = roleComp.Inst.GetComponent<GridFightItemsComponent>().Data.EquipmentItems
                    .FirstOrDefault(x => x.UniqueId == equipmentUid);
                if (equipmentItem == null) continue;

                // get conf
                var equipmentExcel = GameData.GridFightEquipmentData.GetValueOrDefault(equipmentItem.ItemId);
                if (equipmentExcel == null) continue;

                // category (emblem)
                if (equipmentExcel.EquipCategory != GridFightEquipCategoryEnum.Emblem) continue;

                if (equipmentExcel.EquipFuncParamList.Contains(traitId))
                {
                    // we can add this role directly becuz foreach has Except option
                    traitRoles.Add(role);
                }
            }
        }


        return traitRoles;
    }

    public async ValueTask CheckTrait()
    {
        var itemsComp = Inst.GetComponent<GridFightItemsComponent>();
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();

        Dictionary<uint, uint> traitCount = [];
        List<BaseGridFightSyncData> syncList = [];

        foreach (var traitId in GameData.GridFightTraitBasicInfoData.Keys)
        {
            traitCount[traitId] = 0;  // initialize
        }

        foreach (var role in roleComp.Data.Roles.Where(x => x.Pos <= GridFightRoleComponent.PrepareAreaPos))
        {
            if (!GameData.GridFightRoleBasicInfoData.TryGetValue(role.RoleId, out var excel)) continue;

            foreach (var traitId in excel.TraitList)
            {
                traitCount[traitId]++;  // increase count
            }

            // get extra traits from equipments
            foreach (var equipmentUid in role.EquipmentIds)
            {
                var equipmentItem = itemsComp.Data.EquipmentItems.FirstOrDefault(x => x.UniqueId == equipmentUid);
                if (equipmentItem == null) continue;

                // get conf
                var equipmentExcel = GameData.GridFightEquipmentData.GetValueOrDefault(equipmentItem.ItemId);
                if (equipmentExcel == null) continue;

                // check category
                if (equipmentExcel.EquipCategory != GridFightEquipCategoryEnum.Emblem) continue;

                foreach (var traitId in equipmentExcel.EquipFuncParamList)
                {
                    traitCount[traitId]++;  // increase count
                }
            }
        }

        foreach (var (traitId, count) in traitCount)
        {
            var traitExcel = GameData.GridFightTraitBasicInfoData.GetValueOrDefault(traitId);
            if (traitExcel == null) continue;

            var traitLayers = GameData.GridFightTraitLayerData.GetValueOrDefault(traitId);
            if (traitLayers == null) continue;

            var layers = traitLayers.Where(x => x.Key <= count).ToList();
            var layer = layers.Count > 0 ? layers.Max(x => x.Value.Layer) : 0;

            var existingTrait = Data.Traits.FirstOrDefault(x => x.TraitId == traitId);

            if (existingTrait != null)
            {
                var prevLayer = existingTrait.TraitLayer;
                existingTrait.TraitLayer = layer;

                if (prevLayer != layer)
                {
                    // Sync effects
                    foreach (var effect in existingTrait.Effects)
                    {
                        if (effect.HasCoreRoleUniqueId)
                            effect.CoreRoleUniqueId = 0;

                        syncList.Add(new GridFightTraitSyncData(GridFightSrc.KGridFightSrcTraitEffectUpdate, effect, 0,
                            traitId, effect.EffectId));

                        var effectSyncs = await HandleTraitEffect(effect, prevLayer, layer);
                        syncList.AddRange(effectSyncs);
                    }
                }
            }
            else
            {
                if (layer == 0) continue;  // do not add if no layer

                var traitInfo = new GridFightGameTraitPb
                {
                    TraitId = traitId,
                    TraitLayer = layer
                };

                foreach (var effectId in traitExcel.TraitEffectList)
                {
                    var effect = new GridFightGameTraitEffectPb
                    {
                        EffectId = effectId,
                        TraitId = traitId
                    };

                    traitInfo.Effects.Add(effect);

                    var effectSyncs = await HandleTraitEffect(effect, 0, layer);
                    syncList.AddRange(effectSyncs);

                    // sync
                    syncList.Add(new GridFightTraitSyncData(GridFightSrc.KGridFightSrcTraitEffectUpdate, effect, 0, traitId, effectId));
                }

                Data.Traits.Add(traitInfo);
            }
        }

        // Send sync data
        if (syncList.Count > 0)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncList));
        }
    }

    public async ValueTask<List<BaseGridFightSyncData>> HandleTraitEffect(GridFightGameTraitEffectPb effect, uint prevLayer, uint nextLayer)
    {
        var itemsComp = Inst.GetComponent<GridFightItemsComponent>();
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();

        List<BaseGridFightSyncData> syncList = [];

        if (!GameData.GridFightTraitEffectData.TryGetValue(effect.EffectId, out var traitConf) ||
            !GameData.GridFightTraitEffectLayerPaData.TryGetValue(effect.EffectId, out var effectLayerPas)) return syncList;

        effectLayerPas.TryGetValue(prevLayer, out var prevEffectParam);
        effectLayerPas.TryGetValue(nextLayer, out var nextEffectParam);

        // Handle different effect types
        switch (traitConf.TraitEffectType)
        {
            case GridFightTraitEffectTypeEnum.TempEquip:
            {
                // add equip
                var prev = prevEffectParam?.EffectParamList.Select(x => (uint)x.Value).ToList() ?? [];
                var cur = nextEffectParam?.EffectParamList.Select(x => (uint)x.Value).ToList() ?? [];

                // remove prev - cur
                var toRemove = prev.Except(cur).ToList();
                var toAdd = cur.Except(prev).ToList();

                // remove equips
                foreach (var equipId in toRemove)
                {
                    var item = itemsComp.Data.EquipmentItems.FirstOrDefault(x => x.ItemId == equipId);
                    if (item == null) continue;
                    syncList.AddRange(await itemsComp.RemoveEquipment(item.UniqueId,
                        GridFightSrc.KGridFightSrcTraitEffectUpdate, false));
                }

                // add equips
                foreach (var equipId in toAdd)
                {
                    var res = await itemsComp.AddEquipment(equipId, GridFightSrc.KGridFightSrcTraitEffectUpdate,
                        false);
                    syncList.AddRange(res.Item2);
                }

                break;
            }
            case GridFightTraitEffectTypeEnum.TraitBonus:
            {
                if (!effect.HasThreshold)
                    effect.Threshold = 0;  // initialize
                break;
            }
            case GridFightTraitEffectTypeEnum.CoreRoleChoose:
            {
                // create pending action
                if (nextLayer != 0)
                    syncList.AddRange(await Inst.CreatePendingAction<GridFightTraitPendingAction>(
                        GridFightSrc.KGridFightSrcTraitEffectUpdate,
                        false, effect));
                break;
            }
            case GridFightTraitEffectTypeEnum.CoreRoleByEquipNum:
            {
                // check
                var traitRoles = roleComp.Data.Roles.Where(x =>
                    GameData.GridFightRoleBasicInfoData.GetValueOrDefault(x.RoleId)?.TraitList
                        .Contains(effect.TraitId) == true).ToList();

                var coreRole = traitRoles.MaxBy(x => x.EquipmentIds.Count);
                effect.CoreRoleUniqueId = coreRole?.UniqueId ?? 0;

                break;
            }
            case GridFightTraitEffectTypeEnum.SelectEnhance:
            {
                // TODO
                break;
            }
        }

        return syncList;
    }

    public async ValueTask HandleBattleEnd(PVEBattleResultCsReq req, bool success)
    {
        var itemsComp = Inst.GetComponent<GridFightItemsComponent>();

        List<BaseGridFightSyncData> syncDatas = [];

        foreach (var traitInfo in Data.Traits.Where(x => x.TraitLayer > 0))
        {
            foreach (var effectInfo in traitInfo.Effects)
            {
                if (!GameData.GridFightTraitEffectData.TryGetValue(effectInfo.EffectId, out var effectConf) ||
                    !GameData.GridFightTraitEffectLayerPaData.TryGetValue(effectInfo.EffectId,
                        out var effectLayerPas) ||
                    !effectLayerPas.TryGetValue(traitInfo.TraitLayer, out var layerEffectPa)) continue;

                if (effectConf.TraitEffectType != GridFightTraitEffectTypeEnum.TraitBonus) continue;

                if (!GameData.GridFightTraitBonusAddRuleData.TryGetValue(effectInfo.EffectId,
                        out var bonusAddRuleExcel)) continue;

                // add bonus
                var baseBonusValue = req.Stt.GridFightBattleStt.TraitBattleStt
                                         .FirstOrDefault(x => x.TraitId == traitInfo.TraitId)?.TraitEffectInfoList
                                         .FirstOrDefault(x => x.EffectId == effectConf.ID)?.SwitchList
                                         .FirstOrDefault() ??
                                     effectInfo.Threshold;

                // base value
                var addValue = bonusAddRuleExcel.TraitBonusType switch
                {
                    GridFightTraitBonusAddTypeEnum.ByEquipNum => 10u +
                                                                 (uint)GetTraitRoles(traitInfo.TraitId)
                                                                     .Sum(x => x.EquipmentIds.Count),
                    GridFightTraitBonusAddTypeEnum.ByConstWithPerfectPass => (uint)(layerEffectPa.EffectParamList
                        .FirstOrDefault()?.Value ?? 0),
                    _ => 0u
                };

                // rate
                addValue *= bonusAddRuleExcel.TraitBonusType switch
                {
                    GridFightTraitBonusAddTypeEnum.ByEquipNum => (uint)(layerEffectPa.EffectParamList.FirstOrDefault()
                        ?.Value ?? 0),
                    GridFightTraitBonusAddTypeEnum.ByConstWithPerfectPass => success
                        ? bonusAddRuleExcel.ParamList[1]
                        : bonusAddRuleExcel.ParamList[0],
                    _ => 1
                };

                baseBonusValue += addValue;

                // set
                var prev = effectInfo.Threshold;
                effectInfo.Threshold = baseBonusValue;

                // sync
                syncDatas.Add(new GridFightTraitSyncData(GridFightSrc.KGridFightSrcTraitEffectUpdate, effectInfo,
                    effectInfo.EffectId, traitInfo.TraitId, effectInfo.EffectId));

                var addBonuses = GameData.GridFightTraitBonusData
                    .GetValueOrDefault(effectInfo.EffectId)?.Values.Where(x =>
                        x.BonusThreshold > prev && x.BonusThreshold <= baseBonusValue &&
                        x.BonusType == GridFightTraitBonusTypeEnum.Bonus).ToList() ?? [];

                // take bonus effect
                var bonusIdList = addBonuses.SelectMany(x => x.BonusParamList).ToList();

                List<GridFightBasicBonusPoolV2Excel> bonusPool = [];
                foreach (var id in bonusIdList)
                {
                    bonusPool.AddRange(GridFightOrbComponent.ExtractCombinationBonus(id));
                }

                // take effect
                var res = await itemsComp.TakeBasicBonusItems(bonusPool, GridFightSrc.KGridFightSrcTraitEffectUpdate,
                    effectInfo.EffectId, false);

                syncDatas.AddRange(res.Item1);
            }
        }

        if (syncDatas.Count > 0)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncDatas));
        }
    }

    public override GridFightGameInfo ToProto()
    {
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();

        return new GridFightGameInfo
        {
            GridTraitGameInfo = new GridFightGameTraitInfo
            {
                GridFightTraitInfo = { Data.Traits.Select(x => x.ToProto(roleComp)) }
            }
        };
    }
}

public static class GridFightTraitInfoPbExtensions
{
    public static GridGameTraitInfo ToProto(this GridFightGameTraitPb info, GridFightRoleComponent roleComp)
    {
        var traitRoles = roleComp.Data.Roles.Where(x =>
            GameData.GridFightRoleBasicInfoData.GetValueOrDefault(x.RoleId)?.TraitList.Contains(info.TraitId) == true).ToList();

        return new GridGameTraitInfo
        {
            TraitId = info.TraitId,
            TraitEffectLayer = info.TraitLayer,
            GridFightTraitMemberUniqueIdList = { traitRoles.Select(x => x.UniqueId) },
            TraitEffectList = { info.Effects.Select(x => x.ToProto())}
        };
    }

    public static GridFightTraitSyncInfo ToSyncInfo(this GridFightGameTraitEffectPb info)
    {
        return new GridFightTraitSyncInfo
        {
            TraitId = info.TraitId,
            TraitEffectInfo = info.ToProto()
        };
    }

    public static BattleGridFightTraitInfo ToBattleInfo(this GridFightGameTraitPb info, GridFightRoleComponent roleComp)
    {
        var onGroundRoles = roleComp.Data.Roles.Where(x => x.Pos <= GridFightRoleComponent.PrepareAreaPos).ToList();
        var traitRoles = onGroundRoles.Where(x => GameData.GridFightRoleBasicInfoData
            .GetValueOrDefault(x.RoleId)?.TraitList.Contains(info.TraitId) == true).ToList();

        List<GridFightRoleInfoPb> equipmentTraits = [];
        // check equipment traits
        foreach (var role in onGroundRoles.Except(traitRoles))
        {
            var roleExcel = GameData.GridFightRoleBasicInfoData.GetValueOrDefault(role.RoleId);
            if (roleExcel == null) continue;

            foreach (var equipmentUid in role.EquipmentIds)
            {
                // get item
                var equipmentItem = roleComp.Inst.GetComponent<GridFightItemsComponent>().Data.EquipmentItems
                    .FirstOrDefault(x => x.UniqueId == equipmentUid);
                if (equipmentItem == null) continue;

                // get conf
                var equipmentExcel = GameData.GridFightEquipmentData.GetValueOrDefault(equipmentItem.ItemId);
                if (equipmentExcel == null) continue;

                // category (emblem)
                if (equipmentExcel.EquipCategory != GridFightEquipCategoryEnum.Emblem) continue;

                if (equipmentExcel.EquipFuncParamList.Contains(info.TraitId))
                {
                    // we can add this role directly becuz foreach has Except option
                    equipmentTraits.Add(role);
                }
            }
        }

        // check for phainon
        var phainonRole = roleComp.Data.Roles.FirstOrDefault(x =>
            x.Pos <= GridFightRoleComponent.PrepareAreaPos && x.RoleId == 1408);  // hardcode

        var res = new BattleGridFightTraitInfo
        {
            TraitId = info.TraitId,
            TraitEffectLayer = info.TraitLayer,
            MemberList =
            {
                traitRoles.Select(x => new GridFightTraitMember
                {
                    GridUpdateSrc = GridFightTraitSrc.KGridFightTraitSrcRole,
                    MemberRoleId = x.RoleId,
                    MemberRoleUniqueId = x.UniqueId,
                    MemberType = GridFightTraitMemberType.KGridFightTraitMemberRole
                }),
                equipmentTraits.Select(x => new GridFightTraitMember
                {
                    GridUpdateSrc = GridFightTraitSrc.KGridFightTraitSrcEquip,
                    MemberRoleId = x.RoleId,
                    MemberRoleUniqueId = x.UniqueId,
                    MemberType = GridFightTraitMemberType.KGridFightTraitMemberRole
                })
            },
            TraitEffectList = { info.Effects.Select(x => x.ToBattleInfo(roleComp)) }
        };

        if (phainonRole != null && traitRoles.Concat(equipmentTraits).All(x => x.UniqueId != phainonRole.UniqueId))
        {
            res.MemberList.Add(new GridFightTraitMember
            {
                GridUpdateSrc = GridFightTraitSrc.KGridFightTraitSrcDummy,
                MemberRoleId = phainonRole.RoleId,
                MemberRoleUniqueId = phainonRole.UniqueId,
                MemberType = GridFightTraitMemberType.KGridFightTraitMemberRole
            });
        }

        return res;
    }

    public static GridFightTraitEffectInfo ToProto(this GridFightGameTraitEffectPb info)
    {
        var proto = new GridFightTraitEffectInfo
        {
            EffectId = info.EffectId,
            TraitEffectLevelExp = info.Threshold,
            TraitCoreRole = info.CoreRoleUniqueId
        };

        if (info.HasThreshold)
            proto.TraitEffectLevelExp = info.Threshold;

        if (info.HasCoreRoleUniqueId)
            proto.TraitCoreRole = info.CoreRoleUniqueId;

        return proto;
    }

    public static BattleGridFightTraitEffectInfo ToBattleInfo(this GridFightGameTraitEffectPb info, GridFightRoleComponent roleComp)
    {
        var proto = new BattleGridFightTraitEffectInfo
        {
            EffectId = info.EffectId
        };

        if (info.HasCoreRoleUniqueId)
        {
            var role = roleComp.Data.Roles.FirstOrDefault(x => x.UniqueId == info.CoreRoleUniqueId);
            if (role != null)
                proto.TraitCoreRole = new BattleGridFightTraitCoreRoleInfo
                {
                    UniqueId = role.UniqueId,
                    RoleBasicId = role.RoleId
                };
        }

        if (info.HasThreshold)
        {
            proto.TraitEffectLevelInfo = new GridFightTraitEffectLevelInfo
            {
                TraitEffectLevelExp = info.Threshold
            };
        }

        return proto;
    }
}