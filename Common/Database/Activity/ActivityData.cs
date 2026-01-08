using EggLink.DanhengServer.Proto;
using SqlSugar;

namespace EggLink.DanhengServer.Database.Activity;

[SugarTable("Activity")]
public class ActivityData : BaseDatabaseDataHelper
{
    [SugarColumn(IsJson = true)] public TrialActivityData TrialActivityData { get; set; } = new();
    // 必须添加这一行，否则 PacketGetLoginActivityScRsp 会报错
    [SugarColumn(IsJson = true)] public LoginActivityData LoginActivityData { get; set; } = new();
}
public class LoginActivityData
{
    public Dictionary<uint, List<uint>> TakenRewards { get; set; } = new();
    public Dictionary<uint, uint> LoginDays { get; set; } = new();
    public long LastUpdateTick { get; set; }

   public List<EggLink.DanhengServer.Proto.LoginActivityData> ToProto()
{
    var protoList = new List<EggLink.DanhengServer.Proto.LoginActivityData>();

    foreach (var kv in LoginDays)
    {
        var protoData = new EggLink.DanhengServer.Proto.LoginActivityData
        {
            Id = kv.Key,
            LoginDays = kv.Value,
            // PanelId 先给 0，在 Manager 里根据配置表动态填
            PanelId = 0 
        };

        if (TakenRewards.TryGetValue(kv.Key, out var takenList))
        {
            // JLHOGGDHMHG 是 3.7.0 对应的 HasTakeDaysList 混淆名
            protoData.JLHOGGDHMHG.AddRange(takenList);
        }

        protoList.Add(protoData);
    }
    return protoList;
}
}
    

public class TrialActivityData
{
    public List<TrialActivityResultData> Activities { get; set; } = new();
    public int CurTrialStageId { get; set; } = 0;

    public List<TrialActivityInfo> ToProto()
    {
        var proto = new List<TrialActivityInfo>();

        foreach (var activity in Activities)
            proto.Add(new TrialActivityInfo
            {
                StageId = (uint)activity.StageId,
                TakenReward = activity.TakenReward
            });

        return proto;
    }
}

public class TrialActivityResultData
{
    public int StageId { get; set; } = 0;
    public bool TakenReward { get; set; } = false;
}