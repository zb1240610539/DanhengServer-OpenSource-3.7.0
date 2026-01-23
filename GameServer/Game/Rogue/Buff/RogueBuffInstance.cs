using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Data.Custom;
using EggLink.DanhengServer.Data.Excel;
using EggLink.DanhengServer.Enums.Rogue;
using EggLink.DanhengServer.GameServer.Game.Battle;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.Rogue.Buff;

public class RogueBuffInstance(int buffId, int buffLevel)
{
    public int BuffId { get; set; } = buffId;
    public int BuffLevel { get; set; } = buffLevel;
    public BaseRogueBuffExcel BuffExcel { get; set; } = GameData.RogueBuffData[buffId * 100 + buffLevel];

    public int CurSp { get; set; } = 10000;
    public int MaxSp { get; set; } = 10000;

    public int EnhanceCost => 100 + ((int)BuffExcel.RogueBuffCategory - 1) * 30;

    public void OnStartBattle(BattleInstance battle)
{
    // 1. 处理星神回响逻辑 (保持原有逻辑)
    if (BuffExcel is RogueBuffExcel { BattleEventBuffType: RogueBuffAeonTypeEnum.BattleEventBuff })
    {
        GameData.RogueBattleEventData.TryGetValue(BuffExcel.RogueBuffType, out var battleEvent);
        if (battleEvent != null) 
            battle.BattleEvents.Add(BuffId, new BattleEventInstance(battleEvent.BattleEventID, CurSp, MaxSp));
    }

    // 2. 创建 MazeBuff 实例 (将变量定义在外部，防止作用域导致的 CS0103 错误)
    var mazeBuff = new MazeBuff(BuffId, BuffLevel, -1)
    {
        WaveFlag = -1
    };

    // 3. 核心修复：手动从数值表里抓取 ParamList 并注入
    // 直接查找 RogueMazeBuffData，避开 BaseRogueBuffExcel 转换失败的问题
    if (GameData.RogueMazeBuffData.TryGetValue(BuffId * 100 + BuffLevel, out var mazeExcel))
    {
        if (mazeExcel.ParamList != null)
        {
            for (int i = 0; i < mazeExcel.ParamList.Count; i++)
            {
                // 模拟宇宙通用口令：Value1, Value2...
                string key = $"Value{i + 1}";
                float val = mazeExcel.ParamList[i].Value;
                
                // 存入 DynamicValues 字典
                mazeBuff.DynamicValues[key] = val;
            }
        }
    }

    // 4. 将填好数据的 Buff 加入战斗实例
    battle.Buffs.Add(mazeBuff);
}

    public RogueBuff ToProto()
    {
        return new RogueBuff
        {
            BuffId = (uint)BuffId,
            Level = (uint)BuffLevel
        };
    }

    public RogueCommonBuff ToCommonProto()
    {
        return new RogueCommonBuff
        {
            BuffId = (uint)BuffId,
            BuffLevel = (uint)BuffLevel
        };
    }

    public RogueCommonActionResult ToResultProto(RogueCommonActionResultSourceType source)
    {
        return new RogueCommonActionResult
        {
            RogueAction = new RogueCommonActionResultData
            {
                GetBuffList = new RogueCommonBuff
                {
                    BuffId = (uint)BuffId,
                    BuffLevel = (uint)BuffLevel
                }
            },
            Source = source
        };
    }

    public RogueCommonActionResult ToRemoveResultProto(RogueCommonActionResultSourceType source)
    {
        return new RogueCommonActionResult
        {
            RogueAction = new RogueCommonActionResultData
            {
                RemoveBuffList = new RogueCommonBuff
                {
                    BuffId = (uint)BuffId,
                    BuffLevel = (uint)BuffLevel
                }
            },
            Source = source
        };
    }

    public RogueBuffEnhanceInfo ToEnhanceProto()
    {
        return new RogueBuffEnhanceInfo
        {
            BuffId = (uint)BuffId,
            CostData = new ItemCostData
            {
                ItemList =
                {
                    new ItemCost
                    {
                        PileItem = new PileItem
                        {
                            ItemId = 31,
                            ItemNum = (uint)EnhanceCost
                        }
                    }
                }
            }
        };
    }

    public ChessRogueBuffEnhanceInfo ToChessEnhanceProto()
    {
        return new ChessRogueBuffEnhanceInfo
        {
            BuffId = (uint)BuffId,
            CostData = new ItemCostData
            {
                ItemList =
                {
                    new ItemCost
                    {
                        PileItem = new PileItem
                        {
                            ItemId = 31,
                            ItemNum = (uint)EnhanceCost
                        }
                    }
                }
            }
        };
    }
}
