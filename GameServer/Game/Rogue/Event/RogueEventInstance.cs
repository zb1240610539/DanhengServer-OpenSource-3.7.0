using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Custom;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.GameServer.Game.Rogue.Scene.Entity;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Rogue.Event;

public class RogueEventInstance(int eventId, RogueNpc npc, List<RogueEventOption> optionIds, int uniqueId)
{
    public RogueEventInstance(RogueDialogueEventConfig conf, RogueNpc npc, int uniqueId) : this((int)conf.NpcId, npc, [],
        uniqueId) // check in RogueInstance.cs
    {
        Config = conf;

        foreach (var option in conf.Options)
        {
            var argId = 0u;
            if (option.DynamicActions.Count > 0)
            {
                var dynamicIds = option.DynamicActions.Select(x => x.DynamicId).ToList();
                argId = dynamicIds.RandomElement();
            }

            Options.Add(new RogueEventOption(this)
            {
                OptionId = option.OptionId,
                ArgId = argId
            });
        }
    }

    public RogueDialogueEventConfig? Config { get; set; }
    public int EventId { get; set; } = eventId;
    public bool Finished { get; set; }
    public RogueNpc EventEntity { get; set; } = npc;
    public List<RogueEventOption> Options { get; set; } = optionIds;
    public int EventUniqueId { get; set; } = uniqueId;
    public int SelectedOptionId { get; set; } = 0;
    public List<int> EffectEventId { get; set; } = [];

    #region Variable

    public Dictionary<string, int> IntVariables { get; set; } = new();
    public Dictionary<string, double> DoubleVariables { get; set; } = new();

    #endregion

    public async ValueTask Finish()
    {
        Finished = true;
        await EventEntity.FinishDialogue();
    }

    public RogueCommonDialogueDataInfo ToProto()
    {
        var proto = new RogueCommonDialogueDataInfo
        {
            DialogueInfo = ToDialogueInfo(),
            EventUniqueId = (uint)EventUniqueId
        };

        foreach (var option in Options) proto.OptionList.Add(option.ToProto());

        return proto;
    }

    public RogueCommonDialogueInfo ToDialogueInfo()
    {
        var proto = new RogueCommonDialogueInfo
        {
            DialogueBasicInfo = new RogueCommonDialogueBasicInfo
            {
                TalkDialogueId = (uint)EventId
            }
        };

        return proto;
    }
}

public class RogueEventOption(RogueEventInstance eventInstance)
{
    public RogueEventInstance EventInstance { get; set; } = eventInstance;
    public uint OptionId { get; set; }
    public uint ArgId { get; set; }
    public string? BindDoubleValue { get; set; }
    public bool IsSelected { get; set; } = false;
    public bool IsValid { get; set; } = true;
    public bool? OverrideSelected { get; set; } = null;
    public List<RogueEventResultInfo> Results { get; set; } = [];

    public RogueCommonDialogueOptionInfo ToProto()
    {
        return new RogueCommonDialogueOptionInfo
        {
            ArgId = ArgId,
            IsValid = IsValid,
            OptionId = OptionId,
            DisplayValue = new RogueCommonDialogueOptionDisplayInfo
            {
                DisplayFloatValue = BindDoubleValue != null ? (float)EventInstance.DoubleVariables.GetValueOrDefault(BindDoubleValue, 0d) : 0f
            },
            OptionResultInfo = { Results.Select(x => x.ToProto()) },
            Confirm = OverrideSelected ?? IsSelected
        };
    }
}

public class RogueEventResultInfo
{
    public int BattleEventId { get; set; }

    public RogueCommonDialogueOptionResultInfo ToProto()
    {
        return new RogueCommonDialogueOptionResultInfo
        {
            BattleResultInfo = new RogueCommonDialogueOptionBattleResultInfo
            {
                BattleEventId = (uint)BattleEventId
            }
        };
    }
}