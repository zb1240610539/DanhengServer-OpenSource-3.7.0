using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.ChallengePeak;

public class PacketGetChallengePeakDataScRsp : BasePacket
{
    public PacketGetChallengePeakDataScRsp(PlayerInstance player) : base(CmdIds.GetChallengePeakDataScRsp)
    {
        var proto = new GetChallengePeakDataScRsp
        {
            CurPeakGroupId = 1
        };

        foreach (var groupId in GameData.ChallengePeakGroupConfigData.Keys)
            proto.ChallengePeakLevelList.Add(player.ChallengePeakManager!.GetChallengePeakInfo(groupId));

        SetData(proto);
    }
}