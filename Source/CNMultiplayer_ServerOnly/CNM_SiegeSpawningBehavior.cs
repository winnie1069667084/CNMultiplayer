using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer
{
    public class CNM_SiegeSpawningBehavior : SiegeSpawningBehavior
    {
        protected override void SpawnAgents()
        {
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                if (networkCommunicator.IsSynchronized)
                {
                    MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
                    if (component != null && component.ControlledAgent == null && !component.HasSpawnedAgentVisuals && component.Team != null && component.Team != base.Mission.SpectatorTeam && component.TeamInitialPerkInfoReady && component.SpawnTimer.Check(base.Mission.CurrentTime))
                    {
                        MultiplayerClassDivisions.MPHeroClass mpheroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component, false);
                        if (mpheroClassForPeer == null || (mpheroClassForPeer.TroopCasualCost > this.GameMode.GetCurrentGoldForPeer(component) &&  LockTroop(component, mpheroClassForPeer)))
                        {
                            if (component.SelectedTroopIndex != 0)
                            {
                                component.SelectedTroopIndex = 0;
                                GameNetwork.BeginBroadcastModuleEvent();
                                GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkCommunicator, 0));
                                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkCommunicator);
                            }
                            continue;
                        }
                    }
                }
            }
            base.SpawnAgents();
        }

        public static bool LockTroop(MissionPeer component, MultiplayerClassDivisions.MPHeroClass mpheroClassForPeer)
        {
            bool flag = true;
            int Sum = GetTroopTypeCountForTeam(component.Team)[0];
            int Infantry = GetTroopTypeCountForTeam(component.Team)[1];
            int Ranged = GetTroopTypeCountForTeam(component.Team)[2];
            int Cavalry = GetTroopTypeCountForTeam(component.Team)[3];
            int HorseArcher = GetTroopTypeCountForTeam(component.Team)[4];
            BasicCharacterObject Character = mpheroClassForPeer.TroopCharacter;
            if ((Character.IsRanged && !Character.IsMounted && Ranged > Sum / 4) || (Character.IsMounted && !Character.IsRanged && Cavalry > Sum / 4) || (Character.IsMounted && Character.IsRanged && HorseArcher > Sum / 4))
                flag = false;
            return flag;
        }


        public static int[] GetTroopTypeCountForTeam(Team team)//统计某方战场存活兵种数，0总数；1步兵；2射手；3骑兵；4骑射
        {
            int[] num = new int[5];
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component?.Team != null && component.Team == team && component.IsControlledAgentActive)
                {
                    BasicCharacterObject Character = MultiplayerClassDivisions.GetMPHeroClassForPeer(component).HeroCharacter;
                    num[0]++;
                    if (Character.IsInfantry)
                    {
                        num[1]++;
                        continue;
                    }
                    if (Character.IsRanged && !Character.IsMounted)
                        num[2]++;
                    if (Character.IsMounted && !Character.IsRanged)
                        num[3]++;
                    if (Character.IsRanged && Character.IsMounted)
                        num[4]++;
                }
            }
            return num;
        }
    }
}
