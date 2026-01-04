using EggLink.DanhengServer.Enums.Item;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Item;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Item;

[Opcode(CmdIds.DiscardRelicCsReq)]
public class HandlerDiscardRelicCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = DiscardRelicCsReq.Parser.ParseFrom(data);
        var result =
            await connection.Player!.InventoryManager!.DiscardItems(req.RelicUniqueIdList, req.IsDiscard,
                ItemMainTypeEnum.Relic);
        await connection.SendPacket(new PacketDiscardRelicScRsp(result, req.IsDiscard));
    }
}