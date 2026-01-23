using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Data; // 引用 GameData 所在的命名空间
using EggLink.DanhengServer.Enums.Mission; // 引用 MissionPhaseEnum 所在的命名空间

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Raid;

public class PacketGetAllSaveRaidScRsp : BasePacket
{
    public PacketGetAllSaveRaidScRsp(PlayerInstance player) : base(CmdIds.GetAllSaveRaidScRsp)
    {
        var proto = new GetAllSaveRaidScRsp();

        // 确保 RaidManager 和数据不为空
        if (player.RaidManager?.RaidData?.RaidRecordDatas == null)
        {
            SetData(proto);
            return;
        }

        foreach (var dict in player.RaidManager.RaidData.RaidRecordDatas.Values)
        {
            foreach (var record in dict.Values)
            {
                // --- 1. 均衡等级过滤 ---
                // record.WorldLevel 是该副本记录的难度等级
                [cite_start]// player.Data.WorldLevel 是玩家当前的均衡等级 
                if (record.WorldLevel > player.Data.WorldLevel)
                    continue;

                // --- 2. 主线任务解锁过滤 ---
                [cite_start]// 从 GameData 中获取副本配置 [cite: 51]
                if (GameData.RaidConfigData.TryGetValue((int)record.RaidId, out var raidConfig))
                {
                    // 假设配置表中解锁任务字段为 UnlockMainMissionId
                    // 如果有前置任务要求，检查玩家是否已完成该主线任务
                    if (raidConfig.UnlockMainMissionId > 0)
                    {
                        var status = player.MissionManager!.GetMainMissionStatus(raidConfig.UnlockMainMissionId);
                        if (status != MissionPhaseEnum.Finish)
                            continue;
                    }
                }

                // --- 3. 记录添加 ---
                // 只要通过了等级和任务校验，就认为该副本已解锁并同步给客户端
                proto.RaidDataList.Add(new RaidData
                {
                    RaidId = (uint)record.RaidId,
                    WorldLevel = (uint)record.WorldLevel
                });
            }
        }

        SetData(proto);
    }
}
