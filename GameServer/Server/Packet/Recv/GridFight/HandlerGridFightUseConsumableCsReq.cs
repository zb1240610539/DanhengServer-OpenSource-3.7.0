using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUseConsumableCsReq)]
public class HandlerGridFightUseConsumableCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUseConsumableCsReq.Parser.ParseFrom(data);

        var gridFight = connection.Player?.GridFightManager?.GridFightInstance;
        if (gridFight == null)
        {
            await connection.SendPacket(new PacketGridFightUseConsumableScRsp(Retcode.RetGridFightNotInGameplay));
            return;
        }

        var itemsComp = gridFight.GetComponent<GridFightItemsComponent>();

        var code = await itemsComp.UseConsumable(req.ItemId, req.DisplayValue);
        await connection.SendPacket(new PacketGridFightUseConsumableScRsp(code));
    }
}