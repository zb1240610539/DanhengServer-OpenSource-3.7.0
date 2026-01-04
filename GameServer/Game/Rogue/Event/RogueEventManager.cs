using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Enums.Rogue;
using EggLink.DanhengServer.Enums.TournRogue;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.RogueCommon;
using Newtonsoft.Json.Linq;

namespace EggLink.DanhengServer.GameServer.Game.Rogue.Event;

public class RogueEventManager(PlayerInstance player, BaseRogueInstance rogueInstance)
{
    public PlayerInstance Player { get; set; } = player;
    public BaseRogueInstance Rogue { get; set; } = rogueInstance;
    public List<RogueEventInstance> RunningEvent { get; set; } = [];

    public async ValueTask OnNextRoom()
    {
        RunningEvent.Clear(); // Clear all running events
        await Player.SendPacket(new PacketSyncRogueCommonDialogueDataScNotify([]));
    }

    public async ValueTask AddEvent(RogueEventInstance eventInstance)
    {
        RunningEvent.Add(eventInstance);

        // run enter actions
        await RogueEventActionExecutor.ExecuteActions(Rogue, eventInstance, eventInstance.Config!.EnterActions);

        // bind
        foreach (var option in eventInstance.Config!.Options)
        {
            var optionInst = eventInstance.Options.Find(x => x.OptionId == option.OptionId);
            if (optionInst == null) continue;
            // check condition
            foreach (var condition in option.ValidConditions)
            {
                switch (condition.Name)
                {
                    case RogueEventConditionTypeEnum.CondNone:
                        break;
                    case RogueEventConditionTypeEnum.CondCheckMoney:
                        if (Rogue.CurMoney < Convert.ToInt32(condition.Param["GreaterOrEqual"]))
                            optionInst.IsValid = false;
                        break;

                    case RogueEventConditionTypeEnum.CondHasAvatar:
                        if (Rogue.Player.AvatarManager!.GetFormalAvatar(Convert.ToInt32(condition.Param["AvatarId"])) ==
                            null)
                            optionInst.IsValid = false;
                        break;
                    case RogueEventConditionTypeEnum.CondLineupHasAvatar:
                        if (Rogue.Player.LineupManager!.GetCurLineup()?.BaseAvatars?.All(x =>
                                x.BaseAvatarId != Convert.ToInt32(condition.Param["AvatarId"])) == true)
                            optionInst.IsValid = false;
                        break;
                    case RogueEventConditionTypeEnum.CondHasMiracle:
                        var categories = (JArray?)condition.Param["Categories"];

                        if (categories == null || categories.Count == 0) break;
                        List<RogueTournMiracleCategoryEnum> categoryEnums = [];

                        foreach (var cat in categories)
                        {
                            if (Enum.TryParse(cat.ToString(), out RogueTournMiracleCategoryEnum categoryEnum))
                                categoryEnums.Add(categoryEnum);
                        }

                        if (Rogue.RogueMiracles.Keys.All(x =>
                                !categoryEnums.Contains(
                                    GameData.RogueTournMiracleData.GetValueOrDefault(x)?.MiracleCategory ??
                                    RogueTournMiracleCategoryEnum.None)))
                            optionInst.IsValid = false;

                        break;
                    case RogueEventConditionTypeEnum.CondHasBuff:
                        var groupId = Convert.ToInt32(condition.Param["GroupId"]);
                        var group = GameData.RogueBuffGroupData.GetValueOrDefault(groupId);
                        if (group == null) break;

                        var buffInGroup = group.BuffList.Select(x => x.MazeBuffID).ToHashSet();
                        if (Rogue.RogueBuffs.All(x => !buffInGroup.Contains(x.BuffId))) optionInst.IsValid = false;

                        break;
                }
            }

            if (string.IsNullOrEmpty(option.DisplayValueBind.FloatValue)) continue;

            var bindKey = option.DisplayValueBind.FloatValue;

            optionInst.BindDoubleValue = bindKey;
        }

        await Player.SendPacket(new PacketSyncRogueCommonDialogueDataScNotify(RunningEvent));
    }

    public void RemoveEvent(RogueEventInstance eventInstance)
    {
        RunningEvent.Remove(eventInstance);
    }

    public async ValueTask FinishEvent(RogueEventInstance eventInstance)
    {
        await eventInstance.Finish();
    }

    public async ValueTask NpcDisappear(RogueEventInstance eventInstance)
    {
        RunningEvent.Remove(eventInstance);
        await Player.SceneInstance!.RemoveEntity(eventInstance.EventEntity);
    }

    public RogueEventInstance? FindEvent(int optionId)
    {
        return RunningEvent.FirstOrDefault(eventInstance => eventInstance.Options.Exists(x => x.OptionId == optionId));
    }

    public async ValueTask TriggerEvent(RogueEventInstance? eventInstance, int eventId)
    {
        await ValueTask.CompletedTask;
    }

    public async ValueTask SelectOption(RogueEventInstance eventInstance, int optionId)
    {
        eventInstance.SelectedOptionId = optionId;
        var option = eventInstance.Options.Find(x => x.OptionId == optionId);
        if (option == null)
        {
            await Player.SendPacket(new PacketSelectRogueCommonDialogueOptionScRsp());
            return;
        }

        var optionConf = eventInstance.Config!.Options.FirstOrDefault(x => x.OptionId == option.OptionId);
        if (optionConf == null)
        {
            await Player.SendPacket(new PacketSelectRogueCommonDialogueOptionScRsp());
            return;
        }

        // run option effects
        await RogueEventActionExecutor.ExecuteActions(Rogue, eventInstance, optionConf.SelectActions);

        var dynamicAct = optionConf.DynamicActions.FirstOrDefault(x => x.DynamicId == option.ArgId);
        if (dynamicAct != null)
            await RogueEventActionExecutor.ExecuteActions(Rogue, eventInstance, dynamicAct.SelectActions);

        if (eventInstance.EffectEventId.Count > 0)
            option.OverrideSelected = false;

        // send rsp
        await Player.SendPacket(new PacketSyncRogueCommonDialogueOptionFinishScNotify(eventInstance));
        option.IsSelected = true;

        await Player.SendPacket(new PacketSelectRogueCommonDialogueOptionScRsp(eventInstance));


        eventInstance.EffectEventId.Clear();
    }
}