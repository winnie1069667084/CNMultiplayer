using HarmonyLib;
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using Patches;
using ChatCommands;
using Newtonsoft.Json;
using System.IO;
using TaleWorlds.Core;

namespace CNMultiplayer
{
    public class CNMSubModule : MBSubModuleBase
    {
        private void setup()
        {
            string basePath = Environment.CurrentDirectory + "/../../Modules/Native";
            string configPath = Path.Combine(basePath, "chatCommands.json");
            if (!File.Exists(configPath))
            {
                Config config = new Config();
                config.AdminPassword = Helpers.RandomString(6);
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
            this.setup();
            Debug.Print("** CHAT COMMANDS BY MENTALROB LOADED **", 0, Debug.DebugColor.Green);
            ChatCommands.CommandManager cm = new ChatCommands.CommandManager();
        }
        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            mission.AddMissionBehavior(new NotAllPlayersJoinFixBehavior());
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