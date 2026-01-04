using EggLink.DanhengServer.Enums.Rogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Data.Custom;

public class RogueDialogueEventConfig
{
    public uint NpcId { get; set; }
    public uint Progress { get; set; }
    public int Weight { get; set; }

    [JsonProperty(ItemConverterParameters = [typeof(StringEnumConverter)])]
    public List<RogueSubModeEnum> AllowRogueType { get; set; } = [];
    public List<string> AllowRoomType { get; set; } = [];  // Event / Encounter / Reward
    
    public List<RogueDialogueEventActionData> EnterActions { get; set; } = [];
    public List<RogueDialogueEventOptionData> Options { get; set; } = [];
}

public class RogueDialogueEventOptionData
{
    public uint OptionId { get; set; }
    public RogueDialogueEventOptionBindData DisplayValueBind { get; set; } = new();
    public List<RogueDialogueEventActionData> SelectActions { get; set; } = [];
    public List<RogueDialogueEventDialogueActionData> DynamicActions { get; set; } = [];
    public List<RogueDialogueEventConditionData> ValidConditions { get; set; } = [];

}

public class RogueDialogueEventDialogueActionData
{
    public uint DynamicId { get; set; }
    public List<RogueDialogueEventActionData> SelectActions { get; set; } = [];
}

public class RogueDialogueEventOptionBindData
{
    public string FloatValue { get; set; } = "";
}

public class RogueDialogueEventActionData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public RogueEventActionTypeEnum Name { get; set; }

    public Dictionary<string, object> Param { get; set; } = [];
}

public class RogueDialogueEventConditionData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public RogueEventConditionTypeEnum Name { get; set; }

    public Dictionary<string, object> Param { get; set; } = [];
}