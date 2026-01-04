using EggLink.DanhengServer.GameServer.Game.Challenge.Instances;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketGetCurChallengePeakScRsp : BasePacket
{
    public PacketGetCurChallengePeakScRsp(PlayerInstance player) : base(CmdIds.GetCurChallengePeakScRsp)
    {
        var proto = new GetCurChallengePeakScRsp();

        if (player.ChallengeManager!.ChallengeInstance is ChallengePeakInstance peak)
        {
            proto.IsFinished = true;
            proto.PeakLevelId = peak.Data.Peak.CurrentPeakLevelId;
            proto.PeakBossBuff = peak.Data.Peak.Buffs.FirstOrDefault(0u);
            proto.PeakRoundsCount = peak.Data.Peak.RoundCnt;
        }

        SetData(proto);
    }
}