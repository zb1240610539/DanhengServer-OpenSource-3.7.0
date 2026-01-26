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

        // 1. 动态判断数据库类型，选择正确的随机函数
        string randomFunc = ConfigManager.Config.Database.DatabaseType == "mysql" ? "RAND()" : "RANDOM()";

        // 2. 从数据库读取有助战设置的离线玩家，并随机排序
        var assistDataList = DatabaseHelper.sqlSugarScope?
            .Queryable<AvatarData>()
            .Where(it => it.AssistAvatars != null && it.AssistAvatars.Count > 0)
            .Where(it => it.Uid != connection.Player.Uid) // 排除掉自己
            .OrderBy(randomFunc) // 使用对应的随机函数
            .ToPageList(1, 10);

        if (assistDataList != null)
        {
            foreach (var avatarData in assistDataList)
            {
                var ownerData = PlayerData.GetPlayerByUid(avatarData.Uid);
                if (ownerData == null) continue;

                // 3. 随机选一个助战角色 (修复泛型转换错误)
                var randomAvatarIdObj = avatarData.AssistAvatars.RandomElement();
                int avatarId = Convert.ToInt32(randomAvatarIdObj); 
                
                if (avatarId == 0) continue;

                var avatarInfo = avatarData.FormalAvatars.FirstOrDefault(a => a.AvatarId == avatarId);
                if (avatarInfo == null) continue;

                // 4. 构造响应信息
                var playerAssist = new PlayerAssistInfo
                {
                    PlayerInfo = ownerData.ToSimpleProto(FriendOnlineStatus.Offline)
                };

                // 构造 Mock Collection 用于详情显示
                var mockCollection = new PlayerDataCollection(
                    ownerData,
                    new InventoryData { Uid = ownerData.Uid },
                    new EggLink.DanhengServer.Database.Lineup.LineupInfo()
                );

                var detail = avatarInfo.ToDetailProto(1, mockCollection);

                // --- 5. 均衡等级压制 (保持与战斗逻辑对齐) ---
                int myWorldLevel = connection.Player.Data.WorldLevel;

                if (GameData.AvatarPromotionConfigData.TryGetValue(avatarInfo.BaseAvatarId * 10 + myWorldLevel, out var config))
                {
                    if (detail.Level > (uint)config.MaxLevel)
                    {
                        detail.Level = (uint)config.MaxLevel;
                        detail.Promotion = (uint)myWorldLevel; 
                    }
                }

                playerAssist.MDHFANLHNMA = detail;
                rsp.AssistList.Add(playerAssist);
            }
        }

        await connection.SendPacket(new PacketGetAssistListScRsp(rsp));
    }
}
