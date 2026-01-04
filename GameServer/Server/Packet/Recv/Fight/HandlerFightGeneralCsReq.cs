using EggLink.DanhengServer.GameServer.Server.Packet.Send.Fight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Fight;

[Opcode(CmdIds.FightGeneralCsReq)]
public class HandlerFightGeneralCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = FightGeneralCsReq.Parser.ParseFrom(data);

        if (connection.MarbleRoom == null || connection.MarblePlayer == null)
        {
            await connection.SendPacket(new PacketFightGeneralScRsp(Retcode.RetFightRoomNotExist));
            return;
        }

        await connection.MarbleRoom.HandleGeneralRequest(connection.MarblePlayer, req.NetworkMsgType,
            req.FightGeneralInfo?.ToByteArray() ?? []);

        await connection.SendPacket(new PacketFightGeneralScRsp(req.NetworkMsgType));
    }
}