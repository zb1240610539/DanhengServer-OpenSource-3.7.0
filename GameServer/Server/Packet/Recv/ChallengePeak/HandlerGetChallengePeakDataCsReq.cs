using EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;
using EggLink.DanhengServer.Kcp;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.ChallengePeak;

[Opcode(CmdIds.GetChallengePeakDataCsReq)]
public class HandlerGetChallengePeakDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        await connection.SendPacket(new PacketGetChallengePeakDataScRsp(connection.Player!));
    }
}