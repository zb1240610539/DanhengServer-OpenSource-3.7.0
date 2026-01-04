using EggLink.DanhengServer.Proto;

namespace EggLink.DanhengServer.GameServer.Game.GridFight.Component;

public abstract class BaseGridFightComponent(GridFightInstance inst)
{
    public GridFightInstance Inst { get; } = inst;
    public abstract GridFightGameInfo ToProto();
}