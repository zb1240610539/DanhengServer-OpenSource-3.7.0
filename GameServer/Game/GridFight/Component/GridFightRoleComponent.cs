using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database.Avatar;
using EggLink.DanhengServer.Enums.GridFight;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Component;

public class GridFightRoleComponent(GridFightInstance inst) : BaseGridFightComponent(inst)
{
    public const uint PrepareAreaPos = 13;
    public GridFightTeamInfoPb Data { get; set; } = new();

    #region Role

    public uint GetEmptyPos()
    {
        var usedPos = Data.Roles.Select(x => x.Pos).Concat(Data.Forges.Select(x => x.Pos))
            .Concat(Data.Npcs.Select(x => x.Pos)).ToHashSet();

        var pos = 0u;
        for (var i = PrepareAreaPos + 1; i <= PrepareAreaPos + 999; i++)  // temp store area
        {
            if (usedPos.Contains(i)) continue;
            pos = i;
            break;
        }

        return pos;
    }

    public async ValueTask<List<BaseGridFightSyncData>> AddAvatar(uint roleId, uint tier = 1, bool sendPacket = true,
        bool checkMerge = true, GridFightSrc src = GridFightSrc.KGridFightSrcBuyGoods, uint syncGroup = 0, uint targetPos = 0, List<uint>? equipments = null, params uint[] param)
    {
        if (!GameData.GridFightRoleBasicInfoData.TryGetValue(roleId, out var excel)) return [];

        var pos = 0u;
        var initialPos = targetPos > 0 ? targetPos : PrepareAreaPos + 1;

        // get first empty pos
        var usedPos = Data.Roles.Select(x => x.Pos).Concat(Data.Forges.Select(x => x.Pos))
            .Concat(Data.Npcs.Select(x => x.Pos)).ToHashSet();

        for (var i = initialPos; i <= PrepareAreaPos + 999; i++)  // temp store area
        {
            if (usedPos.Contains(i)) continue;
            pos = i;
            break;
        }

        // check if any empty
        if (pos == 0)
        {
            return [];
        }

        var info = new GridFightRoleInfoPb
        {
            RoleId = roleId,
            UniqueId = ++Data.CurUniqueId,
            Tier = tier,
            Pos = pos,
            EquipmentIds = { equipments ?? [] }
        };

        foreach (var saved in excel.RoleSavedValueList)
        {
            info.SavedValues.Add(saved, 0);
        }

        Data.Roles.Add(info);

        List<BaseGridFightSyncData> syncs = [new GridFightRoleAddSyncData(src, info, syncGroup, param)];

        if (checkMerge)
        {
            var mergeSyncs = await CheckIfMergeRole();
            syncs.AddRange(mergeSyncs);
        }

        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
        }

        await Inst.GetComponent<GridFightTraitComponent>().CheckTrait();

        return syncs;
    }

    public async ValueTask<List<BaseGridFightSyncData>> CheckIfMergeRole(bool sendPacket = false)
    {
        var itemsComp = Inst.GetComponent<GridFightItemsComponent>();
        List<BaseGridFightSyncData> syncs = [];
        bool hasMerged;
        uint groupId = 0;

        do
        {
            hasMerged = false;

            // group roles by RoleId and Tier, then filter groups with 3 or more roles
            var mergeCandidates = Data.Roles
                .GroupBy(r => new { r.RoleId, r.Tier })
                .Where(g => g.Count() >= 3)
                .Where(g =>
                {
                    // check if next tier exists
                    var nextTierKey = g.Key.RoleId << 4 | (g.Key.Tier + 1);
                    return GameData.GridFightRoleStarData.ContainsKey(nextTierKey);
                })
                .OrderBy(g => g.Key.RoleId)
                .ThenBy(g => g.Key.Tier)
                .FirstOrDefault(); // process one group at a time to handle continuous merging

            if (mergeCandidates != null)
            {
                var roleId = mergeCandidates.Key.RoleId;
                var currentTier = mergeCandidates.Key.Tier;
                var toMerge = mergeCandidates.Take(3).ToList();
                List<uint> equipments = [];
                
                // remove merged roles
                foreach (var role in toMerge)
                {
                    Data.Roles.Remove(role);
                    syncs.Add(new GridFightRoleRemoveSyncData(GridFightSrc.KGridFightSrcMergeRole, role, groupId));

                    // unequip
                    foreach (var u in role.EquipmentIds.Clone())
                    {
                        if (equipments.Count < 3)
                        {
                            // add
                            equipments.Add(u);
                        }
                        else
                        {
                            // unequip
                            syncs.AddRange(await itemsComp.OnEquipmentUnEquipped(u, role));
                        }
                    }
                }

                // add new merged role with tier + 1
                var addSyncs = await AddAvatar(roleId, currentTier + 1, false, false,
                    GridFightSrc.KGridFightSrcMergeRole, groupId, toMerge.First().Pos, equipments);
                syncs.AddRange(addSyncs);

                groupId++;
                hasMerged = true;
            }
        } while (hasMerged); // continue until no more merges are possible

        if (sendPacket && syncs.Count > 0)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
        }

        return syncs;
    }

    public async ValueTask<List<BaseGridFightSyncData>> SellAvatar(uint uniqueId, bool sendPacket = true)
    {
        var itemsComp = Inst.GetComponent<GridFightItemsComponent>();
        var role = Data.Roles.FirstOrDefault(x => x.UniqueId == uniqueId);
        if (role == null)
        {
            return [];
        }

        Data.Roles.Remove(role);

        var tier = role.Tier;
        var rarity = GameData.GridFightRoleBasicInfoData[role.RoleId].Rarity;

        var sellPrice = GameData.GridFightShopPriceData.GetValueOrDefault(rarity)
            ?.SellGoldList[(int)(tier - 1)] ?? 1;

        var basicComp = Inst.GetComponent<GridFightBasicComponent>();
        await basicComp.UpdateGoldNum((int)sellPrice, false, GridFightSrc.KGridFightSrcRecycleRole);


        List<BaseGridFightSyncData> syncs =
        [
            new GridFightRoleRemoveSyncData(GridFightSrc.KGridFightSrcRecycleRole, role),
            new GridFightGoldSyncData(GridFightSrc.KGridFightSrcNone, basicComp.Data)
        ];

        foreach (var u in role.EquipmentIds.Clone())
        {
            syncs.AddRange(await itemsComp.OnEquipmentUnEquipped(u, role));
        }

        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
        }

        await Inst.GetComponent<GridFightTraitComponent>().CheckTrait();

        return syncs;
    }

    public async ValueTask<List<BaseGridFightSyncData>> DressRole(uint uniqueId, uint equipmentUniqueId,
        GridFightSrc src = GridFightSrc.KGridFightSrcDressEquip,
        bool sendPacket = true, params uint[] param)
    {
        var role = Data.Roles.FirstOrDefault(x => x.UniqueId == uniqueId);
        if (role == null)
        {
            return [];
        }

        // check if equipment exists & not already dressed
        var itemComp = Inst.GetComponent<GridFightItemsComponent>();
        var equipment = itemComp.Data.EquipmentItems.FirstOrDefault(x => x.UniqueId == equipmentUniqueId);
        if (equipment == null ||
            Data.Roles.Any(x => x.EquipmentIds.Contains(equipmentUniqueId))) // already dressed or not exist
        {
            return [];
        }

        role.EquipmentIds.Add(equipmentUniqueId); // ensure no duplicates

        // handle action
        var res = await itemComp.OnEquipmentEquipped(equipmentUniqueId, role);

        res.Add(new GridFightRoleUpdateSyncData(src, role, 0, param));
        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(res));
        }

        await Inst.GetComponent<GridFightTraitComponent>().CheckTrait();

        return res;
    }

    public List<BaseAvatarInfo> GetForegroundAvatarInfos()
    {
        var foreground = Data.Roles.Where(x => x.Pos <= 4).OrderBy(x => x.Pos).ToList();
        List<BaseAvatarInfo> res = [];

        foreach (var role in foreground)
        {
            var excel = GameData.GridFightRoleBasicInfoData[role.RoleId];
            // get formal or special
            var formal = Inst.Player.AvatarManager!.GetFormalAvatar((int)excel.AvatarID);
            if (formal != null)
            {
                res.Add(formal);
            }
            else
            {
                var special = Inst.Player.AvatarManager.GetTrialAvatar((int)excel.SpecialAvatarID);
                if (special != null)
                {
                    res.Add(special);
                }
            }
        }

        return res;
    }

    public List<BaseAvatarInfo> GetBackgroundAvatarInfos(uint maxAvatarNum)
    {
        var foreground = Data.Roles.Where(x => x.Pos <= maxAvatarNum && x.Pos > 4).OrderBy(x => x.Pos).ToList();
        List<BaseAvatarInfo> res = [];

        foreach (var role in foreground)
        {
            var excel = GameData.GridFightRoleBasicInfoData[role.RoleId];
            // get formal or special
            var formal = Inst.Player.AvatarManager!.GetFormalAvatar((int)excel.AvatarID);
            if (formal != null)
            {
                res.Add(formal);
            }
            else
            {
                var special = Inst.Player.AvatarManager.GetTrialAvatar((int)excel.SpecialAvatarID);
                if (special != null)
                {
                    res.Add(special);
                }
            }
        }

        return res;
    }

    #endregion

    #region Forge

    public async ValueTask<List<BaseGridFightSyncData>> AddForgeItem(uint forgeItemId, bool sendPacket = true,
        GridFightSrc src = GridFightSrc.KGridFightSrcNone, uint syncGroup = 0, uint targetPos = 0, params uint[] param)
    {
        if (!GameData.GridFightForgeData.TryGetValue(forgeItemId, out var forgeExcel)) return [];

        var pos = 0u;
        var initialPos = targetPos > 0 ? targetPos : PrepareAreaPos + 1;

        // get first empty pos
        var usedPos = Data.Roles.Select(x => x.Pos).Concat(Data.Forges.Select(x => x.Pos))
            .Concat(Data.Npcs.Select(x => x.Pos)).ToHashSet();
        for (var i = initialPos; i <= PrepareAreaPos + 999; i++) // temp store area
        {
            if (usedPos.Contains(i)) continue;
            pos = i;
            break;
        }

        // check if any empty
        if (pos == 0)
        {
            return [];
        }

        var info = new GridFightForgeInfoPb
        {
            ForgeItemId = forgeItemId,
            UniqueId = ++Data.CurUniqueId,
            Pos = pos
        };

        // generate goods
        if (forgeExcel.FuncType == GridFightForgeFuncTypeEnum.Role)
        {
            var roleRarity = forgeExcel.ParamList[0];
            var tier = forgeExcel.ParamList[1];

            var candidateRoles = GameData.GridFightRoleBasicInfoData.Values
                .Where(x => x.Rarity == roleRarity)
                .ToList(); // filter

            if (candidateRoles.Count == 0) return []; // no candidate roles (should not happen)

            for (var i = 0; i < forgeExcel.EquipNum; i++)
            {
                var role = candidateRoles.RandomElement();

                info.Goods.Add(new GridFightForgeGoodsInfoPb
                {
                    RoleInfo = new GridFightForgeRoleGoodsInfoPb
                    {
                        RoleId = role.ID,
                        Tier = tier
                    }
                });
            }
        }
        else if (forgeExcel.FuncType == GridFightForgeFuncTypeEnum.Equip)
        {
            var equipCategory = forgeExcel.ParamList[0];
            var candidateEquips = GameData.GridFightEquipmentData.Values
                .Where(x => (uint)x.EquipCategory == equipCategory)
                .ToList(); // filter

            if (candidateEquips.Count == 0) return []; // no candidate equips (should not happen)

            for (var i = 0; i < forgeExcel.EquipNum; i++)
            {
                var equip = candidateEquips.RandomElement();
                info.Goods.Add(new GridFightForgeGoodsInfoPb
                {
                    ItemId = equip.ID
                });
            }
        }

        Data.Forges.Add(info);

        List<BaseGridFightSyncData> syncs = [new GridFightAddForgeSyncData(src, info, syncGroup, param)];

        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
        }

        return syncs;
    }

    public async ValueTask<List<BaseGridFightSyncData>> UseForgeItem(uint uniqueId, uint targetIndex)
    {
        var forge = Data.Forges.FirstOrDefault(x => x.UniqueId == uniqueId);
        if (forge == null)
        {
            return [];
        }

        List<BaseGridFightSyncData> syncs = [];

        var good = forge.Goods[(int)targetIndex];
        if (good.HasItemId)
        {
            // equipment
            var addEquipSyncs = await Inst.GetComponent<GridFightItemsComponent>()
                .AddEquipment(good.ItemId, GridFightSrc.KGridFightSrcUseForge, false, uniqueId);

            syncs.AddRange(addEquipSyncs.Item2);
        }
        else
        {
            // role
            var addRoleSyncs = await AddAvatar(good.RoleInfo.RoleId, good.RoleInfo.Tier, true,
                false, GridFightSrc.KGridFightSrcUseForge, uniqueId);

            syncs.AddRange(addRoleSyncs);
        }

        // remove used forge item
        Data.Forges.Remove(forge);
        syncs.Add(new GridFightRemoveForgeSyncData(GridFightSrc.KGridFightSrcUseForge, forge, uniqueId, uniqueId,
            forge.ForgeItemId));

        await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
        return syncs;
    }

    #endregion

    #region Metadata

    public bool HasAnyEmptyPos()
    {
        return Data.Roles.Where(x => x.Pos > PrepareAreaPos).ToList().Count < 9;
    }

    public uint GetEmptyPosCount()
    {
        return (uint)(9 - Data.Roles.Where(x => x.Pos > PrepareAreaPos).ToList().Count);
    }

    public async ValueTask<Retcode> UpdatePos(List<GridFightPosInfo> posList)
    {
        foreach (var pos in posList.Where(x => x.Pos <= PrepareAreaPos))
        {
            var role = Data.Roles.FirstOrDefault(x => x.UniqueId == pos.UniqueId);
            if (role == null) continue;

            if (Data.Roles.Where(x => x.UniqueId != pos.UniqueId && x.Pos <= PrepareAreaPos).Any(x =>
                    GameData.GridFightRoleBasicInfoData.GetValueOrDefault(x.RoleId)?.AvatarID ==
                    GameData.GridFightRoleBasicInfoData.GetValueOrDefault(role.RoleId)?.AvatarID)) 
                return Retcode.RetGridFightSameRoleInBattle;
        }  // only check role

        List<BaseGridFightSyncData> syncs = [];
        foreach (var pos in posList)
        {
            var role = Data.Roles.FirstOrDefault(x => x.UniqueId == pos.UniqueId);
            var forge = Data.Forges.FirstOrDefault(x => x.UniqueId == pos.UniqueId);
            var npc = Data.Npcs.FirstOrDefault(x => x.UniqueId == pos.UniqueId);

            if (role != null)
            {
                role.Pos = pos.Pos;
                syncs.Add(new GridFightRoleUpdateSyncData(GridFightSrc.KGridFightSrcNone, role));
            }

            if (forge != null)
            {
                forge.Pos = pos.Pos;
                syncs.Add(new GridFightForgeUpdateSyncData(GridFightSrc.KGridFightSrcNone, forge));
            }

            if (npc != null)
            {
                npc.Pos = pos.Pos;
                syncs.Add(new GridFightNpcUpdateSyncData(GridFightSrc.KGridFightSrcNone, npc));
            }
        }

        if (syncs.Count > 0)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
        }

        await Inst.GetComponent<GridFightTraitComponent>().CheckTrait();

        return Retcode.RetSucc;
    }

    #endregion

    public override GridFightGameInfo ToProto()
    {
        return new GridFightGameInfo
        {
            GridTeamGameInfo = new GridFightGameTeamInfo
            {
                GridGameRoleList = { Data.Roles.Select(x => x.ToProto()) },
                GridGameForgeItemList = { Data.Forges.Select(x => x.ToProto()) },
                GridGameNpcList = { Data.Npcs.Select(x => x.ToProto()) }
            }
        };
    }
}

public static class GridFightRoleInfoPbExtensions
{
    public static GridGameRoleInfo ToProto(this GridFightRoleInfoPb info)
    {
        return new GridGameRoleInfo
        {
            Id = info.RoleId,
            UniqueId = info.UniqueId,
            Tier = info.Tier,
            Pos = info.Pos,
            GameSavedValueMap = { info.SavedValues },
            EquipUniqueIdList = { info.EquipmentIds }
        };
    }

    public static GridGameNpcInfo ToProto(this GridFightNpcInfoPb info)
    {
        return new GridGameNpcInfo
        {
            Id = info.NpcId,
            UniqueId = info.UniqueId,
            Pos = info.Pos,
            EquipUniqueIdList = { info.EquipmentIds }
        };
    }

    public static GridGameForgeItemInfo ToProto(this GridFightForgeInfoPb info)
    {
        return new GridGameForgeItemInfo
        {
            ForgeItemId = info.ForgeItemId,
            UniqueId = info.UniqueId,
            Pos = info.Pos,
            ForgeGoodsList = { info.Goods.Select(x => x.ToProto()) }
        };
    }

    public static GridFightForgeGoodsInfo ToProto(this GridFightForgeGoodsInfoPb info)
    {
        var proto = new GridFightForgeGoodsInfo();

        if (info.HasItemId)
        {
            proto.EquipmentGoodsInfo = new GridFightForgeEquipmentInfo
            {
                GridFightEquipmentId = info.ItemId
            };
        }
        else
        {
            proto.RoleGoodsInfo = new GridFightForgeRoleInfo
            {
                RoleBasicId = info.RoleInfo.RoleId,
                ForgeRoleTier = info.RoleInfo.Tier
            };
        }

        return proto;
    }

    public static BattleGridFightRoleInfo ToBattleInfo(this GridFightRoleInfoPb info, GridFightItemsInfoPb item)
    {
        return new BattleGridFightRoleInfo
        {
            RoleBasicId = info.RoleId,
            UniqueId = info.UniqueId,
            Tier = info.Tier,
            Pos = info.Pos,
            AvatarId = GameData.GridFightRoleBasicInfoData[info.RoleId].AvatarID,
            RoleEquipmentList =
            {
                item.EquipmentItems.Where(x => info.EquipmentIds.Contains(x.UniqueId)).Select(x => x.ToBattleInfo())
            },
            GameSavedValueMap = { info.SavedValues }
        };
    }

    public static BattleGridFightNpcInfo ToBattleInfo(this GridFightNpcInfoPb info, GridFightItemsInfoPb item)
    {
        return new BattleGridFightNpcInfo
        {
            NpcId = info.NpcId,
            UniqueId = info.UniqueId,
            Pos = info.Pos,
            GridFightEquipmentList =
            {
                item.EquipmentItems.Where(x => info.EquipmentIds.Contains(x.UniqueId)).Select(x => x.ToBattleInfo())
            }
        };
    }
}