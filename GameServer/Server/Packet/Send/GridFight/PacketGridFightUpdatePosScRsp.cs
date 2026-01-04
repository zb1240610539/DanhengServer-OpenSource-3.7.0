using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using Google.Protobuf.Collections;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;

public class PacketGridFightUpdatePosScRsp : BasePacket
{
    public PacketGridFightUpdatePosScRsp(Retcode code, RepeatedField<GridFightPosInfo> list) : base(CmdIds.GridFightUpdatePosScRsp)
    {
        var proto = new GridFightUpdatePosScRsp
        {
            Retcode = (uint)code,
            GridFightPosInfoList = { list }
        };

        SetData(proto);
    }
}