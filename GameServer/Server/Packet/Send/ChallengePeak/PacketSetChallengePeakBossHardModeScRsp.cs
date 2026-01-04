using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketSetChallengePeakBossHardModeScRsp : BasePacket
{
    public PacketSetChallengePeakBossHardModeScRsp(uint groupId, bool isHard) : base(
        CmdIds.SetChallengePeakBossHardModeScRsp)
    {
        var proto = new SetChallengePeakBossHardModeScRsp
        {
            IsHard = isHard,
            PeakGroupId = groupId
        };

        SetData(proto);
    }
}