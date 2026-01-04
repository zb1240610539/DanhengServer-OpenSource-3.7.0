using EggLink.DanhengServer.GameServer.Game.Scene.Component;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.EraFlipper;
using EggLink.DanhengServer.Kcp;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Server.Packet.Recv.EraFlipper;

[Opcode(CmdIds.ChangeEraFlipperDataCsReq)]
public class HandlerChangeEraFlipperDataCsReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = ChangeEraFlipperDataCsReq.Parser.ParseFrom(data);

        var component = connection.Player!.SceneInstance!.GetComponent<EraFlipperSceneComponent>();
        if (component == null)
        {
            await connection.SendPacket(new PacketChangeEraFlipperDataScRsp(Retcode.RetAdventureMapNotExist));
            return;
        }

        if (req.Data.EraFlipperDataList_.Any(x => x.EraFlipperRegionId == 2))
        {
            var curValue = connection.Player.SceneInstance!.GetFloorSavedValue("FSV_FlashBackCount") + 1;
            await connection.Player!.SceneInstance!.UpdateFloorSavedValue("FSV_FlashBackCount", curValue);

            Dictionary<int, int> gpValueDict = [];
            gpValueDict.Add(1, 2);
            gpValueDict.Add(2, 3);
            gpValueDict.Add(3, 5);
            gpValueDict.Add(4, 6);
            var gpValue = gpValueDict.GetValueOrDefault(curValue, 0);
            await connection.Player.SceneInstance!.UpdateGroupProperty(74, "MimiGoStep", gpValue);
        }

        component.ChangeEraFlipperStates(req.Data.EraFlipperDataList_.ToList());
        await connection.SendPacket(new PacketChangeEraFlipperDataScRsp(req));
        //await connection.SendPacket(new PacketEraFlipperDataChangeScNotify(req, connection.Player!.SceneInstance.FloorId));
    }
}