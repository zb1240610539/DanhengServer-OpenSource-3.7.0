using EggLink.DanhengServer.GameServer.Game.Challenge.Definitions;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Challenge;

public class PacketChallengeSettleNotify : BasePacket
{
    public PacketChallengeSettleNotify(BaseLegacyChallengeInstance challenge) : base(CmdIds.ChallengeSettleNotify)
    {
        var proto = new ChallengeSettleNotify
        {
            ChallengeId = (uint)challenge.Config.ID,
            IsWin = challenge.IsWin,
            ChallengeScore = challenge.GetScore1(),
            ScoreTwo = challenge.GetScore2(),
            Star = challenge.GetStars(),
            Reward = new ItemList()
        };

        SetData(proto);
    }
}