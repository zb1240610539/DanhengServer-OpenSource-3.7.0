using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Send.EraFlipper;

public class PacketEraFlipperDataChangeScNotify : BasePacket
{
    public PacketEraFlipperDataChangeScNotify(ChangeEraFlipperDataCsReq req, int floorId) : base(
        CmdIds.EraFlipperDataChangeScNotify)
    {
        var proto = new EraFlipperDataChangeScNotify
        {
            Data = req.Data,
            FloorId = (uint)floorId
        };

        SetData(proto);
    }

    public PacketEraFlipperDataChangeScNotify(int floorId, int regionId, int state) : base(
        CmdIds.EraFlipperDataChangeScNotify)
    {
        var proto = new EraFlipperDataChangeScNotify
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
            FloorId = (uint)floorId
        };

        SetData(proto);
    }

    public PacketEraFlipperDataChangeScNotify(int floorId) : base(CmdIds.EraFlipperDataChangeScNotify)
    {
        var proto = new EraFlipperDataChangeScNotify
        {
            FloorId = (uint)floorId
        };

        SetData(proto);
    }
}