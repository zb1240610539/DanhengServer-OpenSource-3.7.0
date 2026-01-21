using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.Friend;
public class PacketGetPlayerDetailInfoScRsp : BasePacket
{
    // 原有的构造函数保留
    public PacketGetPlayerDetailInfoScRsp(PlayerDetailInfo info) : base(CmdIds.GetPlayerDetailInfoScRsp)
    {
        var proto = new GetPlayerDetailInfoScRsp { DetailInfo = info };
        SetData(proto);
    }

    // --- 核心修复：新增支持详细角色列表的构造函数 ---
    public PacketGetPlayerDetailInfoScRsp(PlayerDetailInfo info, List<DisplayAvatarDetailInfo> displayList) 
        : base(CmdIds.GetPlayerDetailInfoScRsp)
    {
        // 关键：PlayerDetailInfo 里的 DisplayAvatarList 才是控制详情弹窗的数据源
        info.DisplayAvatarList.AddRange(displayList);

        var proto = new GetPlayerDetailInfoScRsp
        {
            DetailInfo = info
        };

        SetData(proto);
    }

    public PacketGetPlayerDetailInfoScRsp() : base(CmdIds.GetPlayerDetailInfoScRsp)
    {
        var proto = new GetPlayerDetailInfoScRsp { Retcode = 3612 };
        SetData(proto);
    }
}
