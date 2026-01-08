using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;
using EggLink.DanhengServer.Util;
using Google.Protobuf; // 必须引用

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendRecommendLineupCsReq)]
public class HandlerGetFriendRecommendLineupCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        uint challengeId = 0;

        // --- 原生底层解析逻辑 ---
        try 
        {
            var input = new CodedInputStream(data);
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                int fieldNumber = WireFormat.GetTagFieldNumber(tag);
                // 3.7.0 混淆规律：ChallengeId (Key) 通常在 Tag 1, 2, 13 或 15
                if (fieldNumber == 1 || fieldNumber == 2 || fieldNumber == 13 || fieldNumber == 15) 
                {
                    challengeId = input.ReadUInt32();
                    if (challengeId > 100) break; // 找到有效的关卡ID（通常大于100）就跳出
                }
                else 
                {
                    input.SkipLastField();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.GetByClassName().Error($"原生解析异常: {ex.Message}");
        }

        Logger.GetByClassName().Info($"[原生战报请求] 解析关卡ID: {challengeId}, Uid: {connection.Player?.Uid}");

        if (connection.Player?.FriendManager == null) return;

        // 调用 Manager 获取数据
        var rspData = connection.Player.FriendManager.GetGlobalRecommendLineup(challengeId);
        
        // 发送回包
        await connection.SendPacket(new PacketGetFriendRecommendLineupScRsp(rspData));
    }
}