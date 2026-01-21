using EggLink.DanhengServer.Database.Gacha;
using EggLink.DanhengServer.Enums;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util; // 引用全局Debug开关
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
    
    // --- 1. 精准获取当前池子的水位标记 ---
    int pityCount = 0;
    if (this.GachaId == 4001) pityCount = data.NewbiePityCount;
    else if (this.GachaId == 1001) pityCount = data.StandardPityCount; // 使用新增的保底标记
    else pityCount = data.LastGachaFailedCount; // 限定池使用此通用计数

    int currentMaxCount = this.MaxCount;
    int softPityStart5 = (GachaType == GachaTypeEnum.WeaponUp) ? 62 : 72; 

    // --- 2. 独立计数累加逻辑 ---
    if (this.GachaId == 4001)
    {
       
        currentMaxCount = 50; 
        softPityStart5 = 40; 
        data.NewbieGachaCount += 1;
    }
    else if (this.GachaId == 1001)
    {
        data.StandardCumulativeCount += 1; // 300抽自选进度永远累加
        data.StandardPityCount += 1;
		data.NewbiePityCount += 1;		// 常驻池保底水位累加
    }
    else
    {
        data.LastGachaFailedCount += 1;    // 限定池水位累加
    }

    data.LastGachaPurpleFailedCount += 1;

    int item;
    var allGoldItems = new List<int>();
    allGoldItems.AddRange(goldAvatars);
    allGoldItems.AddRange(goldWeapons);

    // --- 3. 五星判定 (基于 pityCount) ---
    double currentChance5 = GetRateUpItem5Chance / 1000.0; 
    if (pityCount > softPityStart5)
    {
        currentChance5 += 0.06 * (pityCount - softPityStart5);
    }

    if (random.NextDouble() < currentChance5 || pityCount >= currentMaxCount)
    {
        if (GlobalDebug.EnableVerboseLog)
            Console.WriteLine($"[GACHA_DEBUG] !!!金光闪烁!!! UID:{uid} Banner:{GachaId} 第 {pityCount} 抽触发");

        // --- 4. 独立重置逻辑 ---
        if (this.GachaId == 1001)
        {
            data.StandardPityCount = 0; // 仅重置常驻保底，不重置300抽进度
        }
        else if (this.GachaId != 4001)
        {
            data.NewbiePityCount = 0; // 重置限定池保底
        }
		else
        {
            data.LastGachaFailedCount = 0;
        }

        // 确定获得物品
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
    else
    {
        // --- 四星及以下判定 ---
        double currentChance4 = 0.051; 
        if (data.LastGachaPurpleFailedCount >= 9) currentChance4 = 0.51;

        if (random.NextDouble() < currentChance4 || data.LastGachaPurpleFailedCount >= 10)
        {
            data.LastGachaPurpleFailedCount = 0;
            bool isUp = random.Next(0, 100) < 50 && RateUpItems4.Count > 0;
            if (isUp) item = RateUpItems4[random.Next(0, RateUpItems4.Count)];
            else
            {
                var pool = GachaType == GachaTypeEnum.AvatarUp ? (random.Next(0, 2) == 0 ? purpleAvatars : purpleWeapons) : 
                          (GachaType == GachaTypeEnum.WeaponUp ? (random.Next(0, 10) < 3 ? purpleAvatars : purpleWeapons) : 
                          [.. purpleAvatars, .. purpleWeapons]);
                item = pool[random.Next(0, pool.Count)];
            }
        }
        else
        {
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
           
			GDIFAAHIFBH = (uint)data.NewbieGachaCount,						
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