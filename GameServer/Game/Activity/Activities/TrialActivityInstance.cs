using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database.Activity;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.Activity.Activities;

public class TrialActivityInstance : BaseActivityInstance
{
    public TrialActivityInstance(ActivityManager manager) : base(manager)
    {
        Data = ActivityManager.Data.TrialActivityData;
    }

    public TrialActivityData Data { get; set; }

    public async ValueTask StartActivity(int stageId)
    {
        var player = ActivityManager.Player;

        await player.LineupManager!.DestroyExtraLineup(ExtraLineupType.LineupStageTrial);

        GameData.AvatarDemoConfigData.TryGetValue(stageId, out var excel);
        if (excel != null)
        {
            Data.CurTrialStageId = stageId;
            player.LineupManager.SetExtraLineup(ExtraLineupType.LineupStageTrial, excel.TrialAvatarList.ToList(), true);
            await player.EnterScene(excel.MapEntranceID, 0, true);
        }

        await player.SendPacket(new PacketStartTrialActivityScRsp((uint)stageId));
    }

    public async ValueTask EndActivity(TrialActivityStatus status = TrialActivityStatus.None)
{
    var player = ActivityManager.Player!;

    // 基础逻辑：清理阵容、切场景
    await player.LineupManager!.DestroyExtraLineup(ExtraLineupType.LineupStageTrial);
    player.LineupManager!.LineupData.CurExtraLineup = -1;
    await player.EnterScene(2000101, 0, true);

    if (status == TrialActivityStatus.Finish)
    {
        // 1. 更新内存 (对应签到的 loginData.LoginDays++)
        Data.Activities.Add(new TrialActivityResultData
        {
            StageId = Data.CurTrialStageId
        });

        // 2. 标记保存 (完全模仿签到的 Save() 逻辑)
        // 这会让 DatabaseHelper 在下一个周期把数据存进 SQL
        var saveMethod = ActivityManager.GetType().GetMethod("Save", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        saveMethod?.Invoke(ActivityManager, null);

        // 3. 发送表现通知
        await player.SendPacket(new PacketCurTrialActivityScNotify((uint)Data.CurTrialStageId, status));

        // 4. 【核心模仿点】立即同步数据包
        // 签到是随 Rsp 返回，试用由于是战斗结束，我们主动 Push 一个 Rsp 给它
        await ActivityManager.SyncTrialActivity();
        
        
    }

    Data.CurTrialStageId = 0;
}
}
