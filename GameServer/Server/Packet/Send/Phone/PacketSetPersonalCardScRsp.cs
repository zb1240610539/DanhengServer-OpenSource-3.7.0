using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Phone;

public class PacketSetPersonalCardScRsp : BasePacket
{
    public PacketSetPersonalCardScRsp(uint id) : base(CmdIds.SetPersonalCardScRsp)
    {
        var proto = new SetPersonalCardScRsp
        {
            PersonalCardId = id
        };

        SetData(proto);
    }
}