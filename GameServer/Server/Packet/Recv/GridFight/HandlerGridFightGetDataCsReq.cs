using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightGetDataCsReq)]
public class HandlerGridFightGetDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightGetDataCsReq.Parser.ParseFrom(data);

        await connection.SendPacket(new PacketGridFightGetDataScRsp(connection.Player!));
    }
}