using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.ChallengePeak;

[Opcode(CmdIds.SetChallengePeakMobLineupAvatarCsReq)]
public class HandlerSetChallengePeakMobLineupAvatarCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SetChallengePeakMobLineupAvatarCsReq.Parser.ParseFrom(data);

        await connection.Player!.ChallengePeakManager!.SetLineupAvatars((int)req.PeakGroupId, req.LineupList.ToList());

        await connection.SendPacket(CmdIds.SetChallengePeakMobLineupAvatarScRsp);
    }
}