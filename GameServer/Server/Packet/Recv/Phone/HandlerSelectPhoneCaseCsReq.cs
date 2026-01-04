using EggLink.DanhengServer.GameServer.Server.Packet.Send.Phone;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Phone;

[Opcode(CmdIds.SelectPhoneCaseCsReq)]
public class HandlerSelectPhoneCaseCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SelectPhoneCaseCsReq.Parser.ParseFrom(data);

        connection.Player!.Data.PhoneCase = (int)req.PhoneCase;

        await connection.SendPacket(new PacketSelectPhoneCaseScRsp(req.PhoneCase));
    }
}