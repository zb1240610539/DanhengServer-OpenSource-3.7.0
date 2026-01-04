using EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;
using EggLink.DanhengServer.Kcp;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.ChallengePeak;

[Opcode(CmdIds.GetCurChallengePeakCsReq)]
public class HandlerGetCurChallengePeakCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetCurChallengePeakScRsp(connection.Player!));
    }
}