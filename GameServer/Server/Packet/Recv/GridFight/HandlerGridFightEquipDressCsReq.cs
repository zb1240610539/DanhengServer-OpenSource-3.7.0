using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightEquipDressCsReq)]
public class HandlerGridFightEquipDressCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightEquipDressCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightEquipDressScRsp);
            return;
        }

        var component = inst.GetComponent<GridFightRoleComponent>();
        await component.DressRole(req.DressRoleUniqueId, req.DressEquipmentUniqueId, GridFightSrc.KGridFightSrcDressEquip, true, 1);

        await connection.SendPacket(CmdIds.GridFightEquipDressScRsp);
    }
}