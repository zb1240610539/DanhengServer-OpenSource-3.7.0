using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightHandlePendingActionCsReq)]
public class HandlerGridFightHandlePendingActionCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightHandlePendingActionCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst != null)
            await inst.HandleResultRequest(req);

        await connection.SendPacket(new PacketGridFightHandlePendingActionScRsp(req.QueuePosition));
    }
}