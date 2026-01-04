using EggLink.DanhengServer.GameServer.Game.Challenge.Instances;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketChallengePeakSettleScNotify : BasePacket
{
    public PacketChallengePeakSettleScNotify(ChallengePeakInstance inst, List<uint> targetIdList) : base(
        CmdIds.ChallengePeakSettleScNotify)
    {
        var proto = new ChallengePeakSettleScNotify
        {
            PeakRoundsCount = inst.Data.Peak.RoundCnt,
            IsWin = inst.IsWin,
            PeakLevelId = inst.Data.Peak.CurrentPeakLevelId,
            PeakTargetList = { targetIdList },
            IsUltraBossWin = inst is { IsWin: true, Config.BossExcel: not null } && inst.Data.Peak.IsHard
        };

        SetData(proto);
    }
}