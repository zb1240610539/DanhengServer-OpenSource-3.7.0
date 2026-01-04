using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Avatar;
using EggLink.DanhengServer.Database.Challenge;
using EggLink.DanhengServer.Database.Friend;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendBattleRecordDetailCsReq)]
public class HandlerGetFriendBattleRecordDetailCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetFriendBattleRecordDetailCsReq.Parser.ParseFrom(data);
        var uid = req.Uid;

        // get data from db
        var recordData = DatabaseHelper.Instance!.GetInstance<FriendRecordData>((int)uid);
        var challengeData = DatabaseHelper.Instance!.GetInstance<ChallengeData>((int)uid);
        var avatarData = DatabaseHelper.Instance!.GetInstance<AvatarData>((int)uid);

        if (recordData == null || challengeData == null || avatarData == null)
        {
            await connection.SendPacket(new PacketGetFriendBattleRecordDetailScRsp(Retcode.RetFriendPlayerNotFound));
            return;
        }

        await connection.SendPacket(new PacketGetFriendBattleRecordDetailScRsp(recordData, challengeData, avatarData));
    }
}