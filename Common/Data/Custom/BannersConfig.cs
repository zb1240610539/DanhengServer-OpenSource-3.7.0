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
        
        // --- 1. 初始化保底与软保底阈值 ---
        int currentMaxCount = this.MaxCount;
        int softPityStart5 = (GachaType == GachaTypeEnum.WeaponUp) ? 62 : 72; // 5星软保底：武器63，其他73

        if (this.GachaId == 4001)
        {
            if (data.NewbieGachaCount >= 50) return 0; 
            currentMaxCount = 50; 
            softPityStart5 = 40; 
            data.NewbieGachaCount += 1; 
        }

        if (this.GachaId == 1001) data.StandardCumulativeCount += 1; 

        data.LastGachaFailedCount += 1;
        data.LastGachaPurpleFailedCount += 1;

        int item;

        // 修复 CS9176：改回显式创建 List
        var allGoldItems = new List<int>();
        allGoldItems.AddRange(goldAvatars);
        allGoldItems.AddRange(goldWeapons);

        // --- 2. 五星判定逻辑 (软保底) ---
        double currentChance5 = GetRateUpItem5Chance / 1000.0; // 0.006
        if (data.LastGachaFailedCount > softPityStart5)
        {
            currentChance5 += 0.06 * (data.LastGachaFailedCount - softPityStart5);
        }

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
        // --- 3. 四星判定逻辑 (软保底 + 掉落分类) ---
        else
        {
            double currentChance4 = 0.051; // 基础 5.1%
            // 官服规律：第 9 抽软保底概率激增
            if (data.LastGachaPurpleFailedCount >= 9) currentChance4 = 0.51;

            if (random.NextDouble() < currentChance4 || data.LastGachaPurpleFailedCount >= 10)
            {
                data.LastGachaPurpleFailedCount = 0;
                
                // 判定是否出 4星 UP
                bool isUp = random.Next(0, 100) < 50 && RateUpItems4.Count > 0;

                if (isUp)
                {
                    item = RateUpItems4[random.Next(0, RateUpItems4.Count)];
                }
                else
                {
                    // 根据卡池类型决定掉落倾向 (官服区分角色/武器)
                    if (GachaType == GachaTypeEnum.AvatarUp)
                    {
                        // 角色池：非UP的4星里，角色和武器各 50%
                        var pool = random.Next(0, 2) == 0 ? purpleAvatars : purpleWeapons;
                        item = pool[random.Next(0, pool.Count)];
                    }
                    else if (GachaType == GachaTypeEnum.WeaponUp)
                    {
                        // 武器池：非UP的4星里，武器权重更高 (3:7)
                        var pool = random.Next(0, 10) < 3 ? purpleAvatars : purpleWeapons;
                        item = pool[random.Next(0, pool.Count)];
                    }
                    else
                    {
                        // 常驻/新手池：完全混合随机
                        var allNormalItems = new List<int>();
                        allNormalItems.AddRange(purpleAvatars);
                        allNormalItems.AddRange(purpleWeapons);
                        item = allNormalItems[random.Next(0, allNormalItems.Count)];
                    }
                }
            }
            else
            {
                // 三星垫抽
                item = blueWeapons[random.Next(0, blueWeapons.Count)];
            }
        }

        LogGacha(uid, item);
        return item;
    }

    private void LogGacha(int uid, int item)
    {
        try 
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"GachaLog_{uid}.txt");
            string logEntry = $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Banner: {GachaId} | ItemID: {item}\n";
            File.AppendAllText(logPath, logEntry);
        }
        catch (Exception) { /* ignored */ }
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
        catch { /* ignored */ }
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
