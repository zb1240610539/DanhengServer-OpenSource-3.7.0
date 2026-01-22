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
    
    // --- 1. 根据当前卡池类型，从 GachaData 中精准获取水位 ---
    int pityCount = 0;
    if (this.GachaId == 4001) pityCount = data.NewbiePityCount;
    else if (this.GachaId == 1001) pityCount = data.StandardPityCount;
    else if (GachaType == GachaTypeEnum.AvatarUp) pityCount = data.LastAvatarGachaPity; 
    else if (GachaType == GachaTypeEnum.WeaponUp) pityCount = data.LastWeaponGachaPity;

    // 设置保底参数：武器池 80 满保底，角色池 90
    int currentMaxCount = (GachaType == GachaTypeEnum.WeaponUp) ? 80 : 90;
    int softPityStart5 = (GachaType == GachaTypeEnum.WeaponUp) ? 62 : 72; 

    // --- 2. 独立累加水位 (在判定本次产出前先增加计数) ---
    if (this.GachaId == 4001) {
        data.NewbiePityCount++;
        data.NewbieGachaCount++; // 新手池总计 50 次限制
    }
    else if (this.GachaId == 1001) {
        data.StandardPityCount++;
        data.StandardCumulativeCount++; // 常驻/300抽自选进度
    }
    else if (GachaType == GachaTypeEnum.AvatarUp) {
        data.LastAvatarGachaPity++; 
    }
    else if (GachaType == GachaTypeEnum.WeaponUp) {
        data.LastWeaponGachaPity++; 
    }

    data.LastGachaPurpleFailedCount++; // 四星水位是通用的

    // --- 3. 五星判定概率计算 ---
    double currentChance5 = GetRateUpItem5Chance / 1000.0; 
    if (pityCount >= softPityStart5) {
        // 软保底概率递增：武器池每抽约+7%，角色池每抽约+6%
        currentChance5 += (GachaType == GachaTypeEnum.WeaponUp ? 0.07 : 0.06) * (pityCount - softPityStart5 + 1);
    }

    int item;
    // 判定是否出金 (随机数小于概率，或达到硬保底)
    if (random.NextDouble() < currentChance5 || pityCount + 1 >= currentMaxCount) {
        // --- 4. 【核心修复】触发五星后，精准重置对应的水位字段 ---
        if (this.GachaId == 1001) data.StandardPityCount = 0;
        else if (this.GachaId == 4001) data.NewbiePityCount = 0;
        else if (GachaType == GachaTypeEnum.AvatarUp) data.LastAvatarGachaPity = 0;
        else if (GachaType == GachaTypeEnum.WeaponUp) data.LastWeaponGachaPity = 0;

        // 确定产出并处理大保底逻辑
        if (GachaType == GachaTypeEnum.WeaponUp) {
            item = GetRateUpItem5(goldWeapons, data.LastWeaponGachaFailed);
            data.LastWeaponGachaFailed = !RateUpItems5.Contains(item); // 没抽到UP则下次必出
        }
        else if (GachaType == GachaTypeEnum.AvatarUp) {
            item = GetRateUpItem5(goldAvatars, data.LastAvatarGachaFailed);
            data.LastAvatarGachaFailed = !RateUpItems5.Contains(item); // 没抽到UP则下次必出
        }
        else {
            item = GetRateUpItem5([.. goldAvatars, .. goldWeapons], false);
        }
    }
    else {
        // --- 5. 四星及三星判定逻辑 ---
        double currentChance4 = 0.051; 
        if (data.LastGachaPurpleFailedCount >= 9) currentChance4 = 0.51;

        if (random.NextDouble() < currentChance4 || data.LastGachaPurpleFailedCount >= 10) {
            data.LastGachaPurpleFailedCount = 0;
            bool isUp = random.Next(0, 100) < 50 && RateUpItems4.Count > 0;
            if (isUp) item = RateUpItems4[random.Next(0, RateUpItems4.Count)];
            else {
                var pool = GachaType == GachaTypeEnum.AvatarUp ? (random.Next(0, 2) == 0 ? purpleAvatars : purpleWeapons) : 
                          (GachaType == GachaTypeEnum.WeaponUp ? (random.Next(0, 10) < 3 ? purpleAvatars : purpleWeapons) : 
                          [.. purpleAvatars, .. purpleWeapons]);
                item = pool[random.Next(0, pool.Count)];
            }
        }
        else {
            item = blueWeapons[random.Next(0, blueWeapons.Count)];
        }
    }
    
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