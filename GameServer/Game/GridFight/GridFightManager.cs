using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.GameServer.Game.Rogue;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.GridFight;

public class GridFightManager(PlayerInstance player) : BasePlayerManager(player)
{
    public const uint CurSeasonId = 1;
    public uint CurUniqueId { get; set; }
    public GridFightInstance? GridFightInstance { get; set; }

    #region Game

    public async ValueTask<(Retcode code, GridFightInstance? inst)> StartGamePlay(uint season, uint divisionId, bool isOverLock)
    {
        if (season != CurSeasonId)
            return (Retcode.RetGridFightConfMiss, null);

        if (GridFightInstance != null)
            return (Retcode.RetGridFightAlreadyInGameplay, GridFightInstance);

        GridFightInstance = new GridFightInstance(Player, season, divisionId, isOverLock, ++CurUniqueId);
        GridFightInstance.InitializeComponents();

        await ValueTask.CompletedTask;
        return (Retcode.RetSucc, GridFightInstance);
    }

    #endregion

    #region Serialization

    public GridFightQueryInfo ToProto()
    {
        return new GridFightQueryInfo
        {
            GridFightRewardInfo = ToRewardInfo(),
            GridFightStaticGameInfo = ToGameInfo()
        };
    }

    public GridFightRewardInfo ToRewardInfo()
    {
        var time = RogueManager.GetCurrentRogueTime();

        return new GridFightRewardInfo
        {
            GridFightTalentInfo = new GridFightTalentInfo
            {
                DeployIdList = { GameData.GridFightTalentData.Keys }
            },
            GridFightWeeklyReward = new GridFightTakeWeeklyRewardInfo
            {
                EndTime = time.Item2,
                //FeatureBeginTime = time.Item1
            }
        };
    }

    public GridFightStaticGameInfo ToGameInfo()
    {
        return new GridFightStaticGameInfo
        {
            GridFightTalentInfo = new GridFightTalentInfo
            {
                DeployIdList = { GameData.GridFightSeasonTalentData.Keys }
            },
            DivisionId = GameData.GridFightDivisionInfoData.Where(x => x.Value.SeasonID == CurSeasonId)
                .Select(x => x.Key).Max(),
            GridFightGameValueInfo = ToFightGameValueInfo(),
            Exp = new GridFightExpInfo
            {
                GridWeeklyExtraExp = 1
            },
            MGGGAJJBAMN = 1,
            SubSeasonId = 1
        };
    }

    public GridFightGameValueInfo ToFightGameValueInfo()
    {
        return new GridFightGameValueInfo
        {
            GridFightAvatarInfo = new GridFightAvatarInfo
            {
                GridFightAvatarList = { GameData.GridFightRoleBasicInfoData.Keys }
            },
            GridFightItemInfo = new GridFightItemInfo
            {
                GridFightItemList = { GameData.GridFightItemsData.Keys }
            },
            GridFightCampInfo = new GridFightCampInfo
            {
                GridFightCampList = { GameData.GridFightCampData.Keys }
            },
            GridFightAugmentInfo = new GridFightAugmentInfo
            {
                GridFightAugmentList = { GameData.GridFightAugmentData.Keys }
            },
            GridFightPortalBuffInfo = new GridFightPortalBuffInfo
            {
                GridFightPortalBuffList =
                    { GameData.GridFightPortalBuffData.Values.Where(x => x.IfInBook).Select(x => x.ID) }
            }
        };
    }

    #endregion
}