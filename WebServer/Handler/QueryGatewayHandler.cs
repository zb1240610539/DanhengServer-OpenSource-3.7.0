using System.Text.RegularExpressions;
using EggLink.DanhengServer.Configuration;
using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Enums;
using EggLink.DanhengServer.Internationalization;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;
using EggLink.DanhengServer.WebServer.Request;
using Google.Protobuf;

namespace EggLink.DanhengServer.WebServer.Handler;

internal partial class QueryGatewayHandler
{
    public static Logger Logger = new("GatewayServer");
    public string Data;

    public QueryGatewayHandler(GateWayRequest req)
    {
        var config = ConfigManager.Config;

        // build gateway proto
        var gateServer = new GateServer
        {
            RegionName = config.GameServer.GameServerId,
            Ip = config.GameServer.PublicAddress,
            Port = config.GameServer.Port,
            Msg = I18NManager.Translate("Server.Web.Maintain"),
            EnableVersionUpdate = true,
            EnableUploadBattleLog = true,
            EnableDesignDataVersionUpdate = true,
            EnableWatermark = true,
            EnableAndroidMiddlePackage = true,
            NetworkDiagnostic = true,
            CloseRedeemCode = true,
            UseNewNetworking = true
        };
        if (ConfigManager.Config.GameServer.UsePacketEncryption)
            gateServer.ClientSecretKey = Convert.ToBase64String(Crypto.ClientSecretKey!.GetBytes());

        // Auto separate CN/OS prefix
        var region = ConfigManager.Hotfix.Region;
        if (region == BaseRegionEnum.None) _ = Enum.TryParse(req.version[..2], out region);
        var baseUrl = region switch
        {
            BaseRegionEnum.CN => BaseUrl.CN,
            BaseRegionEnum.OS => BaseUrl.OS,
            _ => BaseUrl.OS
        };

        var remoteHotfixSuccess = false;
        if (ConfigManager.Config.HttpServer.UseFetchRemoteHotfix)
        {
            remoteHotfixSuccess = FetchRemoteHotfix(req, region, gateServer).GetAwaiter().GetResult();
        }

        if (!remoteHotfixSuccess)
        {
            UseLocalHotfix(req, region, baseUrl, gateServer);
        }

        if (!ResourceManager.IsLoaded) gateServer.Retcode = 2;
        Logger.Info("Client request: query_gateway");

        Data = Convert.ToBase64String(gateServer.ToByteArray());
    }

    private async Task<bool> FetchRemoteHotfix(GateWayRequest req, BaseRegionEnum region, GateServer gateServer)
    {
        try
        {
            var gatewayUrl = GetGatewayUrlByVersion(req.version);
            // build query params
            var queryParams = new Dictionary<string, string>
            {
                ["version"] = req.version,
                ["t"] = req.t,
                ["uid"] = req.uid,
                ["language_type"] = req.language_type,
                ["platform_type"] = req.platform_type,
                ["dispatch_seed"] = req.dispatch_seed,
                ["channel_id"] = req.channel_id,
                ["sub_channel_id"] = req.sub_channel_id,
                ["is_need_url"] = req.is_need_url,
                ["game_version"] = req.game_version,
                ["account_type"] = req.account_type,
                ["account_uid"] = req.account_uid
            };

            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));
            var fullUrl = $"{gatewayUrl}?{queryString}";

            var (statusCode, response) = await HttpNetwork.SendGetRequest(fullUrl, 5);

            if (statusCode == 200 && !string.IsNullOrEmpty(response))
            {
                try
                {
                    // parse base64 response
                    var bytes = Convert.FromBase64String(response);
                    var remoteGateServer = GateServer.Parser.ParseFrom(bytes);

                    // check if remote hotfix urls are valid, if not use local configuration
                    if (!string.IsNullOrEmpty(remoteGateServer.AssetBundleUrl))
                    {
                        gateServer.AssetBundleUrl = remoteGateServer.AssetBundleUrl;
                        gateServer.ExAssetBundleUrl = remoteGateServer.ExAssetBundleUrl;
                        gateServer.ExResourceUrl = remoteGateServer.ExResourceUrl;
                        gateServer.LuaUrl = remoteGateServer.LuaUrl;
                        gateServer.IfixUrl = remoteGateServer.IfixUrl;

                        return true;
                    }
                    else
                    {
                        Logger.Warn("Remote hotfix return empty, fall back to local hotfix");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to parse remote hotfix response: {ex.Message}");
                }
            }
            else
            {
                Logger.Warn($"Remote hotfix request failed with status: {statusCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Remote hotfix fetch failed: {ex.Message}");
        }

        return false;
    }

    private void UseLocalHotfix(GateWayRequest req, BaseRegionEnum region, string baseUrl, GateServer gateServer)
    {
        var ver = VersionRegex().Replace(req.version, "");
        ConfigManager.Hotfix.HotfixData.TryGetValue(ver, out var urls);

        if (urls != null)
        {
            if (!string.IsNullOrEmpty(urls.AssetBundleUrl))
                gateServer.AssetBundleUrl = baseUrl + urls.AssetBundleUrl;
            if (!string.IsNullOrEmpty(urls.ExAssetBundleUrl))
                gateServer.ExAssetBundleUrl = baseUrl + urls.ExAssetBundleUrl;
            if (!string.IsNullOrEmpty(urls.ExResourceUrl))
                gateServer.ExResourceUrl = baseUrl + urls.ExResourceUrl;
            if (!string.IsNullOrEmpty(urls.LuaUrl))
                gateServer.LuaUrl = baseUrl + urls.LuaUrl;
            if (!string.IsNullOrEmpty(urls.IfixUrl))
                gateServer.IfixUrl = baseUrl + urls.IfixUrl;
        }
        else
        {
            Logger.Warn($"No local hotfix found for version: {ver}");
        }
    }

    private string GetGatewayUrlByVersion(string version)
    {
        if (version.Contains("CNPROD", StringComparison.OrdinalIgnoreCase))
        {
            return GateWayBaseUrl.CNPROD;
        }
        else if (version.Contains("CNBETA", StringComparison.OrdinalIgnoreCase))
        {
            return GateWayBaseUrl.CNBETA;
        }
        else if (version.Contains("OSPROD", StringComparison.OrdinalIgnoreCase))
        {
            return GateWayBaseUrl.OSPROD;
        }
        else if (version.Contains("OSBETA", StringComparison.OrdinalIgnoreCase))
        {
            return GateWayBaseUrl.OSBETA;
        }
        else
        {
            // default fallback based on region prefix
            var region = version[..2];
            if (region.Equals("CN", StringComparison.OrdinalIgnoreCase))
            {
                return GateWayBaseUrl.CNPROD;
            }
            else
            {
                return GateWayBaseUrl.OSPROD;
            }
        }
    }

    [GeneratedRegex(@"BETA|PROD|CECREATION|Android|Win|iOS")]
    private static partial Regex VersionRegex();
}