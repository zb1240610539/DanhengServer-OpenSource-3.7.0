using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;

public class PacketGetAssistListScRsp : BasePacket
{
    // 修改构造函数，允许传入已经填充好数据的 Proto 对象
    public PacketGetAssistListScRsp(GetAssistListScRsp proto) : base(CmdIds.GetAssistListScRsp)
    {
        SetData(proto);
    }
}
