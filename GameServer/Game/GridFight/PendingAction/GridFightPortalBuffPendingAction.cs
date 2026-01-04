using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.PendingAction;

public class GridFightPortalBuffPendingAction(GridFightInstance inst) : BaseGridFightPendingAction(inst)
{
    public uint MaxRerollCount { get; set; } = 2;
    public uint CurRerollCount { get; set; }

    public async ValueTask RerollBuff()
    {
        if (MaxRerollCount <= CurRerollCount )
            return;

        CurRerollCount++;
        PortalBuffList = GameData.GridFightPortalBuffData.Keys.OrderBy(_ => Guid.NewGuid()).Take(3).ToList();

        // sync
        await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
            new GridFightPendingActionSyncData(GridFightSrc.KGridFightSrcNone, this)));
    }

    public List<uint> PortalBuffList { get; set; } = GameData.GridFightPortalBuffData.Keys.OrderBy(_ => Guid.NewGuid()).Take(3).ToList();

    public override GridFightPendingAction ToProto()
    {
        return new GridFightPendingAction
        {
            PortalBuffAction = new GridFightPortalBuffActionInfo
            {
                GridFightPortalBuffList = { PortalBuffList },
                MaxRerollCount = MaxRerollCount,
                CurRollCount = CurRerollCount
            },
            QueuePosition = QueuePosition
        };
    }
}