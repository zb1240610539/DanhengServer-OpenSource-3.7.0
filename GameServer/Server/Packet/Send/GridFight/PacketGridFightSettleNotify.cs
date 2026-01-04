using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.GridFight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightSettleNotify : BasePacket
{
    public PacketGridFightSettleNotify(GridFightInstance inst) : base(CmdIds.GridFightSettleNotify)
    {
        var divisionId = GameData.GridFightDivisionInfoData.Where(x => x.Value.SeasonID == GridFightManager.CurSeasonId)
            .Select(x => x.Key).Max();

        var proto = new GridFightSettleNotify
        {
            CurDivisionId = divisionId,
            PrevDivisionId = divisionId,
            TournFinishInfo = inst.ToFinishInfo()
        };

        SetData(proto);
    }
}