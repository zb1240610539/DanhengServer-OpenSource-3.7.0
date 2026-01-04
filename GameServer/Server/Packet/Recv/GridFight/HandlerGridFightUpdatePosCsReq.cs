using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUpdatePosCsReq)]
public class HandlerGridFightUpdatePosCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUpdatePosCsReq.Parser.ParseFrom(data);

        if (connection.Player!.GridFightManager!.GridFightInstance == null)
        {
            await connection.SendPacket(
                new PacketGridFightUpdatePosScRsp(Retcode.RetGridFightNotInGameplay, req.GridFightPosInfoList));
            return;
        }

        var gridFight = connection.Player.GridFightManager.GridFightInstance;
        var ret = await gridFight.GetComponent<GridFightRoleComponent>().UpdatePos(req.GridFightPosInfoList.ToList());

        await connection.SendPacket(
            new PacketGridFightUpdatePosScRsp(ret, req.GridFightPosInfoList));
    }
}