using EggLink.DanhengServer.Data.Custom;
using EggLink.DanhengServer.GameServer.Game.RogueTourn;
using EggLink.DanhengServer.Proto;
using EggLink.DanhengServer.Util;

namespace EggLink.DanhengServer.GameServer.Game.Rogue.Buff;

public class RogueBuffSelectMenu(BaseRogueInstance rogue)
{
    public int HintId { get; set; } = 1;
    public List<BaseRogueBuffExcel> Buffs { get; set; } = [];
    public int RollMaxCount { get; set; } = rogue.BaseRerollCount;
    public int RollCount { get; set; }
    public int RollFreeCount { get; set; } = rogue.BaseRerollFreeCount;
    public int RollCost { get; set; } = rogue.CurRerollCost;
    public int QueueAppend { get; set; } = 3;
    public bool IsAeonBuff { get; set; } = false;
    public int CurCount { get; set; } = 1;
    public int TotalCount { get; set; } = 1;
    public List<BaseRogueBuffExcel> BuffPool { get; set; } = [];

    public void RollBuff(List<BaseRogueBuffExcel> buffs, int count = 3)
	{
    BuffPool.Clear();
    BuffPool.AddRange(buffs);

    var list = new RandomList<BaseRogueBuffExcel>();

    // --- 第一步：过滤 ---
    // 找出池子中属于当前命途的祝福
    var pathSpecificBuffs = buffs.Where(b => b.RogueBuffType == rogue.RogueBuffType).ToList();

    // --- 第二步：权重分配 ---
    if (pathSpecificBuffs.Count > 0)
    {
        // 如果有本命途祝福，只把它们加入随机列表
        foreach (var buff in pathSpecificBuffs)
        {
            list.Add(buff, 20); // 保留你的 20 权重
        }
    }
    else
    {
        // 只有在本命途祝福全被抽完的情况下，才开放其他命途
        foreach (var buff in buffs)
        {
            list.Add(buff, 15); // 保留你的 15 权重
        }
    }

    // --- 第三步：抽取 ---
    var result = new List<BaseRogueBuffExcel>();
    for (var i = 0; i < count; i++)
    {
        var buff = list.GetRandom();
        if (buff != null)
        {
            result.Add(buff);
            list.Remove(buff);
        }

        if (list.GetCount() == 0) break; 
    }

    Buffs = result;
}

    public async ValueTask RerollBuff()
    {
        if (RollFreeCount > 0)
        {
            RollFreeCount--; // Free reroll
        }
        else
        {
            if (RollMaxCount - RollCount <= 0) return;
            RollCount++; // Paid reroll
            await rogue.CostMoney(RollCost);
        }

        RollBuff(BuffPool.ToList());
    }

    public RogueActionInstance GetActionInstance()
    {
        rogue.CurActionQueuePosition += QueueAppend;
        return new RogueActionInstance
        {
            QueuePosition = rogue.CurActionQueuePosition,
            RogueBuffSelectMenu = this
        };
    }

    public RogueCommonBuffSelectInfo ToProto()
    {
        var info = new RogueCommonBuffSelectInfo
        {
            CanRoll = true,
            RollBuffCount = (uint)RollCount,
            RollBuffFreeCount = (uint)RollFreeCount,
            RollBuffMaxCount = (uint)RollMaxCount,
            SourceCurCount = (uint)CurCount,
            RollBuffCostData = new ItemCostData
            {
                ItemList =
                {
                    new ItemCost
                    {
                        PileItem = new PileItem
                        {
                            ItemId = 31,
                            ItemNum = (uint)RollCost
                        }
                    }
                }
            },
            SourceHintId = (uint)HintId,
            HandbookUnlockBuffIdList = { Buffs.Select(x => (uint)x.MazeBuffID) },
            SelectBuffs = { Buffs.Select(x => x.ToProto()) }
        };

        if (rogue is RogueTournInstance)
        {
            info.RollBuffCostData = null;
            info.HandbookUnlockBuffIdList.Clear();
        }

        return info;
    }

    public RogueCommonBuffReforgeSelectInfo ToReforgeProto()
    {
        return new RogueCommonBuffReforgeSelectInfo
        {
            SelectBuffs = { Buffs.Select(x => x.ToProto()) }
        };
    }
}
