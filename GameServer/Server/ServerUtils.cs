using System.Reflection;
using EggLink.DanhengServer.Enums.Rogue;
using EggLink.DanhengServer.GameServer.Game.ChessRogue;
using EggLink.DanhengServer.GameServer.Game.ChessRogue.Modifier.ModifierEffect;
using EggLink.DanhengServer.GameServer.Game.Lobby;
using EggLink.DanhengServer.GameServer.Game.Mission;
using EggLink.DanhengServer.GameServer.Game.Mission.FinishAction;
using EggLink.DanhengServer.GameServer.Game.Mission.FinishType;
using EggLink.DanhengServer.GameServer.Game.MultiPlayer;
using EggLink.DanhengServer.GameServer.Game.Rogue.Event;

namespace EggLink.DanhengServer.GameServer.Server;

public static class ServerUtils
{
    public static LobbyServerManager LobbyServerManager { get; set; } = new();
    public static MultiPlayerGameServerManager MultiPlayerGameServerManager { get; set; } = new();

    public static void InitializeHandlers()
    {
        // mission handlers
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<MissionFinishActionAttribute>();
                if (attr != null)
                {
                    var handler = (MissionFinishActionHandler)Activator.CreateInstance(type, null)!;
                    MissionManager.ActionHandlers.Add(attr.FinishAction, handler);
                }

                var attr2 = type.GetCustomAttribute<MissionFinishTypeAttribute>();
                if (attr2 != null)
                {
                    var handler = (MissionFinishTypeHandler)Activator.CreateInstance(type, null)!;
                    MissionManager.FinishTypeHandlers.Add(attr2.FinishType, handler);
                }
            }
        }

        // chess rogue modifier handlers
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<ModifierEffectAttribute>();
                if (attr == null) continue;

                var handler = (ModifierEffectHandler)Activator.CreateInstance(type, null)!;
                ChessRogueInstance.ModifierEffectHandlers.Add(attr.EffectType, handler);
            }
        }
    }
}