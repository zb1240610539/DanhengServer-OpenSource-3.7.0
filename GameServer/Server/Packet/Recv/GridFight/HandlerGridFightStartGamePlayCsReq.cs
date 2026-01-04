using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightStartGamePlayCsReq)]
public class HandlerGridFightStartGamePlayCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightStartGamePlayCsReq.Parser.ParseFrom(data);

        var res = await connection.Player!.GridFightManager!.StartGamePlay(req.Season, req.DivisionId, req.IsOverlock);

        await connection.SendPacket(new PacketGridFightStartGamePlayScRsp(res.code, res.inst));
    }
}