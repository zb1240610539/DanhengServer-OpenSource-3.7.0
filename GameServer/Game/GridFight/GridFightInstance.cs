using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.Battle;
using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.GameServer.Game.GridFight.PendingAction;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.GridFight;

public class GridFightInstance(PlayerInstance player, uint season, uint divisionId, bool isOverLock, uint uniqueId)
{
    public uint Season { get; } = season;
    public uint DivisionId { get; } = divisionId;
    public bool IsOverLock { get; } = isOverLock;
    public uint UniqueId { get; } = uniqueId;
    public List<BaseGridFightComponent> Components { get; } = [];
    public PlayerInstance Player { get; } = player;

    public BattleInstance? StartBattle()
    {
        var battle = Player.BattleManager!.StartGridFightBattle(this);

        return battle;
    }

    public async ValueTask EndBattle(BattleInstance battle, PVEBattleResultCsReq req)
    {
        if (battle.BattleEndStatus == BattleEndStatus.BattleEndQuit) return;

        List<BaseGridFightSyncData> syncs = [];

        var basicComp = GetComponent<GridFightBasicComponent>();
        var levelComp = GetComponent<GridFightLevelComponent>();
        var traitComp = GetComponent<GridFightTraitComponent>();
        var itemsComponent = GetComponent<GridFightItemsComponent>();
        var prevData = basicComp.Data.Clone();
        var curEncounter = levelComp.CurrentSection.Encounters[(int)(levelComp.CurrentSection.BranchId - 1)];

        var expNum = 2u;
        var baseCoin = levelComp.CurrentSection.Excel.BasicGoldRewardNum;
        var interestCoin = Math.Min(basicComp.Data.CurGold / 10, basicComp.Data.MaxInterest);
        var progress = req.Stt.GridFightBattleStt.FinishProgress;

        if (progress == 100)
        {
            basicComp.Data.ComboNum++;
        }
        else
        {
            basicComp.Data.ComboNum = 0;

            // cost hp
            await basicComp.UpdateLineupHp(-5, false);
        }

        var end = levelComp.IsLastSection();

        var comboCoin = basicComp.Data.ComboNum switch
        {
            >= 5 => 3u,
            2 or 3 or 4 => 2u,
            0 => 0u,
            _ => 1u
        };

        await basicComp.UpdateGoldNum((int)(baseCoin + interestCoin + comboCoin), false);
        await basicComp.AddLevelExp(expNum, false);

        List<GridFightRoleDamageSttInfo> sttList = [];
        foreach (var roleBattleStt in req.Stt.GridFightBattleStt.RoleBattleStt)
        {
            var res = await levelComp.AddRoleDamageStt(roleBattleStt.RoleBasicId, roleBattleStt.Damage, false);
            if (res.Item2 != null)
                sttList.Add(res.Item2);
        }

        List<GridFightTraitDamageSttInfo> traitSttList = [];
        foreach (var traitBattleStt in req.Stt.GridFightBattleStt.TraitBattleStt)
        {
            var res = await levelComp.AddTraitDamageStt(traitBattleStt.TraitId, traitBattleStt.Damage, false);
            if (res.Item2 != null)
                traitSttList.Add(res.Item2);
        }

        var curData = basicComp.Data.Clone();

        // if any drop
        var drops = await curEncounter.TakeMonsterDrop(itemsComponent);

        await Player.SendPacket(new PacketGridFightEndBattleStageNotify(this, expNum, prevData, curData,
            sttList, traitSttList, battle.BattleEndStatus == BattleEndStatus.BattleEndWin, baseCoin, interestCoin,
            comboCoin, drops.Item2, progress));

        syncs.AddRange(drops.Item1);

        syncs.Add(new GridFightGoldSyncData(GridFightSrc.KGridFightSrcBattleEnd, basicComp.Data, 0, levelComp.CurrentSection.ChapterId, levelComp.CurrentSection.SectionId));
        syncs.Add(new GridFightPlayerLevelSyncData(GridFightSrc.KGridFightSrcBattleEnd, basicComp.Data));
        syncs.Add(new GridFightLineupHpSyncData(GridFightSrc.KGridFightSrcBattleEnd, basicComp.Data));
        syncs.Add(new GridFightComboNumSyncData(GridFightSrc.KGridFightSrcBattleEnd, basicComp.Data));
        syncs.Add(new GridFightRoleDamageSttSyncData(GridFightSrc.KGridFightSrcBattleEnd, levelComp));
        syncs.AddRange(await levelComp.EnterNextSection(false));

        // encounter drop
        syncs.AddRange(await curEncounter.TakeEncounterDrop(itemsComponent));

        await Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));

        // trait
        await traitComp.HandleBattleEnd(req, progress == 100);

        if (end)
        {
            // settle
            await Player.SendPacket(new PacketGridFightSettleNotify(this));
            Player.GridFightManager!.GridFightInstance = null;
        }
    }

    public void InitializeComponents()
    {
        Components.Add(new GridFightBasicComponent(this));
        Components.Add(new GridFightShopComponent(this));
        Components.Add(new GridFightLevelComponent(this));
        Components.Add(new GridFightRoleComponent(this));
        Components.Add(new GridFightAugmentComponent(this));
        Components.Add(new GridFightTraitComponent(this));
        Components.Add(new GridFightItemsComponent(this));
        Components.Add(new GridFightOrbComponent(this));

        _ = GetComponent<GridFightShopComponent>().RefreshShop(true, false);

        _ = CreatePendingAction<GridFightPortalBuffPendingAction>(sendPacket:false);
        _ = CreatePendingAction<GridFightRoundBeginPendingAction>(sendPacket: false);
        _ = CreatePendingAction<GridFightReturnPreparationPendingAction>(sendPacket: false);
    }

    public T GetComponent<T>() where T : BaseGridFightComponent
    {
        return (T)Components.First(c => c is T);
    }

    public uint GetDivisionDifficulty()
    {
        return GameData.GridFightDivisionStageData.TryGetValue(DivisionId, out var excel)
            ? excel.EnemyDifficultyLevel
            : 0;
    }

    public GridFightCurrentInfo ToProto()
    {
        return new GridFightCurrentInfo
        {
            DivisionId = DivisionId,
            Season = Season,
            IsOverlock = IsOverLock,
            UniqueId = UniqueId,
            PendingAction = GetCurAction().ToProto(),
            GridFightGameData = ToGameDataInfo(),
            RogueCurrentGameInfo = { ToGameInfos() }
        };
    }

    public GridFightFinishInfo ToFinishInfo()
    {
        var roleComp = GetComponent<GridFightRoleComponent>();
        var levelComp = GetComponent<GridFightLevelComponent>();
        var augmentComp = GetComponent<GridFightAugmentComponent>();
        var itemsComp = GetComponent<GridFightItemsComponent>();
        var traitComp = GetComponent<GridFightTraitComponent>();
        var basicComp = GetComponent<GridFightBasicComponent>();

        return new GridFightFinishInfo
        {
            Reason = GridFightSettleReason.KGridFightSettleReasonFinish,
            SettleRoleUniqueIdList = { roleComp.Data.Roles.Where(x => x.Pos <= GridFightRoleComponent.PrepareAreaPos).Select(x => x.UniqueId) },
            GridFightEquipmentList = { itemsComp.Data.EquipmentItems.Select(x => x.ToProto()) },
            GridFightAugmentInfo = { augmentComp.Data.Augments.Select(x => x.ToProto()) },
            SettlePortalBuffList = { levelComp.PortalBuffs.Select(x => x.ToProto()) },
            TraitDamageSttList = { levelComp.TraitDamageSttInfos.Select(x => x.ToProto(traitComp)) },
            RoleDamageSttList = { levelComp.RoleDamageSttInfos.Select(x => x.ToProto()) },
            GridFightTraitInfo = { traitComp.Data.Traits.Select(x => x.ToProto(roleComp)) },
            GridGameRoleList = { roleComp.Data.Roles.Select(x => x.ToProto()) },
            RogueTournCurAreaInfo = new GridFightFinishAreaInfo
            {
                ChapterId = levelComp.CurrentSection.ChapterId,
                GameDivisionId = DivisionId,
                GridFightCurLineupHp = basicComp.Data.CurHp,
                GridFightMaxLineupHp = 100,
                RouteId = levelComp.CurrentSection.Excel.ID,
                SectionId = levelComp.CurrentSection.SectionId
            }
        };
    }

    public List<GridFightGameInfo> ToGameInfos()
    {
        return (from c in Components select c.ToProto()).ToList();
    }

    public GridFightGameData ToGameDataInfo()
    {
        return new GridFightGameData();
    }

    #region Pending Action

    public SortedDictionary<uint, BaseGridFightPendingAction> PendingActions { get; set; } = new();
    private uint _curQueuePos = 1;

    public BaseGridFightPendingAction GetCurAction()
    {
        if (PendingActions.Count > 0)
        {
            return PendingActions.First().Value;
        }

        return new GridFightEmptyPendingAction(this);
    }

    public uint AddPendingAction(BaseGridFightPendingAction action)
    {
        var pos = _curQueuePos++;

        action.QueuePosition = pos;
        PendingActions[pos] = action;

        return pos;
    }

    public async ValueTask<List<BaseGridFightSyncData>> CreatePendingAction<T>(GridFightSrc src = GridFightSrc.KGridFightSrcEnterNode, bool sendPacket = true, params object[] initializeParam) where T: BaseGridFightPendingAction
    {
        object[] paramList = [this, ..initializeParam];
        var action = (T)Activator.CreateInstance(typeof(T), paramList)!;
        var basicComp = GetComponent<GridFightBasicComponent>();

        AddPendingAction(action);

        basicComp.Data.LockReason = (uint)GridFightLockReason.KGridFightLockReasonPendingAction;
        basicComp.Data.LockType = (uint)GridFightLockType.KGridFightLockTypeAll;

        var res = new GridFightPendingActionSyncData(src, action);
        if (sendPacket)
           await Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(res));

        return [res, new GridFightLockInfoSyncData(src, basicComp.Data.Clone())];
    }

    public async ValueTask HandleResultRequest(GridFightHandlePendingActionCsReq req)
    {
        var basicComp = GetComponent<GridFightBasicComponent>();
        var levelComp = GetComponent<GridFightLevelComponent>();
        var roleComp = GetComponent<GridFightRoleComponent>();
        var itemsComp = GetComponent<GridFightItemsComponent>();

        var curAction = GetCurAction();

        // end
        var isFinish = true;
        GridFightSrc src;

        List<BaseGridFightSyncData> syncs = [];

        switch (req.GridFightActionTypeCase)
        {
            case GridFightHandlePendingActionCsReq.GridFightActionTypeOneofCase.PortalBuffAction:
            {
                src = GridFightSrc.KGridFightSrcSelectPortalBuff;

                syncs.AddRange(await levelComp.AddPortalBuff(req.PortalBuffAction.SelectPortalBuffId, false, src));

                // initial supply
                await basicComp.UpdateGoldNum(5, false, GridFightSrc.KGridFightSrcInitialSupplySelect);
                syncs.Add(new GridFightGoldSyncData(GridFightSrc.KGridFightSrcInitialSupplySelect, basicComp.Data));

                var rolePool = GameData.GridFightRoleBasicInfoData.Values.Where(x => x.Rarity == 1).ToList();
                for (var i = 0; i < 2; i++)
                {
                    syncs.AddRange(await roleComp.AddAvatar(rolePool.RandomElement().ID, 1, false, true,
                        GridFightSrc.KGridFightSrcInitialSupplySelect));
                }

                syncs.AddRange(await itemsComp.UpdateConsumable(350102, 1,
                    GridFightSrc.KGridFightSrcInitialSupplySelect, false));

                break;
            }
            case GridFightHandlePendingActionCsReq.GridFightActionTypeOneofCase.PortalBuffRerollAction:
            {
                if (curAction is GridFightPortalBuffPendingAction portalBuffAction)
                {
                    isFinish = false;
                    await portalBuffAction.RerollBuff();
                }

                break;
            }
            case GridFightHandlePendingActionCsReq.GridFightActionTypeOneofCase.AugmentAction:
            {
                src = GridFightSrc.KGridFightSrcSelectAugment;

                syncs.AddRange(await GetComponent<GridFightAugmentComponent>()
                    .AddAugment(req.AugmentAction.AugmentId, false, src));
                break;
            }
            case GridFightHandlePendingActionCsReq.GridFightActionTypeOneofCase.RerollAugmentAction:
            {
                if (curAction is GridFightAugmentPendingAction augmentAction)
                {
                    isFinish = false;
                    await augmentAction.RerollAugment(req.RerollAugmentAction.AugmentId);
                }

                break;
            }
            case GridFightHandlePendingActionCsReq.GridFightActionTypeOneofCase.EliteBranchAction:
            {
                var target = req.EliteBranchAction.EliteBranchId;
                levelComp.CurrentSection.BranchId = target;
                // sync
                syncs.Add(new GridFightLevelSyncData(GridFightSrc.KGridFightSrcNone, levelComp));

                break;
            }
            case GridFightHandlePendingActionCsReq.GridFightActionTypeOneofCase.SupplyAction:
            {
                src = GridFightSrc.KGridFightSrcSelectSupply;

                PendingActions.Remove(curAction.QueuePosition);

                if (curAction is GridFightSupplyPendingAction supplyAction)
                {
                    foreach (var supply in req.SupplyAction.SelectSupplyIndexes)
                    {
                        var role = supplyAction.RoleList[(int)supply - 1];

                        syncs.AddRange(await roleComp.AddAvatar(role.RoleId, 1, false,
                            true,
                            GridFightSrc.KGridFightSrcSelectSupply, 0, 0, null,
                            req.SupplyAction.SelectSupplyIndexes.ToArray()));

                        // add equipment
                        var res = await itemsComp.AddEquipment(role.EquipmentId,
                            GridFightSrc.KGridFightSrcSelectSupply, false,
                            0, req.SupplyAction.SelectSupplyIndexes.ToArray());
                        syncs.AddRange(res.Item2);
                    }
                }

                syncs.AddRange(await CheckCurNodeFinish(src));

                break;
            }
            case GridFightHandlePendingActionCsReq.GridFightActionTypeOneofCase.RecommendEquipmentAction:
            {
                var target = req.RecommendEquipmentAction.SelectEquipmentId;

                var res = await itemsComp.AddEquipment(target, GridFightSrc.KGridFightSrcNone, false);
                syncs.AddRange(res.Item2);

                break;
            }
            case GridFightHandlePendingActionCsReq.GridFightActionTypeOneofCase.TraitAction:
            {
                if (curAction is GridFightTraitPendingAction traitAction)
                {
                    traitAction.Effect.CoreRoleUniqueId = req.TraitAction.UniqueId;

                    // sync
                    syncs.Add(new GridFightTraitSyncData(GridFightSrc.KGridFightSrcTraitEffectUpdate,
                        traitAction.Effect, 0, traitAction.Effect.TraitId, traitAction.Effect.EffectId));
                }

                break;
            }
        }

        if (isFinish)
        {
            PendingActions.Remove(curAction.QueuePosition);
            syncs.Add(new GridFightFinishPendingActionSyncData(GridFightSrc.KGridFightSrcNone,
                curAction.QueuePosition));

            // unlock
            basicComp.Data.LockReason = (uint)GridFightLockReason.KGridFightLockReasonUnknown;
            basicComp.Data.LockType = (uint)GridFightLockType.KGridFightLockTypeNone;

            syncs.Add(new GridFightLockInfoSyncData(GridFightSrc.KGridFightSrcNone, basicComp.Data.Clone()));
        }

        if (PendingActions.Count > 0)
        {
            basicComp.Data.LockReason = (uint)GridFightLockReason.KGridFightLockReasonPendingAction;
            basicComp.Data.LockType = (uint)GridFightLockType.KGridFightLockTypeAll;

            syncs.Add(new GridFightPendingActionSyncData(GridFightSrc.KGridFightSrcNone, GetCurAction(), 1));
            syncs.Add(new GridFightLockInfoSyncData(GridFightSrc.KGridFightSrcNone, basicComp.Data.Clone(), 1));
        }

        await Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncs));
    }

    public async ValueTask<List<BaseGridFightSyncData>> CheckCurNodeFinish(GridFightSrc src)
    {
        var levelComp = GetComponent<GridFightLevelComponent>();
        var curSection = levelComp.CurrentSection;

        if (curSection.Encounters.Count != 0) return [];

        if (PendingActions.Count != 0) return [];

        // next
        return await levelComp.EnterNextSection(src:GridFightSrc.KGridFightSrcNone);
    }

    #endregion
}