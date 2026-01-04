using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Player;

public class PacketQueryProductInfoScRsp : BasePacket
{
    public PacketQueryProductInfoScRsp() : base(CmdIds.QueryProductInfoScRsp)
    {
        var proto = new QueryProductInfoScRsp
        {
            //PEKJLBINDGG = (ulong)Extensions.GetUnixSec() + 8640000, // 100 day
            ProductList =
            {
                GameData.RechargeConfigData.Values
                    .Where(x => x.ProductID.Contains("chn") && !x.ProductID.Contains("cloud")).Select(x => new Product
                    {
                        GiftType = (ProductGiftType)x.GiftType,
                        PriceTier = x.TierID,
                        ProductId = x.ProductID
                    })
            }
        };

        SetData(proto);
    }
}