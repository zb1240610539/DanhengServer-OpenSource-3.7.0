using EggLink.DanhengServer.Database;
using EggLink.DanhengServer.Database.Friend;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Friend;

[Opcode(CmdIds.GetFriendDevelopmentInfoCsReq)]
public class HandlerGetFriendDevelopmentInfoCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetFriendDevelopmentInfoCsReq.Parser.ParseFrom(data);
        var uid = req.Uid;

        // get data
        var recordData = DatabaseHelper.Instance!.GetInstance<FriendRecordData>((int)uid);
        if (recordData == null)
        {
            await connection.SendPacket(new PacketGetFriendDevelopmentInfoScRsp(Retcode.RetFriendPlayerNotFound));
            return;
        }

        await connection.SendPacket(new PacketGetFriendDevelopmentInfoScRsp(recordData));
    }
}