using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.EraFlipper;

public class PacketResetEraFlipperDataScRsp : BasePacket
{
    public PacketResetEraFlipperDataScRsp(int regionId, int state, bool leave) : base(CmdIds.ResetEraFlipperDataScRsp)
    {
        var proto = new ResetEraFlipperDataScRsp
        {
            Data = new EraFlipperDataList
            {
                EraFlipperDataList_ =
                {
                    new EraFlipperData
                    {
                        EraFlipperRegionId = (uint)regionId,
                        State = (uint)state
                    }
                }
            },
            PAHMAGPFDDJ = leave
        };

        SetData(proto);
    }

    public PacketResetEraFlipperDataScRsp(Retcode code) : base(CmdIds.ResetEraFlipperDataScRsp)
    {
        var proto = new ResetEraFlipperDataScRsp
        {
            Retcode = (uint)code
        };
        SetData(proto);
    }
}