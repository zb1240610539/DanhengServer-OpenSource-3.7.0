using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Config.Scene;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.Database.Inventory;
using EggLink.DanhengServer.Enums.Mission;
using EggLink.DanhengServer.GameServer.Game.Battle;
using EggLink.DanhengServer.GameServer.Game.Scene.Component;
using EggLink.DanhengServer.GameServer.Server.Packet.Send.Scene;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;
using EggLink.DanhengServer.Enums.Scene;
using EggLink.DanhengServer.GameServer.Game.Rogue.Scene.Entity;
namespace EggLink.DanhengServer.GameServer.Game.Scene.Entity;

public class EntityMonster(
    SceneInstance scene,
    Position pos,
    Position rot,
    int groupId,
    int instId,
    NPCMonsterDataExcel excel,
    MonsterInfo info) : BaseGameEntity, IGameModifier
{
    public Position Position { get; set; } = pos;
    public Position Rotation { get; set; } = rot;
    public int InstId { get; set; } = instId;
    public SceneInstance Scene { get; set; } = scene;
    public NPCMonsterDataExcel MonsterData { get; set; } = excel;
    public MonsterInfo Info { get; set; } = info;
    public SceneBuff? TempBuff { get; set; }
    public bool IsAlive { get; private set; } = true;

    public int EventId { get; set; } = info.EventID;
    public int CustomStageId { get; set; } = 0;

    public int RogueMonsterId { get; set; } = 0;
    public int CustomLevel { get; set; } = 0;
    public int HardLevelGroup { get; set; } = 0;
    public override int EntityId { get; set; } = 0;
    public override int GroupId { get; set; } = groupId;

    public List<string> Modifiers { get; set; } = [];

    public async ValueTask AddModifier(string modifierName)
    {
        if (Modifiers.Contains(modifierName)) return;

        GameData.AdventureModifierData.TryGetValue(modifierName, out var modifier);
        GameData.AdventureAbilityConfigListData.TryGetValue(MonsterData.ID, out var ability);
        if (modifier == null || ability == null) return;

        await Scene.Player.TaskManager!.AbilityLevelTask.TriggerTasks(ability, modifier.OnCreate, this, [],
            new SceneCastSkillCsReq());

        Modifiers.Add(modifierName);
    }

    public async ValueTask RemoveModifier(string modifierName)
    {
        if (!Modifiers.Contains(modifierName)) return;

        GameData.AdventureModifierData.TryGetValue(modifierName, out var modifier);
        GameData.AdventureAbilityConfigListData.TryGetValue(MonsterData.ID, out var ability);
        if (modifier == null || ability == null) return;

        await Scene.Player.TaskManager!.AbilityLevelTask.TriggerTasks(ability, modifier.OnDestroy, this, [],
            new SceneCastSkillCsReq());

        Modifiers.Remove(modifierName);
    }

    public override async ValueTask AddBuff(SceneBuff buff)
    {
        if (!GameData.MazeBuffData.TryGetValue(buff.BuffId * 10 + buff.BuffLevel, out var buffExcel)) return;

        await AddModifier(buffExcel.ModifierName);
        var oldBuff = BuffList.Find(x => x.BuffId == buff.BuffId);
        if (oldBuff != null) BuffList.Remove(oldBuff);
        BuffList.Add(buff);
        await Scene.Player.SendPacket(new PacketSyncEntityBuffChangeListScNotify(this, buff));
    }

    public override async ValueTask ApplyBuff(BattleInstance instance)
    {
        if (TempBuff != null)
        {
            instance.Buffs.Add(new MazeBuff(TempBuff));
            TempBuff = null;
        }

        if (BuffList.Count == 0) return;

        foreach (var buff in BuffList)
        {
            if (buff.IsExpired()) continue;
            instance.Buffs.Add(new MazeBuff(buff));
        }

        await Scene.Player.SendPacket(new PacketSyncEntityBuffChangeListScNotify(this, BuffList));

        BuffList.Clear();
    }

    public override SceneEntityInfo ToProto()
    {
        var proto = new SceneEntityInfo
        {
            EntityId = (uint)EntityId,
            GroupId = (uint)GroupId,
            InstId = (uint)InstId,
            Motion = new MotionInfo
            {
                Pos = Position.ToProto(),
                Rot = Rotation.ToProto()
            },
            NpcMonster = new SceneNpcMonsterInfo
            {
                EventId = (uint)EventId,
                MonsterId = (uint)MonsterData.ID,
                WorldLevel = (uint)Scene.Player.Data.WorldLevel
            }
        };

        if (RogueMonsterId > 0)
            proto.NpcMonster.ExtraInfo = new NpcMonsterExtraInfo
            {
                RogueGameInfo = new NpcMonsterRogueInfo
                {
                    RogueMonsterId = (uint)RogueMonsterId,
                    Level = (uint)CustomLevel,
                    HardLevelGroup = (uint)HardLevelGroup
                }
            };

        return proto;
    }

    public async ValueTask RemoveBuff(int buffId)
    {
        if (!GameData.MazeBuffData.TryGetValue(buffId * 10 + 1, out var buffExcel)) return;

        var buff = BuffList.Find(x => x.BuffId == buffId);
        if (buff == null) return;

        BuffList.Remove(buff);
        await Scene.Player.SendPacket(new PacketSyncEntityBuffChangeListScNotify(this, [buff]));

        await RemoveModifier(buffExcel.ModifierName);
    }

    public int GetStageId()
    {
        if (CustomStageId > 0) return CustomStageId;
        return Info.EventID;
    }
   public async ValueTask<List<ItemData>> Kill(bool sendPacket = true)
    {
        IsAlive = false;

        // 1. 处理掉落逻辑：根据怪物ID和世界等级获取掉落物
        GameData.MonsterDropData.TryGetValue(MonsterData.ID * 10 + Scene.Player.Data.WorldLevel, out var dropData);
        var dropItems = dropData != null ? dropData.CalculateDrop() : [];
        if (dropItems.Count > 0)
        {
            await Scene.Player.InventoryManager!.AddItems(dropItems, sendPacket);
        }

        // --- 核心修复：红锁解锁与肉鸽逻辑隔离 ---

        // 定义所有的肉鸽/挑战模式黑名单
        var isRogueMode = Scene.GameModeType == GameModeTypeEnum.RogueExplore ||   // 模拟宇宙
                          Scene.GameModeType == GameModeTypeEnum.RogueChallenge || // 周期性挑战
                          Scene.GameModeType == GameModeTypeEnum.RogueAeonRoom ||  // 星神房
                          Scene.GameModeType == GameModeTypeEnum.ChessRogue ||      // 黄金与机械/蝗灾
                          Scene.GameModeType == GameModeTypeEnum.TournRogue ||      // 差分宇宙
                          Scene.GameModeType == GameModeTypeEnum.MagicRogue;       // 不可知域

        if (!isRogueMode)
        {
            // --- A. 大世界逻辑：解锁同组宝箱 ---
            var relatedProps = Scene.Entities.Values
                .OfType<EntityProp>()
                .Where(p => p.GroupId == this.GroupId);

            foreach (var prop in relatedProps)
            {
                // 跳过肉鸽专用的传送门实体
                if (prop is RogueProp) continue;

                // 将大世界宝箱从锁定状态改为关闭（可开启）状态
                await prop.SetState(PropStateEnum.ChestClosed);
            }
        }
        else 
        {
            // --- B. 肉鸽模式逻辑：解除沉浸器(8001)的红锁 ---
            var relatedProps = Scene.Entities.Values
                .OfType<EntityProp>()
                .Where(p => p.GroupId == this.GroupId);

            foreach (var prop in relatedProps)
            {
                // 8001 是标准沉浸装置的 Prop ID
                if (prop.Excel.ID == 8001) 
                {
                    // 【关键点】让红锁链消失：
                    // 从 ChestLocked (11) 切换为 WaitActive (17)
                    // WaitActive 会让球体展开并发出可交互的金色光效
                    await prop.SetState(PropStateEnum.WaitActive); 
                }
            }
        }
        // ---------------------------------------

        // 2. 任务处理：触发杀怪相关的任务进度
        await Scene.Player.MissionManager!.HandleFinishType(MissionFinishTypeEnum.KillMonster, this);
        
        // 3. 移除实体：将怪物从当前场景实例中删除
        await Scene.RemoveEntity(this);
        
        return dropItems;
    }
   
}
