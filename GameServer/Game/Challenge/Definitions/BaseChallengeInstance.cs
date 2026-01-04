using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.GameServer.Game.Battle;
using EggLink.DanhengServer.GameServer.Game.Player;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Proto.ServerSide;

namespace EggLink.DanhengServer.GameServer.Game.Challenge.Definitions;

public abstract class BaseChallengeInstance(PlayerInstance player, ChallengeDataPb data)
{
    public PlayerInstance Player { get; } = player;
    public ChallengeDataPb Data { get; } = data;

    public virtual void OnBattleStart(BattleInstance battle)
    {
        battle.OnBattleEnd += OnBattleEnd;
    }

    public virtual async ValueTask OnBattleEnd(BattleInstance battle, PVEBattleResultCsReq req)
    {
        await ValueTask.CompletedTask;
    }

    public abstract Dictionary<int, List<ChallengeConfigExcel.ChallengeMonsterInfo>> GetStageMonsters();

    public virtual void OnUpdate()
    {
    }
}