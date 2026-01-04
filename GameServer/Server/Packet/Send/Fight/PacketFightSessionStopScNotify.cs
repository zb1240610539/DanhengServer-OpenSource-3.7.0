using EggLink.DanhengServer.GameServer.Game.MultiPlayer;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Fight;

public class PacketFightSessionStopScNotify : BasePacket
{
    public PacketFightSessionStopScNotify(BaseMultiPlayerGameRoomInstance room) : base(CmdIds.FightSessionStopScNotify)
    {
        var proto = new FightSessionStopScNotify
        {
            SessionInfo = new FightSessionInfo
            {
                SessionGameMode = room.GameMode,
                SessionRoomId = (ulong)room.RoomId
            }
        };

        SetData(proto);
    }
}