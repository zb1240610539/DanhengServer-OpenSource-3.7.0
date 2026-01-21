using EggLink.DanhengServer.Database; // 必须引用以支持 SaveInstance
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Gacha;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.Gacha;

[Opcode(CmdIds.ExchangeGachaCeilingCsReq)]
public class HandlerExchangeGachaCeilingCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = ExchangeGachaCeilingCsReq.Parser.ParseFrom(data);
        var player = connection.Player!;
        var gachaData = player.GachaManager!.GachaData!;

        // 1. 验证领取条件
        if (gachaData.StandardCumulativeCount < 300 || gachaData.IsStandardSelected)
        {
            await connection.SendPacket(new PacketExchangeGachaCeilingScRsp(3602));
            return;
        }

        // 2. 使用 InventoryManager 的 AddItem 发放奖励（内部已处理头像和星魂转化）
        var result = await player.InventoryManager!.AddItem((int)req.AvatarId, 1, notify: true, sync: true);

        if (result != null)
        {
            var rsp = new ExchangeGachaCeilingScRsp
            {
                Retcode = 0,
                GachaType = req.GachaType,
                AvatarId = req.AvatarId,
                GachaCeiling = new GachaCeiling
                {
                    CeilingNum = (uint)gachaData.StandardCumulativeCount,
                    IsClaimed = true
                }
            };

            // 修复 CS0118: 使用明确的 Proto.Item 类型名，避免与 Item 命名空间冲突
            if (result.ItemId == (int)req.AvatarId + 10000)
            {
                rsp.TransferItemList = new ItemList();
                rsp.TransferItemList.ItemList_.Add(new EggLink.DanhengServer.Proto.Item
                {
                    ItemId = (uint)result.ItemId,
                    Number = 1
                });
            }

            // 修复 CS1061: GachaManager 没有 Save 方法，改用 DatabaseHelper 直接保存数据实例
            gachaData.IsStandardSelected = true;
            DatabaseHelper.Instance!.SaveInstance(gachaData);

            await connection.SendPacket(new PacketExchangeGachaCeilingScRsp(rsp));
        }
    }
}
