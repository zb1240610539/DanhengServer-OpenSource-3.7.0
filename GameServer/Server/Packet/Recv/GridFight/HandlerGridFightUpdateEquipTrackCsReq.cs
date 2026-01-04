using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.GridFight;

[Opcode(CmdIds.GridFightUpdateEquipTrackCsReq)]
public class HandlerGridFightUpdateEquipTrackCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = GridFightUpdateEquipTrackCsReq.Parser.ParseFrom(data);

        var inst = connection.Player!.GridFightManager!.GridFightInstance;
        if (inst == null)
        {
            await connection.SendPacket(CmdIds.GridFightUpdateEquipTrackScRsp);
            return;
        }

        var component = inst.GetComponent<GridFightBasicComponent>();
        foreach (var info in req.TrackInfoList)
        {
            var target = component.Data.TrackingEquipments.FirstOrDefault(x => x.RoleId == info.TrackRoleId);
            if (info.IsTracking)
            {
                if (target == null)
                {
                    target = new GridFightEquipmentTrackInfoPb
                    {
                        RoleId = info.TrackRoleId,
                        Priority = info.TrackPriority,
                    };
                    target.EquipmentIds.AddRange(info.GridFightItemList);

                    component.Data.TrackingEquipments.Add(target);
                }
                else
                {
                    target.Priority = info.TrackPriority;
                    target.EquipmentIds.Clear();
                    target.EquipmentIds.AddRange(info.GridFightItemList);
                }
            }
            else
            {
                if (target != null)
                {
                    component.Data.TrackingEquipments.Remove(target);
                }
            }
        }

        // sync
        await connection.SendPacket(
            new PacketGridFightSyncUpdateResultScNotify(
                new GridFightRoleTrackEquipmentSyncData(GridFightSrc.KGridFightSrcNone, component)));

        await connection.SendPacket(CmdIds.GridFightUpdateEquipTrackScRsp);
    }
}