// 文件路径: GameServer/Server/Packet/Send/Activity/PacketTakeLoginActivityRewardScRsp.cs
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Activity;

public class PacketTakeLoginActivityRewardScRsp : BasePacket
{
    public PacketTakeLoginActivityRewardScRsp(uint activityId, uint takeDays, uint retcode, ItemList rewards, uint panelId) 
        : base((ushort)CmdIds.TakeLoginActivityRewardScRsp) 
    {
        var proto = new TakeLoginActivityRewardScRsp
        {
            Id = activityId,
            TakeDays = takeDays,
            Retcode = retcode,
            Reward = rewards,
            PanelId = panelId // 这里现在是动态的
        };
        this.SetData(proto); 
    }
}