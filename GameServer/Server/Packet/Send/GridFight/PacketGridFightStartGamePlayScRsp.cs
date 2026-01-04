using EggLink.DanhengServer.GameServer.Game.GridFight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightStartGamePlayScRsp : BasePacket
{
    public PacketGridFightStartGamePlayScRsp(Retcode ret, GridFightInstance? inst) : base(CmdIds.GridFightStartGamePlayScRsp)
    {
        var rsp = new GridFightStartGamePlayScRsp
        {
            Retcode = (uint)ret
        };

        if (inst != null)
            rsp.FightCurrentInfo = inst.ToProto();

        SetData(rsp);
    }
}