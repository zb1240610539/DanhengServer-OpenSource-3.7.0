using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Avatar;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;
using EggLink.DanhengServer.Database.Player;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetAssistListCsReq)]
public class HandlerGetAssistListCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        if (connection?.Player?.Data == null) return;

        var rsp = new GetAssistListScRsp();

        // 1. 从数据库中查询设置了助战角色的玩家 (SqlSugar 语法)
        // 我们随机取 20 条记录，避免一次性加载全服数据导致卡顿
        var assistDataList = DatabaseHelper.sqlSugarScope?
            .Queryable<AvatarData>()
            .Where(it => it.AssistAvatars != null && it.AssistAvatars.Count > 0)
            .ToPageList(1, 20); 

        if (assistDataList == null) return;

        foreach (var avatarData in assistDataList)
        {
            // 跳过自己
            if (avatarData.Uid == connection.Player.Uid) continue;

            // 获取该玩家的基础信息（为了拿到名字）
            var ownerData = PlayerData.GetPlayerByUid(avatarData.Uid);
            if (ownerData == null) continue;

            foreach (var avatarId in avatarData.AssistAvatars)
            {
                // 从该玩家的正式角色列表中找到匹配的 ID
                var avatarInfo = avatarData.FormalAvatars.FirstOrDefault(a => a.AvatarId == avatarId);
                if (avatarInfo == null) continue;

                // 2. 构造助战信息并应用【好友助战修正】
                var info = new AssistSimpleInfo
                {
                    AvatarId = (uint)avatarInfo.AvatarId,
                    Pos = 1,
                    DressedSkinId = (uint)avatarInfo.GetCurPathInfo().Skin,
                    
                    // 等级修正：离线玩家的角色等级同样不能超过借用者 + 10
                    Level = (uint)Math.Min(avatarInfo.Level, connection.Player.Data.Level + 10)
                };

                rsp.AssistSimpleInfoList.Add(info);
            }
        }

        // 3. 发送数据
        await connection.SendPacket(new PacketGetAssistListScRsp(rsp));
    }
}
