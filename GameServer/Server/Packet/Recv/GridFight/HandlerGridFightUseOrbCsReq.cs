using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUseOrbCsReq)]
public class HandlerGridFightUseOrbCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUseOrbCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightUseOrbScRsp);
            return;
        }

        var component = inst.GetComponent<GridFightOrbComponent>();
        await component.UseOrb(req.TargetOrbUniqueIdList.ToList());

        await connection.SendPacket(CmdIds.GridFightUseOrbScRsp);
    }
}