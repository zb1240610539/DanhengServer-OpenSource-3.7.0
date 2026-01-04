using EggLink.DanhengServer.GameServer.Game.Challenge.Instances;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Challenge;

public class PacketChallengeBossPhaseSettleNotify : BasePacket
{
    public PacketChallengeBossPhaseSettleNotify(ChallengeBossInstance challenge, BattleTargetList? targetLists = null) :
        base(CmdIds
            .ChallengeBossPhaseSettleNotify)
    {
        var proto = new ChallengeBossPhaseSettleNotify
        {
            ChallengeId = (uint)challenge.Config.ID,
            IsWin = challenge.IsWin,
            ChallengeScore = challenge.Data.Boss.ScoreStage1,
            ScoreTwo = challenge.Data.Boss.ScoreStage2,
            Star = challenge.Data.Boss.Stars,
            Phase = challenge.Data.Boss.CurrentStage,
            IsReward = true,
            ShowRemainAction = challenge.Data.Boss.CurrentStage == challenge.Config.StageNum,
            CurChallengeType = 1
        };

        proto.BattleTargetList.AddRange(targetLists?.BattleTargetList_ ?? []);

        SetData(proto);
    }
}