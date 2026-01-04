using EggLink.DanhengServer.GameServer.Server.Packet.Send.Adventure;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Adventure;

[Opcode(CmdIds.QuickStartCocoonStageCsReq)]
public class HandlerQuickStartCocoonStageCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = QuickStartCocoonStageCsReq.Parser.ParseFrom(data);
        var battle =
            await connection.Player!.BattleManager!.StartCocoonStage((int)req.CocoonId, (int)req.CocoonChallengeTimes,
                (int)req.WorldLevel);
        connection.Player.SceneInstance?.OnEnterStage();

        if (battle != null)
            await connection.SendPacket(new PacketQuickStartCocoonStageScRsp(battle, (int)req.CocoonId, (int)req.CocoonChallengeTimes));
        else
            await connection.SendPacket(new PacketQuickStartCocoonStageScRsp());
    }
}