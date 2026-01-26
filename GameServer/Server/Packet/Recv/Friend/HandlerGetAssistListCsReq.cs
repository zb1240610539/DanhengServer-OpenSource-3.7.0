using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Avatar;
using EggLink.DanhengServer.Database.Player;
using EggLink.DanhengServer.Database.Inventory;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetAssistListCsReq)]
public class HandlerGetAssistListCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        if (connection?.Player?.Data == null) return;

        var rsp = new GetAssistListScRsp { Retcode = 0 };

        // 1. 获取所有有助战的离线玩家，并进行随机排序
        var assistDataList = DatabaseHelper.sqlSugarScope?
            .Queryable<AvatarData>()
            .Where(it => it.AssistAvatars != null && it.AssistAvatars.Count > 0)
            .Where(it => it.Uid != connection.Player.Uid) // 排除掉自己
            .ToOrderByRandom() // 核心：每次刷新看到的人都不一样
            .ToPageList(1, 10); // 每次取 10 个展示，保持列表清爽

        if (assistDataList != null)
        {
            foreach (var avatarData in assistDataList)
            {
                var ownerData = PlayerData.GetPlayerByUid(avatarData.Uid);
                if (ownerData == null) continue;

                // 2. 去重逻辑：从该玩家的助战列表中随机选一个，而不是全部显示
                var avatarId = avatarData.AssistAvatars.RandomElement();
                if (avatarId == 0) continue;

                var avatarInfo = avatarData.FormalAvatars.FirstOrDefault(a => a.AvatarId == (int)avatarId);
                if (avatarInfo == null) continue;

                // 3. 构造 PlayerAssistInfo
                var playerAssist = new PlayerAssistInfo
                {
                    PlayerInfo = ownerData.ToSimpleProto(FriendOnlineStatus.Offline)
                };

                // 4. 构造数据集合并填充展示协议
                var mockCollection = new PlayerDataCollection(
                    ownerData,
                    new InventoryData { Uid = ownerData.Uid },
                    new EggLink.DanhengServer.Database.Lineup.LineupInfo()
                );

                var detail = avatarInfo.ToDetailProto(1, mockCollection);

                // --- 5. 均衡等级压制 (配合 ToBattleProto 的逻辑) ---
                int myWorldLevel = connection.Player.Data.WorldLevel;

                // 查找该位面等级允许的最高晋阶配置
                if (GameData.AvatarPromotionConfigData.TryGetValue(avatarInfo.BaseAvatarId * 10 + myWorldLevel, out var config))
                {
                    if (detail.Level > (uint)config.MaxLevel)
                    {
                        detail.Level = (uint)config.MaxLevel;
                        detail.Promotion = (uint)myWorldLevel; 
                        // 同步修正晋阶，确保 UI 上显示的星星数量符合当前均衡等级
                    }
                }

                playerAssist.MDHFANLHNMA = detail;
                rsp.AssistList.Add(playerAssist);
            }
        }

        // 6. 发送响应
        await connection.SendPacket(new PacketGetAssistListScRsp(rsp));
    }
}
