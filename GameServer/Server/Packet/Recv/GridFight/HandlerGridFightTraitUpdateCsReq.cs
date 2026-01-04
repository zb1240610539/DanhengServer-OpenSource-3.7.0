using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightTraitUpdateCsReq)]
public class HandlerGridFightTraitUpdateCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightTraitUpdateCsReq.Parser.ParseFrom(data);

        var inst = connection.Player?.GridFightManager?.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightTraitUpdateScRsp);
            return;
        }

        var traitComp = inst.GetComponent<GridFightTraitComponent>();
        var effect = traitComp.Data.Traits.FirstOrDefault(x => x.TraitId == req.TraitId)?.Effects
            .FirstOrDefault(x => x.EffectId == req.EffectId);

        if (effect == null)
        {
            await connection.SendPacket(CmdIds.GridFightTraitUpdateScRsp);
            return;
        }

        effect.CoreRoleUniqueId = req.TraitCoreRoleInfo.UniqueId;
        // sync
        await connection.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
            new GridFightTraitSyncData(GridFightSrc.KGridFightSrcTraitEffectUpdate, effect, 0, effect.TraitId,
                effect.EffectId)));

        await connection.SendPacket(CmdIds.GridFightTraitUpdateScRsp);
    }
}