﻿using CNMultiplayer.Server.Modes.CNMSiege;
using CNMultiplayer.Server.Patches.Behaviors;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using CNMultiplayer.Common.XML;

namespace CNMultiplayer.Server
{
    public class CNMSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Harmony harmony = new Harmony("CNMultiplayer");
            harmony.PatchAll();
            Module.CurrentModule.AddMultiplayerGameMode(new CNMSiegeGameMode());
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            // 修复“有玩家未准备好”bug，by mentalrob
            mission.AddMissionBehavior(new NotAllPlayersJoinFixBehavior());
            // 根据不同的模式切换mpclassdivision，用于实现对原版的兼容
            MBObjectManager.Instance.ClearAllObjectsWithType(typeof(MultiplayerClassDivisions.MPHeroClass));
            new LoadXMLbyMode().ModeJudgment();
        }
    }
}