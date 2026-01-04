using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightEquipCraftCsReq)]
public class HandlerGridFightEquipCraftCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightEquipCraftCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightEquipCraftScRsp);
            return;
        }

        var component = inst.GetComponent<GridFightItemsComponent>();
        await component.CraftEquipment(req.CraftEquipId, req.CraftMaterials.ToList());

        await connection.SendPacket(CmdIds.GridFightEquipCraftScRsp);
    }
}