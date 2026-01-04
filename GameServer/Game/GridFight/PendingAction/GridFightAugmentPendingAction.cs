using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.PendingAction;

public class GridFightAugmentPendingAction : BaseGridFightPendingAction
{
    public List<GridFightAugmentSelectUnit> Augments { get; set; } = [];
    public override GridFightPendingAction ToProto()
    {
        return new GridFightPendingAction
        {
            QueuePosition = QueuePosition,
            AugmentAction = new GridFightAugmentActionInfo
            {
                PendingAugmentInfoList = { Augments.Select(x => x.ToProto()) }
            }
        };
    }

    public GridFightAugmentPendingAction(GridFightInstance inst) : base(inst)
    {
        for (var i = 0; i < 3; i++)
        {
            var augmentId = GameData.GridFightAugmentData.Keys.Where(x => Augments.All(c => c.AugmentId != x)).ToList()
                .RandomElement();

            Augments.Add(new GridFightAugmentSelectUnit
            {
                AugmentId = augmentId
            });
        }
    }

    public async ValueTask RerollAugment(uint augmentId)
    {
        var augment = Augments.FirstOrDefault(x => x.AugmentId == augmentId);
        if (augment == null) return;

        if (augment.CurRerollNum >= augment.MaxRerollNum)
            return;

        var newAugmentId = GameData.GridFightAugmentData.Keys.Where(x => Augments.All(c => c.AugmentId != x)).ToList();
        augment.AugmentId = newAugmentId.RandomElement();

        augment.CurRerollNum++;

        // sync
        await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(
            new GridFightPendingActionSyncData(GridFightSrc.KGridFightSrcNone, this)));
    }
}

public class GridFightAugmentSelectUnit
{
    public uint AugmentId { get; set; }
    public uint MaxRerollNum { get; set; } = 2;
    public uint CurRerollNum { get; set; }

    public GridFightPendingAugmentInfo ToProto()
    {
        return new GridFightPendingAugmentInfo
        {
            AugmentId = AugmentId,
            AugmentCurRerollCount = CurRerollNum,
            AugmentMaxRerollCount = MaxRerollNum
        };
    }
}