using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketStartChallengePeakScRsp : BasePacket
{
    public PacketStartChallengePeakScRsp(Retcode retcode) : base(CmdIds.StartChallengePeakScRsp)
    {
        var proto = new StartChallengePeakScRsp
        {
            Retcode = (uint)retcode
        };

        SetData(proto);
    }
}