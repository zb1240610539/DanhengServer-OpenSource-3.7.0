using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Custom;
using EggLink.DanhengServer.Enums.Rogue;
using EggLink.DanhengServer.Enums.TournRogue;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;
using System;

namespace EggLink.DanhengServer.GameServer.Game.RogueTourn.Scene;

public class RogueTournRoomInstance(int roomIndex, RogueTournLevelInstance levelInstance)
{
    public uint RoomId { get; set; }
    public int RoomIndex { get; set; } = roomIndex;
    public RogueTournRoomStatus Status { get; set; } = RogueTournRoomStatus.None;
    public RogueTournLevelInstance LevelInstance { get; set; } = levelInstance;
    public RogueTournRoomTypeEnum RoomType { get; set; }
    public RogueTournVariantTypeEnum VariantType { get; set; }

    public RogueTournRoomConfig? Config { get; set; }

    public RogueTournRoomList ToProto()
    {
        return new RogueTournRoomList
        {
            RoomId = RoomId,
            RoomIndex = (uint)RoomIndex,
            Status = Status
        };
    }

    public void Init(RogueTournRoomTypeEnum type)
    {
        if (Status is RogueTournRoomStatus.Processing or RogueTournRoomStatus.Finish)
            return; // already initialized

        RoomType = type;
        VariantType = type switch
        {
            RogueTournRoomTypeEnum.Battle => RoomIndex == 1 ? RogueTournVariantTypeEnum.BattleTriple : RogueTournVariantTypeEnum.BattleDouble,
            RogueTournRoomTypeEnum.Event => RogueTournVariantTypeEnum.EventTriple,
            RogueTournRoomTypeEnum.Encounter => RogueTournVariantTypeEnum.EncounterDouble,
            RogueTournRoomTypeEnum.Reward => RogueTournVariantTypeEnum.RewardDouble,
            _ => RogueTournVariantTypeEnum.None
        };

        Status = RogueTournRoomStatus.Processing;

        // get config
        Config = RoomType == RogueTournRoomTypeEnum.Adventure
            ? GameData.RogueTournRoomGenData.Where(x => x.RoomType == RoomType).ToList().RandomElement()
            : GameData.RogueTournRoomGenData
                .Where(x => x.EntranceId == LevelInstance.EntranceId && x.RoomType == RoomType).ToList()
                .RandomElement();

        if (Config == null)
        {
            Status = RogueTournRoomStatus.Finish;
            return;
        }

        // get room id (unique)
        var entryIdSuffix = (Config?.EntranceId ?? 0) % 1000;
        var suffix = entryIdSuffix * 100 + 20 + LevelInstance.LevelIndex;

        var sameTypeExcels = GameData.RogueTournRoomData.Where(x =>
            x.Value.RogueRoomType == RoomType && x.Key.ToString().EndsWith(suffix.ToString())).ToArray();

        if (sameTypeExcels.Length == 0)
        {
            suffix -= LevelInstance.LevelIndex;

            sameTypeExcels = GameData.RogueTournRoomData.Where(x =>
                x.Value.RogueRoomType == RoomType && x.Key.ToString().EndsWith(suffix.ToString())).ToArray();
        }

        sameTypeExcels = sameTypeExcels.Where(x => LevelInstance.Rooms.All(r => r.RoomId != x.Key)).ToArray();  // unique

        var sameVariantTypeExcels = sameTypeExcels.Where(x => x.Value.VariantType == VariantType).ToArray();
        if (sameVariantTypeExcels.Length > 0)
            sameTypeExcels = sameVariantTypeExcels;

        RoomId = sameTypeExcels.Select(x => x.Key)
            .ToList()
            .RandomElement();
    }

    public List<int> GetLoadGroupList()
    {
        var groupList = new List<int>();
        groupList.AddRange(Config!.DefaultLoadBasicGroup);
        if (VariantType.ToString().Contains("Double"))
            groupList.AddRange(Config.DefaultLoadGroup.Take(2));
        else if (VariantType.ToString().Contains("Single"))
            groupList.AddRange(Config.DefaultLoadGroup.Take(1));
        else
            groupList.AddRange(Config.DefaultLoadGroup);

        //if (RoomIndex == 1)  // first room
        groupList.AddRange(Config.SubMonsterGroup);

        return groupList;
    }
}