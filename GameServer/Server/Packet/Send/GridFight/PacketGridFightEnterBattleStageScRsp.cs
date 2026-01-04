using EggLink.DanhengServer.GameServer.Game.Battle;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightEnterBattleStageScRsp : BasePacket
{
    public PacketGridFightEnterBattleStageScRsp(Retcode code = Retcode.RetSucc, BattleInstance? inst = null) : base(
        CmdIds.GridFightEnterBattleStageScRsp)
    {
        var proto = new GridFightEnterBattleStageScRsp
        {
            Retcode = (uint)code
        };

        if (inst != null)
            proto.BattleInfo = inst.ToProto();

        SetData(proto);
    }
}