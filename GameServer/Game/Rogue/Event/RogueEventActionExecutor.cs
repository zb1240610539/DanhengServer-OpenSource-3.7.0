using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Custom;
using EggLink.DanhengServer.Enums.Rogue;
using EggLink.DanhengServer.Enums.TournRogue;
using EggLink.DanhengServer.GameServer.Game.RogueTourn;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Lineup;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Linq;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Rogue.Event;

public static class RogueEventActionExecutor
{
    public static async ValueTask ExecuteActions(BaseRogueInstance rogue, RogueEventInstance eventInst,
        List<RogueDialogueEventActionData> actionDatas)
    {
        foreach (var actionData in actionDatas)
        {
            await ExecuteAction(rogue, eventInst, actionData);
        }
    }

    public static async ValueTask ExecuteAction(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        switch (actionData.Name)
        {
            case RogueEventActionTypeEnum.ActionEnhanceRandomBuff:
                await ActionEnhanceRandomBuff(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionAddBuff:
                await ActionAddBuff(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionChangeMoney:
                await ActionChangeMoney(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionRandomAction:
                await ActionRandomAction(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionChangeLineupData:
                await ActionChangeLineupData(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionRemoveBuff:
                await ActionRemoveBuff(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionInvalidateOption:
                await ActionInvalidateOption(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionAddMiracle:
                await ActionAddMiracle(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionGetBuffInGroup:
                await ActionGetBuffInGroup(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionTriggerBattle:
                await ActionTriggerBattle(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionChangeMoneyRatio:
                await ActionChangeMoneyRatio(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionSetPrivateVariable:
                await ActionSetPrivateVariable(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionRandomActionByVariable:
                await ActionRandomActionByVariable(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionChangePrivateVariable:
                await ActionChangePrivateVariable(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionGetBuffByRandomCount:
                await ActionGetBuffByRandomCount(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionGetBuff:
                await ActionGetBuff(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionGetMiracleByCategory:
                await ActionGetMiracleByCategory(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionDropFormula:
                await ActionDropFormula(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionAddFormula:
                await ActionAddFormula(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionGetBuffInFormula:
                await ActionGetBuffInFormula(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionEnhanceAllBuff:
                await ActionEnhanceAllBuff(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionGetBuffInFormulaUntilExpandAll:
                await ActionGetBuffInFormulaUntilExpandAll(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionAddFormulaAndExpand:
                await ActionAddFormulaAndExpand(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionDropMiracle:
                await ActionDropMiracle(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionDropBuff:
                await ActionDropBuff(rogue, eventInst, actionData);
                break;
            case RogueEventActionTypeEnum.ActionAddDialogueEvent:
                await ActionAddDialogueEvent(rogue, eventInst, actionData);
                break;
            default:
                break;
        }
    }

    private static async ValueTask ActionEnhanceRandomBuff(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var count = Convert.ToInt32(actionData.Param.GetValueOrDefault("Count", 0));
        var groupId = Convert.ToInt32(actionData.Param.GetValueOrDefault("GroupId", 0));
        if (count <= 0) return;

        var group = GameData.RogueBuffGroupData.GetValueOrDefault(groupId)?.BuffList.Select(x => x.MazeBuffID).ToList();
        if (group == null && groupId != 0) return;

        var possibleBuffs = groupId == 0 || group == null
            ? rogue.RogueBuffs.Where(x => x.BuffLevel == 1).ToList()
            : rogue.RogueBuffs.Where(x => x.BuffLevel == 1 && group.Contains(x.BuffId)).ToList();

        var buffCount = possibleBuffs.Count;

        var selectedBuffs = possibleBuffs.OrderBy(_ => Guid.NewGuid()).Take(Math.Min(count, buffCount)).ToList();  // avoid buffCount < count

        await rogue.EnhanceBuffs(selectedBuffs.Select(x => x.BuffId).ToList());
    }

    private static async ValueTask ActionAddBuff(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var count = Convert.ToInt32(actionData.Param.GetValueOrDefault("Count", 0));
        var groupId = Convert.ToInt32(actionData.Param.GetValueOrDefault("GroupId", 0));
        var hintId = Convert.ToInt32(actionData.Param.GetValueOrDefault("HintId", 7));
        if (count <= 0 || groupId <= 0) return;

        await rogue.RollBuff(count, groupId, hintId);
    }

    private static async ValueTask ActionChangeMoney(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var amount = Convert.ToInt32(actionData.Param.GetValueOrDefault("Count", 0));
        var showType = Convert.ToInt32(actionData.Param.GetValueOrDefault("ShowType", 0));

        switch (amount)
        {
            case 0:
                return;
            case > 0:
                await rogue.GainMoney(amount, showType);
                break;
            default:
                await rogue.CostMoney(-amount, showType);
                break;
        }
    }

    private static async ValueTask ActionRandomAction(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var percent = Convert.ToDouble(actionData.Param.GetValueOrDefault("Percent", 1.0));
        var succAction = (JArray?)actionData.Param.GetValueOrDefault("SuccAction");
        var failAction = (JArray?)actionData.Param.GetValueOrDefault("FailAction");
        if (succAction == null || failAction == null) return;
        var roll = Random.Shared.NextDouble();

        if (roll <= percent)
            await ExecuteActions(rogue, eventInst, succAction.ToObject<List<RogueDialogueEventActionData>>()!);
        else
            await ExecuteActions(rogue, eventInst, failAction.ToObject<List<RogueDialogueEventActionData>>()!);
    }

    private static async ValueTask ActionChangeLineupData(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var hpChangeByCurHp = Convert.ToInt32(actionData.Param.GetValueOrDefault("HpChangeByCurHp", 0));
        var hpChange = Convert.ToInt32(actionData.Param.GetValueOrDefault("HpChange", 0));
        var spChange = Convert.ToInt32(actionData.Param.GetValueOrDefault("SpChange", 0));
        var mpChange = Convert.ToInt32(actionData.Param.GetValueOrDefault("MpChange", 0));

        foreach (var formalAvatar in rogue.Player.AvatarManager!.AvatarData.FormalAvatars)
        {
            if (hpChange != 0)
                formalAvatar.ExtraLineupHp = hpChange;

            if (hpChangeByCurHp != 0) formalAvatar.ExtraLineupHp = (int)Math.Ceiling(formalAvatar.ExtraLineupHp * ((10000 + hpChangeByCurHp) / 10000d));

            if (spChange != 0)
                formalAvatar.ExtraLineupSp = Math.Min(Math.Max(formalAvatar.ExtraLineupSp + spChange, 0), 10000);
        }

        var curLineup = rogue.Player.LineupManager!.GetCurLineup()!;
        if (mpChange != 0)
            curLineup.Mp = Math.Min(Math.Max(0, curLineup.Mp + mpChange), rogue.Player.LineupManager!.GetMaxMp());

        // sync
        await rogue.Player.SendPacket(new PacketSyncLineupNotify(rogue.Player.LineupManager!.GetCurLineup()!));
    }

    private static async ValueTask ActionRemoveBuff(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var count = Convert.ToInt32(actionData.Param.GetValueOrDefault("Count", 1));
        var groupId = Convert.ToInt32(actionData.Param.GetValueOrDefault("GroupId", 0));
        if (count <= 0) return;

        var group = GameData.RogueBuffGroupData.GetValueOrDefault(groupId)?.BuffList.Select(x => x.MazeBuffID).ToList();
        if (group == null && groupId != 0) return;

        var ownedBuffs = groupId == 0 || group == null
            ? rogue.RogueBuffs.ToList()
            : rogue.RogueBuffs.Where(x => group.Contains(x.BuffId)).ToList();

        if (ownedBuffs.Count == 0) return;
        var selectedBuffs = ownedBuffs.OrderBy(_ => Guid.NewGuid()).Take(Math.Min(count, ownedBuffs.Count)).ToList();  // avoid ownedBuffs.Count < count

        await rogue.RemoveBuffList(selectedBuffs.Select(x => x.BuffId).ToList());
    }

    private static async ValueTask ActionInvalidateOption(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var optionId = Convert.ToInt32(actionData.Param.GetValueOrDefault("OptionId", 0));
        if (optionId == 0) return;

        var option = eventInst.Options.Find(x => x.OptionId == optionId);
        if (option == null) return;

        option.IsValid = false;

        await ValueTask.CompletedTask;
    }

    private static async ValueTask ActionAddMiracle(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var miracleId = Convert.ToInt32(actionData.Param.GetValueOrDefault("MiracleId", 0));
        if (miracleId == 0) return;

        await rogue.AddMiracle(miracleId, RogueCommonActionResultSourceType.Dialogue);
    }

    private static async ValueTask ActionGetBuffInGroup(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var groupId = Convert.ToInt32(actionData.Param.GetValueOrDefault("GroupId", 0));
        if (groupId == 0) return;

        var group = GameData.RogueBuffGroupData.GetValueOrDefault(groupId)?.BuffList.ToList();
        if (group == null) return;

        var containsBuffs = rogue.RogueBuffs.Select(x => x.BuffId).ToList();
        var buffs = group.Where(x => !containsBuffs.Contains(x.MazeBuffID)).ToList();  // exclude owned buffs

        // add buffs
        await rogue.AddBuffList(buffs);
    }

    private static async ValueTask ActionTriggerBattle(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var stageId = Convert.ToInt32(actionData.Param.GetValueOrDefault("StageId", 0));
        var winActions = (JArray?)actionData.Param.GetValueOrDefault("WinActions");  // TODO
        if (!GameData.PlaneEventData.ContainsKey(stageId * 10 + rogue.Player.Data.WorldLevel)) return;

        var optionInst = eventInst.Options.Find(x => x.OptionId == eventInst.SelectedOptionId);
        if (optionInst == null) return;

        optionInst.Results.Add(new RogueEventResultInfo
        {
            BattleEventId = stageId
        });

        await ValueTask.CompletedTask;
    }

    private static async ValueTask ActionChangeMoneyRatio(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var ratio = Convert.ToDouble(actionData.Param.GetValueOrDefault("Ratio", 0.5d));
        var showType = Convert.ToInt32(actionData.Param.GetValueOrDefault("ShowType", 0));
        if (ratio <= 0) return;

        var newMoney = (int)Math.Ceiling(rogue.CurMoney * ratio);
        var updateAmount = newMoney - rogue.CurMoney;

        if (updateAmount > 0)
            await rogue.GainMoney(updateAmount, showType);
        else if (updateAmount < 0)
            await rogue.CostMoney(-updateAmount, showType);
    }

    private static async ValueTask ActionSetPrivateVariable(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var variableName = (string?)actionData.Param.GetValueOrDefault("VariableName");
        var value = actionData.Param.GetValueOrDefault("Value");
        var variableType = (string?)actionData.Param.GetValueOrDefault("VariableType");
        if (string.IsNullOrEmpty(variableName) || string.IsNullOrEmpty(variableType)) return;

        switch (variableType)
        {
            case "int":
                var val = (int?)value;
                eventInst.IntVariables[variableName] = val ?? 0;
                break;
            case "double":
                var valD = (double?)value;
                eventInst.DoubleVariables[variableName] = valD ?? 0;
                break;
        }

        await ValueTask.CompletedTask;
    }

    private static async ValueTask ActionRandomActionByVariable(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var percentName = (string?)actionData.Param.GetValueOrDefault("PercentName");
        var succAction = (JArray?)actionData.Param.GetValueOrDefault("SuccAction");
        var failAction = (JArray?)actionData.Param.GetValueOrDefault("FailAction");
        
        if (string.IsNullOrEmpty(percentName) || succAction == null || failAction == null) return;
        if (!eventInst.DoubleVariables.TryGetValue(percentName, out var percent)) return;

        var roll = Random.Shared.NextDouble();
        if (roll <= percent)
            await ExecuteActions(rogue, eventInst, succAction.ToObject<List<RogueDialogueEventActionData>>()!);
        else
            await ExecuteActions(rogue, eventInst, failAction.ToObject<List<RogueDialogueEventActionData>>()!);
    }

    private static async ValueTask ActionChangePrivateVariable(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var variableName = (string?)actionData.Param.GetValueOrDefault("VariableName");
        var value = actionData.Param.GetValueOrDefault("Value");
        var variableType = (string?)actionData.Param.GetValueOrDefault("VariableType");
        var maxValue = actionData.Param.GetValueOrDefault("MaxValue");

        if (string.IsNullOrEmpty(variableName) || string.IsNullOrEmpty(variableType)) return;

        switch (variableType)
        {
            case "int":
                var maxInt = maxValue == null ? int.MaxValue : Convert.ToInt32(maxValue);
                var valI = (int?)value ?? 0;

                if (eventInst.IntVariables.TryGetValue(variableName, out var intVar))
                    eventInst.IntVariables[variableName] = Math.Min(intVar + valI, maxInt);
                else
                    eventInst.IntVariables[variableName] = Math.Min(valI, maxInt);
                break;
            case "double":
                var maxDouble = maxValue == null ? double.MaxValue : Convert.ToDouble(maxValue);
                var valD = (double?)value ?? 0;

                if (eventInst.DoubleVariables.TryGetValue(variableName, out var doubleVar))
                    eventInst.DoubleVariables[variableName] = Math.Min(doubleVar + valD, maxDouble);
                else
                    eventInst.DoubleVariables[variableName] = Math.Min(valD, maxDouble);

                break;
        }

        await ValueTask.CompletedTask;
    }

    private static async ValueTask ActionGetBuffByRandomCount(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var minCount = Convert.ToInt32(actionData.Param.GetValueOrDefault("MinCount", 1));
        var maxCount = Convert.ToInt32(actionData.Param.GetValueOrDefault("MaxCount", 1));
        var groupId = Convert.ToInt32(actionData.Param.GetValueOrDefault("GroupId", 0));
        if (minCount <= 0 || maxCount < minCount) return;
        var count = Random.Shared.Next(minCount, maxCount + 1);

        var group = GameData.RogueBuffGroupData.GetValueOrDefault(groupId)?.BuffList.ToList();
        if (group == null) return;

        var owned = rogue.RogueBuffs.Select(x => x.BuffId).ToList();
        var notOwned = group.Where(x => !owned.Contains(x.MazeBuffID)).ToList();
        if (notOwned.Count == 0) return;

        await rogue.AddBuffList(notOwned.OrderBy(_ => Guid.NewGuid()).Take(Math.Min(count, notOwned.Count)).ToList());
    }

    private static async ValueTask ActionGetBuff(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var count = Convert.ToInt32(actionData.Param.GetValueOrDefault("Count", 1));
        var groupId = Convert.ToInt32(actionData.Param.GetValueOrDefault("GroupId", 0));
        if (count <= 0) return;

        var group = GameData.RogueBuffGroupData.GetValueOrDefault(groupId)?.BuffList.ToList();
        if (group == null) return;

        var owned = rogue.RogueBuffs.Select(x => x.BuffId).ToList();
        var notOwned = group.Where(x => !owned.Contains(x.MazeBuffID)).ToList();
        if (notOwned.Count == 0) return;

        await rogue.AddBuffList(notOwned.OrderBy(_ => Guid.NewGuid()).Take(Math.Min(count, notOwned.Count)).ToList());
    }

    private static async ValueTask ActionGetMiracleByCategory(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var categories = (JArray?)actionData.Param.GetValueOrDefault("Categories");
        var count = Convert.ToInt32(actionData.Param.GetValueOrDefault("Count", 1));
        if (categories == null || categories.Count == 0 || count == 0) return;
        List<RogueTournMiracleCategoryEnum> categoryEnums = [];
        foreach (var cat in categories)
        {
            if (Enum.TryParse(cat.ToString(), out RogueTournMiracleCategoryEnum categoryEnum))
                categoryEnums.Add(categoryEnum);
        }

        if (categoryEnums.Count == 0) return;
        var possibleMiracles = GameData.RogueTournMiracleData.Values
            .Where(x => categoryEnums.Contains(x.MiracleCategory) && !rogue.RogueMiracles.ContainsKey(x.MiracleID) &&
                        x.TournMode == RogueTournModeEnum.Tourn2)
            .ToList();

        if (possibleMiracles.Count == 0) return;
        var selectedMiracles = possibleMiracles.OrderBy(_ => Guid.NewGuid()).Take(Math.Min(count, possibleMiracles.Count)).ToList();  // avoid possibleMiracles.Count < count

        await rogue.AddMiracleList(selectedMiracles.Select(x => x.MiracleID).ToList(), RogueCommonActionResultSourceType.Dialogue);
    }

    private static async ValueTask ActionDropFormula(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        // TODO
        await ValueTask.CompletedTask;
    }

    private static async ValueTask ActionAddFormula(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var count = Convert.ToInt32(actionData.Param.GetValueOrDefault("Count", 1));
        var categories = (JArray?)actionData.Param.GetValueOrDefault("Categories");
        if (count <= 0 || categories == null) return;

        List<RogueFormulaCategoryEnum> categoryList = [];
        foreach (var category in categories)
        {
            if (Enum.TryParse(category.ToString(), out RogueFormulaCategoryEnum catEnum))
                categoryList.Add(catEnum);
        }

        if (rogue is RogueTournInstance inst)
            await inst.RollFormula(count, categoryList);
    }

    private static async ValueTask ActionGetBuffInFormula(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var count = Convert.ToInt32(actionData.Param.GetValueOrDefault("Count", 1));
        if (count <= 0) return;

        if (rogue is not RogueTournInstance inst) return;

        Dictionary<int, int> typeNumDict = [];
        foreach (var excel in inst.RogueFormulas)
        {
            typeNumDict.TryAdd(excel.MainBuffTypeID, 0);
            typeNumDict.TryAdd(excel.SubBuffTypeID, 0);

            typeNumDict[excel.MainBuffTypeID] = Math.Max(typeNumDict[excel.MainBuffTypeID], excel.MainBuffNum);
            typeNumDict[excel.SubBuffTypeID] = Math.Max(typeNumDict[excel.SubBuffTypeID], excel.SubBuffNum);
        }

        List<int> availableTypes = [];
        foreach (var (typeId, num) in typeNumDict)
        {
            var ownedNum = inst.RogueBuffs.Count(x => x.BuffExcel.RogueBuffType == typeId);
            if (ownedNum < num)
                availableTypes.Add(typeId);
        }

        if (availableTypes.Count == 0) return;

        var owned = rogue.RogueBuffs.Select(x => x.BuffId).ToList();
        var notOwned = GameData.RogueBuffData.Values.Where(x => x is RogueTournBuffExcel)
            .Where(x => !owned.Contains(x.MazeBuffID) && availableTypes.Contains(x.RogueBuffType) && x.MazeBuffLevel == 1).ToList();
        if (notOwned.Count == 0) return;

        await rogue.AddBuffList(notOwned.OrderBy(_ => Guid.NewGuid()).Take(Math.Min(count, notOwned.Count)).ToList());  // avoid notOwned.Count < count
    }

    private static async ValueTask ActionEnhanceAllBuff(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        if (rogue.RogueBuffs.Count == 0) return;

        await rogue.EnhanceBuffs(rogue.RogueBuffs.Where(x => x.BuffLevel == 1).Select(x => x.BuffId).ToList());
    }

    private static async ValueTask ActionGetBuffInFormulaUntilExpandAll(BaseRogueInstance rogue,
        RogueEventInstance eventInst, RogueDialogueEventActionData actionData)
    {
        if (rogue is not RogueTournInstance inst) return;

        Dictionary<int, int> typeNumDict = [];
        foreach (var excel in inst.RogueFormulas)
        {
            typeNumDict.TryAdd(excel.MainBuffTypeID, 0);
            typeNumDict.TryAdd(excel.SubBuffTypeID, 0);

            typeNumDict[excel.MainBuffTypeID] = Math.Max(typeNumDict[excel.MainBuffTypeID], excel.MainBuffNum);
            typeNumDict[excel.SubBuffTypeID] = Math.Max(typeNumDict[excel.SubBuffTypeID], excel.SubBuffNum);
        }

        List<int> availableTypes = [];
        foreach (var (typeId, num) in typeNumDict)
        {
            var ownedNum = inst.RogueBuffs.Count(x => x.BuffExcel.RogueBuffType == typeId);
            if (ownedNum < num)
                availableTypes.Add(typeId);
        }

        if (availableTypes.Count == 0) return;

        var owned = rogue.RogueBuffs.Select(x => x.BuffId).ToList();
        var notOwned = GameData.RogueBuffData.Values.Where(x => x is RogueTournBuffExcel)
            .Where(x => !owned.Contains(x.MazeBuffID) && availableTypes.Contains(x.RogueBuffType) && x.MazeBuffLevel == 1).ToList();
        if (notOwned.Count == 0) return;

        while (inst.ExpandedFormulaIdList.Count < inst.RogueFormulas.Count)
        {
            var add = notOwned.RandomElement();
            await inst.AddBuff(add.MazeBuffID);

            notOwned.Remove(add);
            if (notOwned.Count == 0) break;
        }
    }

    private static async ValueTask ActionAddFormulaAndExpand(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var count = Convert.ToInt32(actionData.Param.GetValueOrDefault("Count", 1));
        var categories = (JArray?)actionData.Param.GetValueOrDefault("Categories");
        if (count <= 0 || categories == null) return;

        List<RogueFormulaCategoryEnum> categoryList = [];
        foreach (var category in categories)
        {
            if (Enum.TryParse(category.ToString(), out RogueFormulaCategoryEnum catEnum))
                categoryList.Add(catEnum);
        }

        if (rogue is RogueTournInstance inst)
            await inst.RollFormula(count, categoryList);

        // TODO expand it
    }

    private static async ValueTask ActionDropMiracle(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        await ValueTask.CompletedTask;
    }

    private static async ValueTask ActionDropBuff(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        // TODO select buff to remove
        await ValueTask.CompletedTask;
    }

    private static async ValueTask ActionAddDialogueEvent(BaseRogueInstance rogue, RogueEventInstance eventInst,
        RogueDialogueEventActionData actionData)
    {
        var eventId = Convert.ToInt32(actionData.Param.GetValueOrDefault("EventId", 0));
        if (eventId == 0) return;

        eventInst.EffectEventId.Add(eventId);
        await ValueTask.CompletedTask;
    }
}