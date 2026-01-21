using EggLink.DanhengServer.Database.Gacha;
using EggLink.DanhengServer.Enums;
using EggLink.DanhengServer.Proto;
using GachaInfo = EggLink.DanhengServer.Proto.GachaInfo;
using System.IO; 
using System.Text.Json; 
using System.Linq; 

namespace EggLink.DanhengServer.Data.Custom;

public class BannersConfig
{
    public List<BannerConfig> Banners { get; set; } = [];
}

public class BannerConfig
{
    // --- 性能优化：静态缓存变量与锁 ---
    private static string? _cachedHost;
    private static readonly object _configLock = new();

    public int GachaId { get; set; }
    public long BeginTime { get; set; }
    public long EndTime { get; set; }
    public GachaTypeEnum GachaType { get; set; }
    public List<int> RateUpItems5 { get; set; } = [];
    public List<int> RateUpItems4 { get; set; } = [];
    public int GetRateUpItem5Chance { get; set; } = 6;
    public int MaxCount { get; set; } = 90;
    public int EventChance { get; set; } = 50;

  public int DoGacha(List<int> goldAvatars, List<int> purpleAvatars, List<int> purpleWeapons, List<int> goldWeapons,
    List<int> blueWeapons, GachaData data, int uid)
{
    var random = new Random();
    
    // --- 【核心修复 1：新手池 4001 特殊判定】 ---
    int currentMaxCount = this.MaxCount; // 默认使用配置的 90 抽
    if (this.GachaId == 4001)
    {
        // 如果数据库记录已抽满 50 次，直接拦截返回 0
        if (data.NewbieGachaCount >= 50) return 0; 
        
        // 覆盖保底阈值为 50（新手池 50 必金）
        currentMaxCount = 50; 
        // 累加新手池独立计数
        data.NewbieGachaCount += 1; 
    }

    // --- 【核心修复 2：常驻池 1001 计数】 ---
    if (this.GachaId == 1001)
    {
        // 累加 300 抽自选进度，这个字段对应 GachaCeiling 的 ceiling_num
        data.StandardCumulativeCount += 1; 
    }

    // 增加基础 5 星保底水位
    data.LastGachaFailedCount += 1;
    int item;

    // 准备池子列表 (保持不变)
    var allGoldItems = new List<int>();
    allGoldItems.AddRange(goldAvatars);
    allGoldItems.AddRange(goldWeapons);

    var allNormalItems = new List<int>();
    allNormalItems.AddRange(purpleAvatars);
    allNormalItems.AddRange(purpleWeapons);

    // --- 【核心修复 3：使用动态保底阈值】 ---
    // 这里将原来的 MaxCount 替换为 currentMaxCount
    if (data.LastGachaFailedCount >= currentMaxCount || IsRateUp())
    {
        data.LastGachaFailedCount = 0; // 触发金光，重置 5 星水位
        
        if (GachaType == GachaTypeEnum.WeaponUp)
        {
            item = GetRateUpItem5(goldWeapons, data.LastWeaponGachaFailed);
            data.LastWeaponGachaFailed = !RateUpItems5.Contains(item);
        }
        else if (GachaType == GachaTypeEnum.AvatarUp)
        {
            item = GetRateUpItem5(goldAvatars, data.LastAvatarGachaFailed);
            data.LastAvatarGachaFailed = !RateUpItems5.Contains(item);
        }
        else
        {
            // 常驻或新手池逻辑
            item = GetRateUpItem5(allGoldItems, false);
        }
    }
    else
    {
        // 4 星保底逻辑 (保持不变)
        if (IsRate4() || data.LastGachaPurpleFailedCount >= 10)
        {
            item = IsRateUp4() ? RateUpItems4[random.Next(0, RateUpItems4.Count)] : allNormalItems[random.Next(0, allNormalItems.Count)];
            data.LastGachaPurpleFailedCount = 0; // 触发紫光，重置 4 星水位
        }
        else
        {
            item = blueWeapons[random.Next(0, blueWeapons.Count)];
            data.LastGachaPurpleFailedCount += 1; // 增加 4 星水位
        }
    }

    // --- 本地日志记录逻辑 (保持不变) ---
    try 
    {
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"GachaLog_{uid}.txt");
        string logEntry = $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Banner: {GachaId} | ItemID: {item}\n";
        File.AppendAllText(logPath, logEntry);
        Console.WriteLine($"[Gacha] UID: {uid} | Pool: {GachaId} | Item: {item}");
    }
    catch (Exception ex) 
    {
        Console.WriteLine($"[Error] Gacha log failed: {ex.Message}");
    }

    return item;
}

    public GachaInfo ToInfo(List<int> goldAvatar, int playerUid)
    {
        // --- 静态配置缓存优化逻辑 ---
        if (_cachedHost == null)
        {
            lock (_configLock)
            {
                if (_cachedHost == null)
                {
                    _cachedHost = LoadHostFromConfig();
                }
            }
        }
        string host = _cachedHost ?? "127.0.0.1:520"; 

        var info = new GachaInfo
        {
            GachaId = (uint)GachaId,
            DetailWebview = $"http://{host}/gacha/history?id={GachaId}&uid={playerUid}",
            DropHistoryWebview = $"http://{host}/gacha/history?id={GachaId}&uid={playerUid}"
        };

        if (GachaType != GachaTypeEnum.Normal)
        {
            info.BeginTime = BeginTime;
            info.EndTime = EndTime;
        }

        if (RateUpItems4.Count > 0) info.ItemDetailList.AddRange(RateUpItems4.Select(id => (uint)id));
        if (RateUpItems5.Count > 0)
        {
            info.PrizeItemList.AddRange(RateUpItems5.Select(id => (uint)id));
            info.ItemDetailList.AddRange(RateUpItems5.Select(id => (uint)id));
        }

        if (GachaId == 1001)
        {
            info.GachaCeiling = new GachaCeiling
            {
                IsClaimed = false,
                AvatarList = { goldAvatar.Select(id => new GachaCeilingAvatar { AvatarId = (uint)id }) }
            };
        }

        return info;
    }

    // 辅助方法：仅在缓存失效时调用
    private string? LoadHostFromConfig()
    {
        try 
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");
            if (File.Exists(configPath))
            {
                string jsonContent = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(jsonContent);
                var httpServer = doc.RootElement.GetProperty("HttpServer");
                string address = httpServer.GetProperty("PublicAddress").GetString() ?? "127.0.0.1";
                int port = httpServer.GetProperty("Port").GetInt32();
                return $"{address}:{port}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Gacha] 读取 Config.json 失败: {ex.Message}");
        }
        return null;
    }

    public bool IsEvent() => new Random().Next(0, 100) < EventChance;
    public bool IsRateUp() => new Random().Next(0, 1000) < GetRateUpItem5Chance;
    public bool IsRateUp4() => new Random().Next(0, 100) < 50;
    public bool IsRate4() => new Random().Next(0, 100) < 10;
    public int GetRateUpItem5(List<int> gold, bool forceUp)
    {
        var random = new Random();
        if (IsEvent() || forceUp) return RateUpItems5[random.Next(0, RateUpItems5.Count)];
        return gold[random.Next(0, gold.Count)];
    }
}
