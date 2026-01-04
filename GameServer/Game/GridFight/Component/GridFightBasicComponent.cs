using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Component;

public class GridFightBasicComponent(GridFightInstance inst) : BaseGridFightComponent(inst)
{
    #region Fields & Properties

    public const uint MaxHp = 100;

    public GridFightBasicInfoPb Data { get; set; } = new()
    {
        CurHp = 100,
        CurLevel = 3,
        MaxAvatarNum = 3,
        BuyLevelCost = 4,
        CurGold = 0,
        MaxInterest = 5,
        MaxLevel = 10,
        OffFieldAvatarNum = 6
    };

    #endregion

    #region Data Management

    public async ValueTask<Retcode> UpdateGoldNum(int changeNum, bool sendPacket = true, GridFightSrc src = GridFightSrc.KGridFightSrcNone)
    {
        if (changeNum < 0 && -changeNum > Data.CurGold)
        {
            return Retcode.RetGridFightCoinNotEnough;
        }

        Data.CurGold = (uint)(Data.CurGold + changeNum);

        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(new GridFightGoldSyncData(src, Data)));
        }

        return Retcode.RetSucc;
    }

    public async ValueTask<Retcode> UpdateLineupHp(int changeNum, bool sendPacket = true, GridFightSrc src = GridFightSrc.KGridFightSrcBattleEnd)
    {
        Data.CurHp = (uint)Math.Min(Math.Max(Data.CurHp + changeNum, 0), MaxHp);

        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(new GridFightLineupHpSyncData(src, Data)));
        }

        return Retcode.RetSucc;
    }

    public async ValueTask<Retcode> BuyLevelExp(bool sendPacket = true)
    {
        if (!GameData.GridFightPlayerLevelData.TryGetValue(Data.CurLevel, out var levelConf) || levelConf.LevelUpExp == 0)
            return Retcode.RetGridFightGameplayLevelMax;

        // COST
        if (await UpdateGoldNum((int)-Data.BuyLevelCost, false) != Retcode.RetSucc)
            return Retcode.RetGridFightCoinNotEnough;

        return await AddLevelExp(4, sendPacket);
    }

    public async ValueTask<Retcode> AddLevelExp(uint exp, bool sendPacket = true)
    {
        var upperLevels = GameData.GridFightPlayerLevelData.Values.Where(x => x.PlayerLevel >= Data.CurLevel)
            .OrderBy(x => x.PlayerLevel).ToList();

        if (upperLevels.Count == 1)  // 1 contain cur level
            return Retcode.RetGridFightGameplayLevelMax;  // already max level

        Data.LevelExp += exp;

        // LEVEL UP
        var costExp = 0;
        var targetLevel = Data.CurLevel;

        foreach (var level in upperLevels)
        {
            if (level.LevelUpExp + costExp > Data.LevelExp)
                break;

            if (level.LevelUpExp == 0) continue;  // max level

            costExp += (int)level.LevelUpExp;
            targetLevel = level.PlayerLevel + 1;
        }

        if (targetLevel > Data.CurLevel)
        {
            await UpgradeLevel(targetLevel - Data.CurLevel, false);
        }

        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
                new GridFightGoldSyncData(GridFightSrc.KGridFightSrcNone, Data),
                new GridFightPlayerLevelSyncData(GridFightSrc.KGridFightSrcNone, Data),
                new GridFightMaxAvatarNumSyncData(GridFightSrc.KGridFightSrcNone, Data),
                new GridFightBuyExpCostSyncData(GridFightSrc.KGridFightSrcNone, Data)));
        }

        return Retcode.RetSucc;
    }

    public async ValueTask<Retcode> UpgradeLevel(uint level, bool sendPacket = true)
    {
        if (!GameData.GridFightPlayerLevelData.TryGetValue(level + Data.CurLevel, out var levelConf))
            return Retcode.RetGridFightGameplayLevelMax;

        // adjust exp and other stats
        for (var i = Data.CurLevel; i < level + Data.CurLevel; i++)
        {
            if (!GameData.GridFightPlayerLevelData.TryGetValue(i, out var curLevelConf))
                break;

            Data.LevelExp -= curLevelConf.LevelUpExp;
        }

        Data.CurLevel += level;

        Data.MaxAvatarNum = levelConf.AvatarMaxNumber;

        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
                new GridFightGoldSyncData(GridFightSrc.KGridFightSrcNone, Data),
                new GridFightPlayerLevelSyncData(GridFightSrc.KGridFightSrcNone, Data),
                new GridFightMaxAvatarNumSyncData(GridFightSrc.KGridFightSrcNone, Data),
                new GridFightBuyExpCostSyncData(GridFightSrc.KGridFightSrcNone, Data)));
        }

        return Retcode.RetSucc;
    }

    #endregion

    #region Information

    public uint GetFieldCount()
    {
        return 4 + Data.OffFieldAvatarNum;
    }

    #endregion

    #region Serialization

    public override GridFightGameInfo ToProto()
    {
        var roleComp = Inst.GetComponent<GridFightRoleComponent>();
        var itemsComp = Inst.GetComponent<GridFightItemsComponent>();

        return new GridFightGameInfo
        {
            GridBasicInfo = new GridFightGameBasicInfo
            {
                GridFightCurLevel = Data.CurLevel,
                GridFightCurLevelExp = Data.LevelExp,
                GridFightLevelCost = Data.BuyLevelCost,
                GridFightMaxAvatarCount = 9,
                GridFightOffFieldMaxCount = Math.Min(9, Data.OffFieldAvatarNum),
                GridFightMaxFieldCount = Math.Min(13, Data.MaxAvatarNum),
                GridFightLineupHp = Data.CurHp,
                GridFightCurGold = Data.CurGold,
                GridFightMaxInterestGold = Data.MaxInterest,
                GridFightComboWinNum = Data.ComboNum,
                OCMGMEHECBB = new OPIBBPCHFII
                {
                },
                GameLockInfo = new GridFightLockInfo
                {
                    LockReason = (GridFightLockReason)Data.LockReason,
                    LockType = (GridFightLockType)Data.LockType
                },
                GridFightTargetGuideCode = Data.GuideCode,
                TrackTraitIdList = { Data.TrackingTraits },
                RoleTrackEquipmentList = { Data.TrackingEquipments.Select(x => x.ToProto(roleComp, itemsComp)) },
                GridFightMaxLevel = Data.MaxLevel
            }
        };
    }

    #endregion
}

public static class GridFightBasicComponentExtensions
{
    public static RoleTrackEquipmentInfo ToProto(this GridFightEquipmentTrackInfoPb equip, GridFightRoleComponent role, GridFightItemsComponent items)
    {
        var info = new RoleTrackEquipmentInfo
        {
            TrackRoleId = equip.RoleId,
            TrackPriority = equip.Priority,
            GridFightItemList = { equip.EquipmentIds },
        };

        var targetRole = role.Data.Roles
            .FirstOrDefault(x => x.RoleId == equip.RoleId && x.Pos <= GridFightRoleComponent.PrepareAreaPos);

        if (targetRole == null) return info;
        
        foreach (var uniqueId in targetRole.EquipmentIds)
        {
            var item = items.Data.EquipmentItems.FirstOrDefault(x => x.UniqueId == uniqueId);
            if (item != null && equip.EquipmentIds.Contains(item.ItemId))
            {
                info.TrackEquippedIdList.Add(item.ItemId);
            }
        }

        return info;
    }
}