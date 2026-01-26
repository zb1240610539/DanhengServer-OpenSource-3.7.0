using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Avatar;
using EggLink.DanhengServer.Database.Player;
using EggLink.DanhengServer.Database.Inventory; // 必须引用，用于构造 Collection
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetAssistListCsReq)]
public class HandlerGetAssistListCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        if (connection?.Player?.Data == null) return;

        var rsp = new GetAssistListScRsp { Retcode = 0 };

        // 1. 从数据库读取有助战设置的离线玩家 (取前20个)
        var assistDataList = DatabaseHelper.sqlSugarScope?
            .Queryable<AvatarData>()
            .Where(it => it.AssistAvatars != null && it.AssistAvatars.Count > 0)
            .ToPageList(1, 20);

        if (assistDataList != null)
        {
            foreach (var avatarData in assistDataList)
            {
                // 跳过自己
                if (avatarData.Uid == connection.Player.Uid) continue;

                // 获取助战者的基础玩家信息
                var ownerData = PlayerData.GetPlayerByUid(avatarData.Uid);
                if (ownerData == null) continue;

                foreach (var avatarId in avatarData.AssistAvatars)
                {
                    // 在 FormalAvatars 列表中查找对应的角色数据
                    var avatarInfo = avatarData.FormalAvatars.FirstOrDefault(a => a.AvatarId == avatarId);
                    if (avatarInfo == null) continue;

                    // 2. 构造嵌套的 PlayerAssistInfo
                    var playerAssist = new PlayerAssistInfo
                    {
                        PlayerInfo = ownerData.ToSimpleProto(FriendOnlineStatus.Offline)
                    };

                    // 3. 填充那个混淆字段 MDHFANLHNMA (类型为 DisplayAvatarDetailInfo)
                    // 构造一个临时的 Collection 以满足 ToDetailProto 的参数需求
                    var mockCollection = new PlayerDataCollection(
                        ownerData, 
                        new InventoryData { Uid = ownerData.Uid }, // 避免空引用
                        new EggLink.DanhengServer.Database.Lineup.LineupInfo()
                    );

                    // 调用你 AvatarData.cs 里的 ToDetailProto 方法
                    var detail = avatarInfo.ToDetailProto(1, mockCollection);
                    
                    // --- 好友助战修正：等级压制 ---
                    // 修正后的等级 = Min(对方角色原始等级, 你的等级 + 10)
                    detail.Level = (uint)Math.Min(avatarInfo.Level, connection.Player.Data.Level + 10);
                    
                    playerAssist.MDHFANLHNMA = detail;

                    // 4. 添加到响应列表
                    rsp.AssistList.Add(playerAssist);
                }
            }
        }

        // 5. 发送填充了真实数据的包
        await connection.SendPacket(new PacketGetAssistListScRsp(rsp));
    }
}
