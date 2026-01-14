using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Rogue.Scene;

public class RogueRoomInstance
{
    /// <summary>
    /// 初始化模拟宇宙房间 
    /// </summary>
    /// <param name="excel">来自 RogueMap.json 的点位信息</param>
    /// <param name="areaId">来自 RogueAreaConfig.json 的世界 ID (如 130, 180, 190)</param>
    public RogueRoomInstance(RogueMapExcel excel, int areaId)
    {
        SiteId = excel.SiteID;
        NextSiteIds = excel.NextSiteIDList;
        
        // 初始状态：起点设为解锁，其余锁定
        Status = excel.IsStart ? RogueRoomStatus.Unlock : RogueRoomStatus.Lock;

        // --- 核心逻辑：区分普通房与BOSS 房 ---
        
        // SiteID 13 是通用的关底点位，111/112 是分歧 BOSS 点位
        if (SiteId == 13 || SiteId == 111 || SiteId == 112)
        {
            RoomId = GetBossRoomIdByArea(areaId);
        }
        else
        {
            // 普通房间：从站点对应的池子随机抽取 RoomId
            if (GameData.RogueMapGenData.TryGetValue(SiteId, out var genData))
            {
                RoomId = genData.RandomElement();
            }
            else
            {
                // 兜底：若池子没定义，默认使用 SiteId 映射
                RoomId = SiteId;
            }
        }

        // 加载具体的房间配置数据
        if (GameData.RogueRoomData.TryGetValue(RoomId, out var roomExcel))
        {
            Excel = roomExcel;
        }
        else
        {
            // 最终兜底：若 RoomId 在 RogueRoom.json 不存在，指向一个基础战斗房
            Excel = GameData.RogueRoomData.Values.First(x => x.RogueRoomType == 1);
            RoomId = Excel.RogueRoomID;
        }
    }

    public int RoomId { get; set; }
    public int SiteId { get; set; }
    public RogueRoomStatus Status { get; set; } = RogueRoomStatus.Lock;
    public List<int> NextSiteIds { get; set; }
    public RogueRoomExcel Excel { get; set; }

    /// <summary>
    /// 解决 BOSS 固定 BUG：根据世界进度返回正确的房间 ID
    /// </summary>
    private int GetBossRoomIdByArea(int areaId)
    {
        // 提取世界进度 (例如 130 -> 3, 180 -> 8)
        int progress = areaId / 10;

        return progress switch
        {
            1 => 211121,   // 世界 1
            2 => 221131,   // 世界 2
            3 => 231713,   // 世界 3 (杰帕德)
            4 => 121713,   // 世界 4 (史瓦罗)
            5 => 131713,   // 世界 5 (卡芙卡)
            6 => 122713,   // 世界 6 (可可利亚)
            7 => 132713,   // 世界 7 (丰饶玄鹿)
            // 如果你没有 8 和 9 的专属 ID，通常官方是复用 6 和 7 的场景但替换怪物
            8 => 122713,   // 世界 8 复用场景配置
            9 => 132713,   // 世界 9 复用场景配置
            _ => 111713    // 默认
        };
    }

    /// <summary>
    /// 序列化为协议包通知客户端渲染地图
    /// </summary>
    public RogueRoom ToProto()
    {
        return new RogueRoom
        {
            RoomId = (uint)RoomId,
            SiteId = (uint)SiteId,
            CurStatus = Status,
            // 混淆字段赋值：决定地图上显示的图标类型（战斗/事件/BOSS）
            IMIMGFAAGHM = (uint)Excel.RogueRoomType 
        };
    }
}
