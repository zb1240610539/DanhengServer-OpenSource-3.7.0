using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Rogue.Scene;

public class RogueRoomInstance
{
    public int RoomId { get; set; }
    public int SiteId { get; set; }
    public RogueRoomStatus Status { get; set; } = RogueRoomStatus.Lock;
    public List<int> NextSiteIds { get; set; }
    public RogueRoomExcel Excel { get; set; }
    public int MonsterLevel { get; set; } 
    public int MapId { get; set; }

    /// <summary>
    /// 初始化模拟宇宙房间
    /// </summary>
    public RogueRoomInstance(RogueMapExcel excel, RogueAreaConfigExcel areaConfig)
    {
        SiteId = excel.SiteID;
        NextSiteIds = excel.NextSiteIDList;
        MapId = excel.RogueMapID;
        Status = excel.IsStart ? RogueRoomStatus.Unlock : RogueRoomStatus.Lock;

        // --- 1. 获取世界索引与 BOSS 锚点 ---
        int worldIndex = (areaConfig.RogueAreaID / 10) % 10; 
        int bossRoomId = GetBossRoomIdByWorldIndex(worldIndex);

        // --- 2. 动态分组逻辑：根据 BOSS ID 截取号段前缀 ---
        // 规律：307 取 "3"，200713 取 "200"，111713 取 "111"
        string idStr = bossRoomId.ToString();
        string prefix = idStr.Length <= 3 ? idStr.Substring(0, 1) : idStr.Substring(0, 3);

        // 从 JSON 数据中筛选出属于该号段（世界）的所有房间
        var worldRoomPool = GameData.RogueRoomData.Values
            .Where(r => r.RogueRoomID.ToString().StartsWith(prefix))
            .ToList();

        bool isBossRoom = SiteId == 13 || SiteId == 111 || SiteId == 112;
        bool isEliteRoom = SiteId == 4 || SiteId == 8;

        if (isBossRoom)
        {
            // [策略 A] 锁定 BOSS 房
            RoomId = bossRoomId;
        }
        else if (isEliteRoom)
        {
            // [策略 B] 精英房：从号段池中筛选 Type 6
            var elitePool = worldRoomPool.Where(r => r.RogueRoomType == 6).OrderBy(r => r.RogueRoomID).ToList();
            if (elitePool.Count > 0)
            {
                // Site 4 拿第一个精英，Site 8 拿最后一个精英 (确保不重复)
                RoomId = (SiteId == 4) ? elitePool.First().RogueRoomID : elitePool.Last().RogueRoomID;
            }
            else { RoomId = GetDefaultRandom(SiteId); }
        }
        else
        {
            // [策略 C] 普通小怪房：从号段池中随机筛选 Type 1
            var normalPool = worldRoomPool.Where(r => r.RogueRoomType == 1).ToList();
            RoomId = normalPool.Count > 0 ? normalPool.RandomElement().RogueRoomID : GetDefaultRandom(SiteId);
        }

        // --- 3. 加载具体的房间配置数据 ---
        if (GameData.RogueRoomData.TryGetValue(RoomId, out var roomExcel))
        {
            Excel = roomExcel;
        }
        else
        {
            Excel = GameData.RogueRoomData.Values.First(x => x.RogueRoomType == 1);
            RoomId = Excel.RogueRoomID;
        }

        // --- 4. 等级计算 (保留黄金公式) ---
        int baseLevel = areaConfig.RecommendLevel != 0 ? areaConfig.RecommendLevel : areaConfig.Difficulty * 10;
        this.MonsterLevel = baseLevel + (SiteId > 1 ? SiteId / 2 : 0);
    }

    /// <summary>
    /// 核心分组数据：通过 BOSS 房 ID 决定整个世界的房间号段
    /// </summary>
    private int GetBossRoomIdByWorldIndex(int index)
    {
        return index switch
        {
            1 => 307,
            2 => 200713, // 锚点 200
            3 => 111713, // 锚点 111
            4 => 211713, // 锚点 211
            5 => 132713, // 锚点 132
            6 => 122713, // 锚点 122
            7 => 222713, // 锚点 222
            8 => 231713, // 锚点 231
            9 => 311713, // 锚点 311
            _ => 111713
        };
    }

    private int GetDefaultRandom(int siteId)
    {
        return GameData.RogueMapGenData.TryGetValue(siteId, out var genData) 
               ? genData.RandomElement() : siteId;
    }

    public RogueRoom ToProto()
    {
        return new RogueRoom
        {
            RoomId = (uint)RoomId,
            SiteId = (uint)SiteId,
            CurStatus = Status,
            IMIMGFAAGHM = (uint)Excel.RogueRoomType
        };
    }
}
