using ChatCommands;
using CNMultiplayer.Common;
using CNMultiplayer.Modes.Captain;
using CNMultiplayer.Modes.Siege;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.IO;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer
{
    public class CNMSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Harmony harmony = new Harmony("CNMultiplayer");
            harmony.PatchAll();
            this.Setup();
            _ = new ChatCommands.CommandManager();
            Module.CurrentModule.AddMultiplayerGameMode(new CNMSiegeGameMode());
            Module.CurrentModule.AddMultiplayerGameMode(new CNMCaptainGameMode());
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            mission.AddMissionBehavior(new NotAllPlayersJoinFixBehavior());
            MBObjectManager.Instance.ClearAllObjectsWithType(typeof(MultiplayerClassDivisions.MPHeroClass)); //目前只实现了根据游戏模式加载MPClassDivisions
            new LoadXMLbyMode().ModeJudgment();
            if (MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) == "CNMCaptain")
            {
                CompressionOrder.FormationClassCompressionInfo = new CompressionInfo.Integer(-1, 100, true);
            }
            else
            {
                CompressionOrder.FormationClassCompressionInfo = new CompressionInfo.Integer(-1, 10, true);
            }
        }

        public override void OnMultiplayerGameStart(Game game, object starterObject)
        {
            Debug.Print("** CHAT HANDLER ADDED **", 0, Debug.DebugColor.Green);
            game.AddGameHandler<ChatHandler>();
        }

        public override void OnGameEnd(Game game)
        {
            game.RemoveGameHandler<ChatHandler>();
        }

        private void Setup()
        {
            string basePath = Environment.CurrentDirectory + "/../../Modules/Native";
            string configPath = Path.Combine(basePath, "chatCommands.json");
            if (!File.Exists(configPath))
            {
                Config config = new Config { AdminPassword = ChatCommands.Helpers.RandomString(6) };
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
    }
}