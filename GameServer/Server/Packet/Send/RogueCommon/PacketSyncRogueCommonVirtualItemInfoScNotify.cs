using EggLink.DanhengServer.GameServer.Game.Rogue;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.RogueCommon;

public class PacketSyncRogueCommonVirtualItemInfoScNotify : BasePacket
{
    public PacketSyncRogueCommonVirtualItemInfoScNotify(BaseRogueInstance instance) : base(
        CmdIds.SyncRogueCommonVirtualItemInfoScNotify)
    {
        var proto = new SyncRogueCommonVirtualItemInfoScNotify
        {
            CommonItemInfo =
            {
                new RogueCommonVirtualItemInfo
                {
                    VirtualItemId = 31,
                    VirtualItemNum = (uint)instance.CurMoney
                },
                // 2. 同步沉浸券 (ID 33)
                // 直接使用我们刚才在 instance 里定义的属性
                new RogueCommonVirtualItemInfo
                {
                    VirtualItemId = 33,
                    VirtualItemNum = (uint)instance.CurImmersionToken
                }
            }
        };

        SetData(proto);
    }
}
