﻿using CNMultiplayer.Modes.Captain;
using CNMultiplayer.Modes.IndividualDeathMatch;
using CNMultiplayer.Modes.Siege;
using HarmonyLib;
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
            //待学习调整：*地图投票界面、WelcomeMessage、*不同模式加载不同XML
            Module.CurrentModule.AddMultiplayerGameMode(new CNMSiegeGameMode());
            Module.CurrentModule.AddMultiplayerGameMode(new CNMCaptainGameMode());
            Module.CurrentModule.AddMultiplayerGameMode(new IDMGameMode());
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
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
    }
}