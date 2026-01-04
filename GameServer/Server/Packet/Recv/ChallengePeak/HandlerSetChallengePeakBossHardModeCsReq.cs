using EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.ChallengePeak;

[Opcode(CmdIds.SetChallengePeakBossHardModeCsReq)]
public class HandlerSetChallengePeakBossHardModeCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SetChallengePeakBossHardModeCsReq.Parser.ParseFrom(data);

        connection.Player!.ChallengePeakManager!.BossIsHard = req.IsHard;

        await connection.SendPacket(new PacketSetChallengePeakBossHardModeScRsp(req.PeakGroupId, req.IsHard));
    }
}