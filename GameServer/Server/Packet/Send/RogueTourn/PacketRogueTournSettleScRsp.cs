using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.GameServer.Game.RogueTourn;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.RogueTourn;

public class PacketRogueTournSettleScRsp : BasePacket
{
    public PacketRogueTournSettleScRsp(RogueTournInstance instance) : base(CmdIds.RogueTournSettleScRsp)
    {
        var maxDivision = GameData.RogueTournDivisionData.Values.MaxBy(x => x.DivisionLevel) ?? new RogueTournDivisionExcel();

        var proto = new RogueTournSettleScRsp
        {
            RogueTournCurSceneInfo = instance.ToCurSceneInfo(),
            TournFinishInfo = new RogueTournFinishInfo
            {
                RogueTournCurInfo = instance.ToProto(),
                RogueLineupInfo = instance.Player.LineupManager!.GetCurLineup()!.ToProto(),
                CJCOJAMLEEL = new(),
                NewDivisionInfo = new RogueTournDivisionInfo
                {
                    DivisionLevel = (uint)maxDivision.DivisionLevel,
                    DivisionProgress = (uint)maxDivision.DivisionProgress
                },
                GCGLNKFDKKN = new(),
                KGCIAIAFIBE = new(),
                PFOEPFPHFNJ = new()
            }
        };

        SetData(proto);
    }

    public PacketRogueTournSettleScRsp() : base(CmdIds.RogueTournSettleScRsp)
    {
        var proto = new RogueTournSettleScRsp
        {
            Retcode = 1
        };

        SetData(proto);
    }
}