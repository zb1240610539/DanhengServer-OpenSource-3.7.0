using EggLink.DanhengServer.Enums.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Data.Custom;

public class CustomPacketQueueConfig
{
    public List<PacketActionData> Queue { get; set; } = [];
}

public class PacketActionData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public PacketActionTypeEnum Action { get; set; }
    public PacketActionParamData Param { get; set; } = new();
}

public class PacketActionParamData
{
    public string PacketName { get; set; } = "";
    public string PacketData { get; set; } = "";
    public bool InterruptFormalHandler { get; set; } = false;
}