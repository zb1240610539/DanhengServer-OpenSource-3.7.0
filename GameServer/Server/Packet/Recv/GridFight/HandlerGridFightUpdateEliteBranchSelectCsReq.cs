using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUpdateEliteBranchSelectCsReq)]
public class HandlerGridFightUpdateEliteBranchSelectCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUpdateEliteBranchSelectCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightUpdateEliteBranchSelectScRsp);
            return;
        }

        var component = inst.GetComponent<GridFightLevelComponent>();

        component.CurrentSection.BranchId = req.EliteBranchId;

        // sync
        await connection.SendPacket(
            new PacketGridFightSyncUpdateResultScNotify(new GridFightEliteBranchSyncData(GridFightSrc.KGridFightSrcNone, component.CurrentSection)));

        await connection.SendPacket(CmdIds.GridFightUpdateEliteBranchSelectScRsp);
    }
}