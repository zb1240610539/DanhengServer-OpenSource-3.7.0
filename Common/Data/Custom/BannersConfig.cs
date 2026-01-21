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
    public int GetRateUpItem5Chance { get; set; } = 6; // 基础万分比，6 = 0.6%
    public int MaxCount { get; set; } = 90;
    public int EventChance { get; set; } = 50;

    public int DoGacha(List<int> goldAvatars, List<int> purpleAvatars, List<int> purpleWeapons, List<int> goldWeapons,
        List<int> blueWeapons, GachaData data, int uid)
    {
        var random = new Random();
        
        // --- 【核心修复 1：新手池 4001 特殊判定】 ---
        int currentMaxCount = this.MaxCount; 
        int softPityStart5 = (GachaType == GachaTypeEnum.WeaponUp) ? 62 : 72; // 软保底开始水位

        if (this.GachaId == 4001)
        {
            if (data.NewbieGachaCount >= 50) return 0; 
            currentMaxCount = 50; 
            softPityStart5 = 40; // 新手池 40 抽开始软保底
            data.NewbieGachaCount += 1; 
        }

        // --- 【核心修复 2：常驻池 1001 计数】 ---
        if (this.GachaId == 1001)
        {
            data.StandardCumulativeCount += 1; 
        }

        // 水位累加
        data.LastGachaFailedCount += 1;
        data.LastGachaPurpleFailedCount += 1;

        int item;

        // 准备池子列表
        var allGoldItems = [.. goldAvatars, .. goldWeapons];
        var allNormalItems = [.. purpleAvatars, .. purpleWeapons];

        // --- 【核心修改：计算动态 5 星概率】 ---
        double currentChance5 = GetRateUpItem5Chance / 1000.0; // 基础 0.006
        if (data.LastGachaFailedCount > softPityStart5)
        {
            // 软保底公式：基础概率 + 超过水位后的每抽 6% 提升
            currentChance5 += 0.06 * (data.LastGachaFailedCount - softPityStart5);
        }

        // --- 5 星判定 ---
        if (random.NextDouble() < currentChance5 || data.LastGachaFailedCount >= currentMaxCount)
        {
            data.LastGachaFailedCount = 0; 
            
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
                item = GetRateUpItem5(allGoldItems, false);
            }
        }
        // --- 4 星判定 (包含软保底) ---
        else
        {
            double currentChance4 = 0.051; // 基础 5.1%
            if (data.LastGachaPurpleFailedCount >= 8)
            {
                // 4 星软保底：第 8、9 抽概率激增到 51%
                currentChance4 = 0.51; 
            }

            if (random.NextDouble() < currentChance4 || data.LastGachaPurpleFailedCount >= 10)
            {
                data.LastGachaPurpleFailedCount = 0;
                // 判定是否出 UP
                bool isUp = random.Next(0, 100) < 50 && RateUpItems4.Count > 0;
                item = isUp ? RateUpItems4[random.Next(0, RateUpItems4.Count)] : allNormalItems[random.Next(0, allNormalItems.Count)];
            }
            // --- 3 星判定 ---
            else
            {
                item = blueWeapons[random.Next(0, blueWeapons.Count)];
            }
        }

        // --- 本地日志记录 ---
        try 
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"GachaLog_{uid}.txt");
            string logEntry = $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Banner: {GachaId} | ItemID: {item}\n";
            File.AppendAllText(logPath, logEntry);
        }
        catch (Exception ex) { Console.WriteLine($"[Error] Gacha log failed: {ex.Message}"); }

        return item;
    }

    public GachaInfo ToInfo(List<int> goldAvatar, int playerUid, GachaData data) 
    {
        if (_cachedHost == null)
        {
            lock (_configLock)
            {
                if (_cachedHost == null) _cachedHost = LoadHostFromConfig();
            }
        }
        string host = _cachedHost ?? "127.0.0.1:520"; 

        var info = new GachaInfo
        {
            GachaId = (uint)GachaId,
            IINCDJPOOMC = (uint)data.LastGachaFailedCount,
            GDIFAAHIFBH = (uint)data.LastGachaPurpleFailedCount,
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
                IsClaimed = data.IsStandardSelected,
                CeilingNum = (uint)data.StandardCumulativeCount, 
                AvatarList = { goldAvatar.Select(id => new GachaCeilingAvatar { AvatarId = (uint)id }) }
            };
        }

        return info;
    }

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
        catch (Exception ex) { Console.WriteLine($"[Gacha] 读取 Config.json 失败: {ex.Message}"); }
        return null;
    }

    public bool IsEvent() => new Random().Next(0, 100) < EventChance;
    public int GetRateUpItem5(List<int> gold, bool forceUp)
    {
        var random = new Random();
        if (IsEvent() || forceUp) return RateUpItems5[random.Next(0, RateUpItems5.Count)];
        return gold[random.Next(0, gold.Count)];
    }
}
