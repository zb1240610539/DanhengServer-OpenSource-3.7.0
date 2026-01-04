using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.GameServer.Game.GridFight.Component;
using EggLink.DanhengServer.Internationalization;
using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.Command.Command.Cmd;

[CommandInfo("grid", "Game.Command.Grid.Desc", "Game.Command.Grid.Usage")]
public class CommandGrid : ICommand
{
    [CommandMethod("role")]
    public async ValueTask AddRole(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var inst = arg.Target.Player!.GridFightManager?.GridFightInstance;
        if (inst == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (arg.BasicArgs.Count < 2)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var roleId = (uint)arg.GetInt(0);
        var tier = (uint)arg.GetInt(1);

        if (!GameData.GridFightRoleStarData.ContainsKey(roleId << 4 | tier))
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.InvalidRole"));
            return;
        }

        await inst.GetComponent<GridFightRoleComponent>().AddAvatar(roleId, tier, src:GridFightSrc.KGridFightSrcNone);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.AddedRole"));
    }

    [CommandMethod("gold")]
    public async ValueTask UpdateGold(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var inst = arg.Target.Player!.GridFightManager?.GridFightInstance;
        if (inst == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (arg.BasicArgs.Count < 1)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var gold = arg.GetInt(0);

        await inst.GetComponent<GridFightBasicComponent>().UpdateGoldNum(gold);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.UpdateGold", gold.ToString()));
    }

    [CommandMethod("equip")]
    public async ValueTask AddEquipment(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var inst = arg.Target.Player!.GridFightManager?.GridFightInstance;
        if (inst == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (arg.BasicArgs.Count < 1)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var equipmentId = (uint)arg.GetInt(0);

        await inst.GetComponent<GridFightItemsComponent>().AddEquipment(equipmentId);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.AddEquipment", equipmentId.ToString()));
    }

    [CommandMethod("orb")]
    public async ValueTask AddOrb(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var inst = arg.Target.Player!.GridFightManager?.GridFightInstance;
        if (inst == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (arg.BasicArgs.Count < 1)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var orbId = (uint)arg.GetInt(0);

        await inst.GetComponent<GridFightOrbComponent>().AddOrb(orbId);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.AddOrb", orbId.ToString()));
    }

    [CommandMethod("consumable")]
    public async ValueTask AddConsumable(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var inst = arg.Target.Player!.GridFightManager?.GridFightInstance;
        if (inst == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (arg.BasicArgs.Count < 1)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var consumableId = (uint)arg.GetInt(0);

        await inst.GetComponent<GridFightItemsComponent>().UpdateConsumable(consumableId, 1);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.AddConsumable", consumableId.ToString()));
    }

    [CommandMethod("section")]
    public async ValueTask SetSection(CommandArg arg)
    {
        if (arg.Target == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        var inst = arg.Target.Player!.GridFightManager?.GridFightInstance;
        if (inst == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.NotInGame"));
            return;
        }

        if (arg.BasicArgs.Count < 2)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var chapterId = (uint)arg.GetInt(0);
        var sectionId = (uint)arg.GetInt(1);

        await inst.GetComponent<GridFightLevelComponent>().EnterSection(chapterId, sectionId, true, GridFightSrc.KGridFightSrcNone);
        await arg.SendMsg(I18NManager.Translate("Game.Command.Grid.EnterSection", chapterId.ToString(), sectionId.ToString()));
    }
}