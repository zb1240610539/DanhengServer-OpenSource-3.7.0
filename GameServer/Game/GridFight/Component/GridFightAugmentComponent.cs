using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.GridFight.Sync;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.GridFight;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Component;

public class GridFightAugmentComponent(GridFightInstance inst) : BaseGridFightComponent(inst)
{
    public GridFightAugmentInfoPb Data { get; set; } = new();

    public async ValueTask<List<BaseGridFightSyncData>> AddAugment(uint augmentId, bool sendPacket = true, GridFightSrc src = GridFightSrc.KGridFightSrcSelectAugment, uint syncGroup = 0)
    {
        if (!GameData.GridFightAugmentData.TryGetValue(augmentId, out var excel)) return [];

        var info = new GridFightGameAugmentPb
        {
            AugmentId = augmentId
        };

        foreach (var saved in excel.AugmentSavedValueList)
        {
            info.SavedValues.Add(saved, 0);
        }

        Data.Augments.Add(info);

        var syncData = new GridFightAddAugmentSyncData(src, info, syncGroup);
        if (sendPacket)
        {
            await Inst.Player.SendPacket(new PacketGridFightSyncUpdateResultScNotify(syncData));
        }

        return [syncData];
    }

    public uint GetAugmentDifficulty()
    {
        if (!GameData.GridFightDivisionInfoData.TryGetValue(Inst.DivisionId, out var excel)) return 0;
        var difficulty = 0u;

        foreach (var augment in Data.Augments)
        {
            if (!GameData.GridFightAugmentData.TryGetValue(augment.AugmentId, out var augmentExcel)) continue;

            var diff = GameData.GridFightAugmentMonsterData.GetValueOrDefault(excel.DivisionLevel, [])
                .GetValueOrDefault(augmentExcel.Quality)?.EnemyDiffLvAdd ?? 0;

            difficulty += diff;
        }

        return difficulty;
    }

    public override GridFightGameInfo ToProto()
    {
        return new GridFightGameInfo
        {
            GridAugmentInfo = new GridFightGameAugmentInfo
            {
                GridFightAugmentInfo = { Data.Augments.Select(x => x.ToProto()) }
            }
        };
    }
}

public static class GridFightAugmentExtensions 
{
    public static GridGameAugmentInfo ToProto(this GridFightGameAugmentPb info)
    {
        return new GridGameAugmentInfo
        {
            AugmentId = info.AugmentId,
            GameSavedValueMap = { info.SavedValues }
        };
    }

    public static BattleGridFightAugmentInfo ToBattleInfo(this GridFightGameAugmentPb info)
    {
        return new BattleGridFightAugmentInfo
        {
            AugmentId = info.AugmentId,
            GameSavedValueMap = { info.SavedValues }
        };
    }
}