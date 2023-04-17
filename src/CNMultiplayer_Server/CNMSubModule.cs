using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using HarmonyLib;
using ChatCommands;
using Newtonsoft.Json;
using System.IO;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using CNMultiplayer_Server.Modes.Siege;
using CNMultiplayer_Server.Common;

namespace CNMultiplayer_Server
{
    public class CNMSubModule : MBSubModuleBase
    {
        private void Setup()
        {
            string basePath = Environment.CurrentDirectory + "/../../Modules/Native";
            string configPath = Path.Combine(basePath, "chatCommands.json");
            if (!File.Exists(configPath))
            {
                Config config = new Config{ AdminPassword = ChatCommands.Helpers.RandomString(6) };
                ConfigManager.SetConfig(config);
                string json = JsonConvert.SerializeObject(config);
                File.WriteAllText(configPath, json);
            }
            else
            {
                string configString = File.ReadAllText(configPath);
                Config config = JsonConvert.DeserializeObject<Config>(configString);
                ConfigManager.SetConfig(config);
            }
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Harmony harmony = new Harmony("CNMultiplayer");
            harmony.PatchAll();
            this.Setup();
            Debug.Print("** CHAT COMMANDS BY MENTALROB LOADED **", 0, Debug.DebugColor.Green);
            _ = new ChatCommands.CommandManager();
            Module.CurrentModule.AddMultiplayerGameMode(new CNMSiegeGameMode());
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            mission.AddMissionBehavior(new NotAllPlayersJoinFixBehavior());
            MBObjectManager.Instance.ClearAllObjectsWithType(typeof(MultiplayerClassDivisions.MPHeroClass)); //目前只实现了根据游戏模式加载MPClassDivisions
            new LoadXMLbyMode().ModeJudgment();
        }

        public override void OnMultiplayerGameStart(Game game, object starterObject)
        {
            Debug.Print("** CHAT HANDLER ADDED **", 0, Debug.DebugColor.Green);
            game.AddGameHandler<ChatHandler>();
            // game.AddGameHandler<ManipulatedChatBox>();

        }
        public override void OnGameEnd(Game game)
        {
            game.RemoveGameHandler<ChatHandler>();
        }
    }
}