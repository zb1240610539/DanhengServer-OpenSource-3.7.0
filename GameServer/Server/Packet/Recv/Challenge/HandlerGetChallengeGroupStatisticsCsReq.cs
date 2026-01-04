using EggLink.DanhengServer.GameServer.Server.Packet.Send.Challenge;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Challenge;

[Opcode(CmdIds.GetChallengeGroupStatisticsCsReq)]
public class HandlerGetChallengeGroupStatisticsCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GetChallengeGroupStatisticsCsReq.Parser.ParseFrom(data);

        await connection.SendPacket(new PacketGetChallengeGroupStatisticsScRsp(req.GroupId,
            connection.Player!.FriendRecordData!.ChallengeGroupStatistics.Values.FirstOrDefault(x =>
                x.GroupId == req.GroupId)));
    }
}