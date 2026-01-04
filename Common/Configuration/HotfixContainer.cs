using EggLink.DanhengServer.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EggLink.DanhengServer.Configuration;

public class HotfixContainer
{
    [JsonConverter(typeof(StringEnumConverter))]
    public BaseRegionEnum Region { get; set; } = BaseRegionEnum.None;

    public Dictionary<string, DownloadUrlConfig> HotfixData { get; set; } = [];
}

public class DownloadUrlConfig
{
    public string AssetBundleUrl { get; set; } = "";
    public string ExAssetBundleUrl { get; set; } = "";
    public string ExResourceUrl { get; set; } = "";
    public string LuaUrl { get; set; } = "";
    public string IfixUrl { get; set; } = "";
}

public static class GateWayBaseUrl
{
    public const string CNBETA = "https://beta-release01-cn.bhsr.com/query_gateway";
    public const string CNPROD = "https://prod-gf-cn-dp01.bhsr.com/query_gateway";
    public const string OSBETA = "https://beta-release01-asia.starrails.com/query_gateway";
    public const string OSPROD = "https://prod-official-asia-dp01.starrails.com/query_gateway";
}

public static class BaseUrl
{
    public const string CN = "https://autopatchcn.bhsr.com/";
    public const string OS = "https://autopatchos.starrails.com/";
}