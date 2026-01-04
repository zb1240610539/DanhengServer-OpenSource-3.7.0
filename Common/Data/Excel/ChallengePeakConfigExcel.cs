using EggLink.DanhengServer.Util;
using Newtonsoft.Json;

namespace EggLink.DanhengServer.Data.Excel;

[ResourceEntity("ChallengePeakConfig.json")]
public class ChallengePeakConfigExcel : ExcelResource
{
    public int ID { get; set; }
    public List<int> TagList { get; set; } = [];
    public List<int> HPProgressValueList { get; set; } = [];
    public List<int> ProgressValueList { get; set; } = [];
    public List<int> EventIDList { get; set; } = [];
    public List<int> NormalTargetList { get; set; } = [];

    [JsonIgnore]
    public Dictionary<int, List<ChallengeConfigExcel.ChallengeMonsterInfo>> ChallengeMonsters { get; } = [];

    [JsonIgnore] public ChallengePeakBossConfigExcel? BossExcel { get; set; }

    public override int GetId()
    {
        return ID;
    }

    public override void Loaded()
    {
        GameData.ChallengePeakConfigData.TryAdd(ID, this);
    }

    public override void AfterAllDone()
    {
        var groupId = (int)GameConstants.CHALLENGE_PEAK_TARGET_ENTRY_ID[GameConstants.CHALLENGE_PEAK_CUR_GROUP_ID][1];
        ChallengeMonsters.Add(groupId, []);

        var curConfId = 200000;
        foreach (var eventId in EventIDList)
        {
            // get from stage id
            if (!GameData.StageConfigData.TryGetValue(eventId, out var stage)) continue;

            var monsterId = stage.MonsterList.LastOrDefault()?.Monster0 ?? 0;
            if (!GameData.MonsterConfigData.TryGetValue(monsterId, out var monsterConf)) continue;
            if (!GameData.MonsterTemplateConfigData.TryGetValue(monsterConf.MonsterTemplateID, out var template)) continue;

            var npcMonsterId = template.NPCMonsterList.Take(2).LastOrDefault(0);

            ChallengeMonsters[groupId]
                .Add(new ChallengeConfigExcel.ChallengeMonsterInfo(++curConfId, npcMonsterId,
                    eventId));
        }
    }
}