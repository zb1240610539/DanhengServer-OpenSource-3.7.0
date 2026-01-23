using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database.Activity;
using EggLink.DanhengServer.Database.Inventory;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.PlayerSync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Scene;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Database;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Activity;

[Opcode(CmdIds.TakeTrialActivityRewardCsReq)]
public class HandlerTakeTrialActivityRewardCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = TakeTrialActivityRewardCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;

        // 1. 获取试用关卡和奖励配置
        if (!GameData.AvatarDemoConfigData.TryGetValue((int)req.StageId, out var stage)) return;
        if (!GameData.RewardDataData.TryGetValue(stage.RewardID, out var reward)) return;

        // 2. 构造奖励列表
        var rewardItems = reward.GetItems().Select(x => new ItemData { ItemId = x.Item1, Count = x.Item2 }).ToList();
        
        // 如果有星琼奖励，加入列表。AddItems 内部会自动处理 Player.Data.Hcoin 的增加
        if (reward.Hcoin > 0)
        {
            rewardItems.Add(new ItemData { ItemId = 1, Count = (int)reward.Hcoin });
        }

        // 3. 批量添加物品
        // AddItems 内部会处理数据逻辑并统一发送物品同步包
        var itemList = await player.InventoryManager!.AddItems(rewardItems, notify: true);

        // 4. 更新试用活动进度
        var trialData = player.ActivityManager!.Data.TrialActivityData;
        var activityIndex = trialData.Activities.FindIndex(x => x.StageId == req.StageId);
        if (activityIndex != -1)
        {
            trialData.Activities[activityIndex] = new TrialActivityResultData
            {
                StageId = (int)req.StageId,
                TakenReward = true
            };
        }

       

        // 6. 核心同步：刷新顶栏 UI
        // 因为 AddItems 内部对虚拟物品使用了静默模式，所以这里必须手动补发一个全量玩家数据包
        // 这样客户端左上角的星琼余额才会立刻刷新
        await player.SendPacket(new PacketPlayerSyncScNotify(player.ToProto()));

        // 7. 发送响应包，结束客户端的转圈等待
        await connection.SendPacket(new PacketTakeTrialActivityRewardScRsp(req.StageId, rewardItems));

        // 8. 全量同步活动状态，防止逻辑死锁
        await player.ActivityManager.SyncTrialActivity();
    }
}
