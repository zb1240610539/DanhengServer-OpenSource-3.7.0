using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EggLink.DanhengServer.WebServer.Controllers;

[ApiController]
[Route("gacha")]
public class GachaController : ControllerBase
{
    private static Dictionary<string, string> _itemMap = new();
    private static BannerList _bannerData = new();
    
    private static readonly string ItemJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GachaItems.json");
    private static readonly string BannerJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Banners.json");

    public class BannerConfig {
        [JsonPropertyName("gachaId")] public int GachaId { get; set; }
        [JsonPropertyName("rateUpItems5")] public List<int> RateUpItems5 { get; set; } = [];
        [JsonPropertyName("rateUpItems4")] public List<int> RateUpItems4 { get; set; } = [];
    }
    public class BannerList {
        [JsonPropertyName("Banners")] public List<BannerConfig> Banners { get; set; } = [];
    }

    private void LoadAllData() {
        if (_itemMap.Count == 0 && System.IO.File.Exists(ItemJsonPath)) {
            try {
                string json = System.IO.File.ReadAllText(ItemJsonPath);
                _itemMap = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
            } catch { _itemMap = []; }
        }
        if ((_bannerData.Banners == null || _bannerData.Banners.Count == 0) && System.IO.File.Exists(BannerJsonPath)) {
            try {
                string json = System.IO.File.ReadAllText(BannerJsonPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _bannerData = JsonSerializer.Deserialize<BannerList>(json, options) ?? new();
            } catch { _bannerData = new(); }
        }
    }

    [HttpGet("history")]
    public ContentResult GetHistory(int id, int uid, int page = 1)
    {
        LoadAllData();
        int pageSize = 10; 
        var currentBanner = _bannerData.Banners?.FirstOrDefault(b => b.GachaId == id);
        string bannerName = id switch { 1001 => "常规跃迁", 4001 => "始发跃迁", _ => (id >= 2000 && id < 3000) ? "角色活动跃迁" : "光锥活动跃迁" };

        string html = "<html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no'>" +
                      "<style>" +
                      "body{background:#f0ede4; color:#666; font-family:sans-serif; margin:0; padding:0; width:100vw; height:100vh; display:flex; flex-direction:column; overflow-x:hidden;}" +
                      ".content-box{background:#f0ede4; flex:1; width:100%; box-sizing:border-box; padding:15px; display:flex; flex-direction:column;}" +
                      ".header-tabs{display:flex; justify-content:center; margin-bottom:10px; border-bottom: 2px solid #d6ad76; flex-shrink:0;}" +
                      ".tab-item{padding: 8px 30px; color:#999; font-size:16px; cursor:pointer;}" +
                      ".tab-active{color:#333; font-weight:bold; position:relative;}" +
                      ".tab-active::after{content:''; position:absolute; bottom:-2px; left:0; width:100%; height:4px; background:#d6ad76;}" +
                      ".view-container{flex:1; display:flex; flex-direction:column; overflow-y:auto;}" +
                      ".detail-view{display:none; padding-bottom:20px;}" +
                      ".detail-card{background:#f9f7f2; border:1px solid #d3cdc1; padding:12px; margin-bottom:12px; font-size:13px; line-height:1.6;}" +
                      ".detail-h4{margin:0 0 8px 0; color:#bd955a; font-size:15px; border-bottom:1px solid #d3cdc1; padding-bottom:4px; font-weight:bold;}" +
                      "table{width:100%; border-collapse:collapse; border: 1px solid #d3cdc1; background:#f9f7f2; table-layout: fixed; flex:1;}" +
                      "th{background:#e4e0d5; color:#8e8a82; font-weight:normal; padding:10px; border: 1px solid #d3cdc1; font-size:13px;}" +
                      "td{padding: 0; height: 6.5vh; text-align:center; border: 1px solid #d3cdc1; color:#666; font-size:13px;}" +
                      ".rarity-5{color:#bd955a; font-weight:bold;}" + 
                      ".rarity-4{color:#a256ff; font-weight:bold;}" + 
                      ".pager{display:flex; justify-content:center; align-items:center; gap:30px; margin:15px 0; flex-shrink:0;}" +
                      ".pager a{text-decoration:none; color:#d6ad76; font-size:22px; font-weight:bold;}" +
                      ".pager .current-page{background:#3b82f6; color:#fff; width:28px; height:28px; display:flex; align-items:center; justify-content:center; border-radius:2px; font-size:14px;}" +
                      ".item-grid{display:grid; grid-template-columns: 1fr 1fr; gap:5px; margin-top:5px;}" +
                      "b{color:#444;}" +
                      "</style>" +
                      "<script>function showTab(t){" +
                      "document.getElementById('h-v').style.display=(t=='h'?'flex':'none');" +
                      "document.getElementById('d-v').style.display=(t=='d'?'block':'none');" +
                      "document.getElementById('tab-h').className=(t=='h'?'tab-item tab-active':'tab-item');" +
                      "document.getElementById('tab-d').className=(t=='d'?'tab-item tab-active':'tab-item');" +
                      "}</script></head><body><div class='content-box'>";

        html += "<div class='header-tabs'><div id='tab-d' class='tab-item' onclick=\"showTab('d')\">查看详情</div><div id='tab-h' class='tab-item tab-active' onclick=\"showTab('h')\">历史记录</div></div>";
        html += "<div class='view-container'>";

        // --- 1. 【详情页：对齐官方全量文案】 ---
        html += "<div id='d-v' class='detail-view'>";
        html += $"<h2 style='font-size:18px; color:#333;'>{bannerName}</h2>";
        
        html += "<div class='detail-card'><h4 class='detail-h4'>▌ 活动详情</h4>" +
                "活动期间，限定5星角色与4星角色的跃迁成功概率将大幅提升。<br>" +
                "※本跃迁属于「角色活动跃迁」，在任意「角色活动跃迁」中未获取5星角色的累计跃迁次数会一直累计，与其他跃迁的保底相互独立，互不影响。</div>";

        html += "<div class='detail-card'><h4 class='detail-h4'>▌ 跃迁说明</h4>" +
                "<b>■ 跃迁判定顺序</b><br>每次跃迁时，系统会先判定稀有度（5星、4星或3星），再从对应稀有度的库中随机抽取具体对象。<br><br>" +
                "<b>■ 5星对象</b><br>基础概率为<b>0.600%</b>，综合概率（含保底）为<b>1.600%</b>。最多<b>90</b>次跃迁必定获取5星角色。<br>" +
                "当获取5星角色时，有<b>50.000%</b>的概率为本期UP角色。若非本期UP，则下次获取的5星必为本期UP。<br><br>" +
                "<b>■ 4星对象</b><br>基础概率为<b>5.100%</b>，综合概率（含保底）为<b>13.000%</b>。最多<b>10</b>次跃迁必定获取4星或以上对象。<br>" +
                "当获取4星对象时，有<b>50.000%</b>的概率为本期UP角色。</div>";

        if (currentBanner != null) {
            html += "<div class='detail-card'><h4 class='detail-h4'>▌ 对象清单</h4>" +
                    "<b>5星对象基础概率：0.600%</b><div class='item-grid rarity-5'>";
            foreach (var bid in currentBanner.RateUpItems5) html += $"<span>{_itemMap.GetValueOrDefault(bid.ToString(), bid.ToString())} (UP)</span>";
            html += "</div><br><b>4星对象基础概率：5.100%</b><div class='item-grid rarity-4'>";
            foreach (var bid in currentBanner.RateUpItems4) html += $"<span>{_itemMap.GetValueOrDefault(bid.ToString(), bid.ToString())} (UP)</span>";
            html += "</div></div>";
        }
        html += "</div>";

        // --- 2. 【历史记录：10格高度铺满优化版】 ---
        html += "<div id='h-v' style='display:flex; flex-direction:column; height:100%;'>";
        html += $"<h2 style='font-size:18px; color:#333; margin-bottom:5px;'>{bannerName}</h2>";
        html += "<div style='font-size:12px; color:#8e8a82; margin-bottom:10px;'>历史记录查询有1小时左右延迟。以下时间基于服务器时间显示。</div>";

        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"GachaLog_{uid}.txt");
        if (System.IO.File.Exists(logPath)) {
            var allLines = System.IO.File.ReadAllLines(logPath).Reverse().ToList();
            var filteredList = allLines.Where(line => {
                var parts = line.Split('|'); if (parts.Length < 3) return false;
                int bId = int.Parse(parts[1].Replace("Banner: ", "").Trim());
                return (id == 1001 || id == 4001) ? (bId == id) : (bId != 1001 && bId != 4001);
            }).ToList();

            int maxPage = (int)Math.Ceiling((double)filteredList.Count / pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, maxPage));
            var pagedList = filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            html += "<table><tr><th style='width:20%'>对象类型</th><th style='width:35%'>对象名称</th><th style='width:20%'>跃迁类型</th><th style='width:25%'>跃迁时间</th></tr>";
            foreach (var line in pagedList) {
                var parts = line.Split('|');
                string time = parts[0].Replace("Time: ", "").Trim().Substring(5, 11); // 截取月-日 时:分
                string itemIdStr = parts[2].Replace("ItemID: ", "").Trim();
                int itemId = int.Parse(itemIdStr);
                string name = _itemMap.GetValueOrDefault(itemIdStr, $"物品{itemIdStr}");
                string type = (itemId >= 20000) ? "光锥" : "角色";
                string cssClass = (IsFiveStar(itemId) || (itemId >= 23000 && itemId <= 23100)) ? "rarity-5" : "rarity-4";
                if (itemId < 10000 && !IsFiveStar(itemId) && itemId > 0) { } else if (itemId >= 20000 && itemId <= 20022) cssClass = "";
                html += $"<tr><td>{type}</td><td class='{cssClass}'>{name}</td><td>跃迁</td><td>{time}</td></tr>";
            }
            // 补齐 10 行
            for (int i = pagedList.Count; i < pageSize; i++) html += "<tr><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td></tr>";
            html += "</table>";
            html += $"<div class='pager'><a href='?id={id}&uid={uid}&page={page-1}'>«</a><span class='current-page'>{page}</span><a href='?id={id}&uid={uid}&page={page+1}'>»</a></div>";
        } else {
            html += "<p style='text-align:center; padding-top:100px; color:#ccc;'>暂无记录</p>";
        }
        
        html += "</div></div></div></body></html>";
        return Content(html, "text/html");
    }

    private bool IsFiveStar(int id) {
        int[] f = {1003,1004,1005,1006,1101,1102,1104,1107,1203,1204,1205,1208,1209,1211,1212,1213,1302,1303,1304,1307,1308,1309,1310,1314,1315,1401,1402,1414};
        return f.Contains(id);
    }
}