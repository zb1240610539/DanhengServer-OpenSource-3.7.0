using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database.Avatar;
using EggLink.DanhengServer.Database.Lineup;
using EggLink.DanhengServer.Enums.GridFight;
using EggLink.DanhengServer.GameServer.Game.GridFight;
using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;
using LineupInfo = EggLink.DanhengServer.Database.Lineup.LineupInfo;

namespace EggLink.DanhengServer.GameServer.Game.Battle.Custom;

public class BattleGridFightOptions(GridFightGameSectionInfo curSection, GridFightInstance inst, PlayerInstance player)
{
    public GridFightGameEncounterInfo Encounter { get; set; } = curSection.Encounters.RandomElement();
    public GridFightInstance Inst { get; set; } = inst;
    public GridFightRoleComponent RoleComponent { get; set; } = inst.GetComponent<GridFightRoleComponent>();
    public GridFightLevelComponent LevelComponent { get; set; } = inst.GetComponent<GridFightLevelComponent>();
    public GridFightBasicComponent BasicComponent { get; set; } = inst.GetComponent<GridFightBasicComponent>();
    public GridFightAugmentComponent AugmentComponent { get; set; } = inst.GetComponent<GridFightAugmentComponent>();
    public GridFightTraitComponent TraitComponent { get; set; } = inst.GetComponent<GridFightTraitComponent>();
    public GridFightItemsComponent ItemsComponent { get; set; } = inst.GetComponent<GridFightItemsComponent>();
    public GridFightGameSectionInfo CurSection { get; set; } = curSection;
    public PlayerInstance Player { get; set; } = player;

    public void HandleProto(SceneBattleInfo proto, BattleInstance battle)
    {
        var avatars = RoleComponent.GetForegroundAvatarInfos();
        var backAvatars = RoleComponent.GetBackgroundAvatarInfos(BasicComponent.GetFieldCount());

        var tempLineup = new LineupInfo
        {
            BaseAvatars = avatars.Concat(backAvatars).Select(y => new LineupAvatarInfo
            {
                BaseAvatarId = y.BaseAvatarId
            }).ToList(),
            LineupType = (int)ExtraLineupType.LineupGridFight
        };

        // set all avatars to full hp for battle
        foreach (var baseAvatarInfo in avatars.Concat(backAvatars))
        {
            baseAvatarInfo.SetCurHp(10000, true);
        }

        // foreground avatars
        var formatted = avatars.Select(x =>
            x.ToBattleProto(
                new PlayerDataCollection(Player.Data, Player.InventoryManager!.Data, tempLineup),
                x is SpecialAvatarInfo ? AvatarType.AvatarTrialType : AvatarType.AvatarGridFightType)).ToList();

        // background avatars
        var backFormatted = backAvatars.Select(x =>
            x.ToBattleProto(
                new PlayerDataCollection(Player.Data, Player.InventoryManager!.Data, tempLineup),
                x is SpecialAvatarInfo ? AvatarType.AvatarTrialType : AvatarType.AvatarGridFightType)).ToList();

        proto.BattleAvatarList.Add(formatted.Take(4));

        // affix buff
        foreach (var affix in LevelComponent.Affixes)
        {
            if (!GameData.GridFightAffixConfigData.TryGetValue(affix, out var affixConf) || affixConf.AffixRule != GridFightAffixRuleEnum.Mazebuff) continue;
            battle.Buffs.Add(new MazeBuff(35300000 + (int)affix, 1, -1)  // TODO I WANNA READ FROM GAMEDATA, BUT IT SEEMS GAMEDATA IS WRONG
            {
                WaveFlag = -1
            });
        }

        // monsters
        foreach (var wave in Encounter.MonsterWaves)
        {
            proto.MonsterWaveList.Add(new SceneMonsterWave
            {
                BattleStageId = proto.StageId,
                BattleWaveId = wave.Wave,
                MonsterParam = new SceneMonsterWaveParam
                {
                    EliteGroup = CurSection.EliteGroupId,
                    DNEAMPLLFME = 5  // ?
                },
                MonsterList =
                {
                    wave.Monsters.Select(x => new SceneMonster
                    {
                        MonsterId = x.Monster.MonsterID,
                        ExtraInfo = new SceneMonsterExtraInfo
                        {
                            BattleGridFightInfo = new SceneMonsterGridFightInfo
                            {
                                Tier = Math.Max(1, x.Tier),
                                GridFightDropItemList = { x.DropItems }
                            }
                        }
                    })
                }
            });
        }

        // battle events
        foreach (var role in RoleComponent.Data.Roles)
        {
            if (!GameData.GridFightRoleStarData.TryGetValue(role.RoleId << 4 | role.Tier, out var roleConf)) continue;
            battle.BattleEvents.TryAdd((int)roleConf.BEID, new BattleEventInstance((int)roleConf.BEID, 5000));
        }

        // traits battle events
        foreach (var traitBeId in TraitComponent.Data.Traits
                     .Select(x => GameData.GridFightTraitBasicInfoData.GetValueOrDefault(x.TraitId, new()))
                     .SelectMany(x => x.BEIDList))
        {
            battle.BattleEvents.TryAdd((int)traitBeId, new BattleEventInstance((int)traitBeId, 5000));
        }

        // penalty bonus rule id
        var ruleId = CurSection.Excel.PenaltyBonusRuleIDList.FirstOrDefault(0u);
        if (ruleId == 0)
        {
            if (GameData.GridFightNodeTemplateData.TryGetValue(CurSection.Excel.NodeTemplateID, out var node)) ruleId = node.PenaltyBonusRuleID;
        }

        proto.BattleGridFightInfo = new BattleGridFightInfo
        {
            GridGameRoleList =
            {
                RoleComponent.Data.Roles.Where(x => x.Pos <= BasicComponent.GetFieldCount()).OrderBy(x => x.Pos).Select(x => x.ToBattleInfo(ItemsComponent.Data))
            },
            GridFightCurLevel = BasicComponent.Data.CurLevel,
            GridFightLineupHp = BasicComponent.Data.CurHp,
            GridFightAvatarList = { backFormatted },
            GridFightStageInfo = new BattleGridFightStageInfo
            {
                ChapterId = CurSection.ChapterId,
                RouteId = CurSection.Excel.ID,
                SectionId = CurSection.SectionId
            },
            IsOverlock = Inst.IsOverLock,
            Season = Inst.Season,
            BattleDifficulty = AugmentComponent.GetAugmentDifficulty() + Inst.GetDivisionDifficulty() +
                               (Encounter.EncounterDifficulty - 1) * 5,
            GameDivisionId = Inst.DivisionId,
            PenaltyBonusRuleId = ruleId,
            GridFightAugmentInfo = { AugmentComponent.Data.Augments.Select(x => x.ToBattleInfo()) },
            GridFightPortalBuffList = { LevelComponent.PortalBuffs.Select(x => x.ToBattleInfo()) },
            GridFightTraitInfo = { TraitComponent.Data.Traits.Select(x => x.ToBattleInfo(RoleComponent)) },
            GridGameNpcList = { RoleComponent.Data.Npcs.Select(x => x.ToBattleInfo(ItemsComponent.Data)) }
        };
    }
}