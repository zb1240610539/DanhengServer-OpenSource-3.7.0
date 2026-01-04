using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketChallengePeakGroupDataUpdateScNotify : BasePacket
{
    public PacketChallengePeakGroupDataUpdateScNotify(ChallengePeakLevelInfo info) : base(
        CmdIds.ChallengePeakGroupDataUpdateScNotify)
    {
        var proto = new ChallengePeakGroupDataUpdateScNotify
        {
            UpdatePeakData = info
        };

        SetData(proto);
    }
}