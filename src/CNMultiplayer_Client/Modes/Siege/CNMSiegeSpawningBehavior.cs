using NetworkMessages.FromServer;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer_Client.Modes.Siege
{
    public class CNMSiegeSpawningBehavior : SpawningBehaviorBase
    {
        public override void Initialize(SpawnComponent spawnComponent)
        {
            base.Initialize(spawnComponent);
            base.OnAllAgentsFromPeerSpawnedFromVisuals += OnAllAgentsFromPeerSpawnedFromVisuals;
        }

        public override void Clear()
        {
            base.Clear();
            base.OnAllAgentsFromPeerSpawnedFromVisuals -= OnAllAgentsFromPeerSpawnedFromVisuals;
        }

        public override void OnTick(float dt)
        {
            if (IsSpawningEnabled && _spawnCheckTimer.Check(base.Mission.CurrentTime))
            {
                SpawnAgents();
            }
            base.OnTick(dt);
        }

        protected override void SpawnAgents()
        {
            BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                if (!networkPeer.IsSynchronized)
                {
                    continue;
                }

                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component == null || component.ControlledAgent != null || component.HasSpawnedAgentVisuals || component.Team == null || component.Team == base.Mission.SpectatorTeam || !component.TeamInitialPerkInfoReady || !component.SpawnTimer.Check(base.Mission.CurrentTime))
                {
                    continue;
                }

                BasicCultureObject basicCultureObject = ((component.Team.Side == BattleSideEnum.Attacker) ? @object : object2);
                MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
                if (mPHeroClassForPeer == null || mPHeroClassForPeer.TroopCasualCost > GameMode.GetCurrentGoldForPeer(component) || LockTroop(component, mPHeroClassForPeer))
                {
                    if (component.SelectedTroopIndex != 0)
                    {
                        component.SelectedTroopIndex = 0;
                        GameNetwork.BeginBroadcastModuleEvent();
                        GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeer, 0));
                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);
                    }
                    continue;
                }

                BasicCharacterObject heroCharacter = mPHeroClassForPeer.HeroCharacter;
                Equipment equipment = heroCharacter.Equipment.Clone();
                IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = MPPerkObject.GetOnSpawnPerkHandler(component)?.GetAlternativeEquipments(isPlayer: true);
                if (enumerable != null)
                {
                    foreach (var item in enumerable)
                    {
                        equipment[item.Item1] = item.Item2;
                    }
                }

                AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).MissionPeer(component).Equipment(equipment).Team(component.Team)
                    .TroopOrigin(new BasicBattleAgentOrigin(heroCharacter))
                    .IsFemale(component.Peer.IsFemale)
                    .BodyProperties(GetBodyProperties(component, (component.Team == base.Mission.AttackerTeam) ? @object : object2))
                    .VisualsIndex(0)
                    .ClothingColor1((component.Team == base.Mission.AttackerTeam) ? basicCultureObject.Color : basicCultureObject.ClothAlternativeColor)
                    .ClothingColor2((component.Team == base.Mission.AttackerTeam) ? basicCultureObject.Color2 : basicCultureObject.ClothAlternativeColor2);
                if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
                {
                    base.AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(component, agentBuildData, component.SelectedTroopIndex);
                }

                GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData);
            }

            if (base.Mission.AttackerTeam != null)
            {
                int num = 0;
                foreach (Agent activeAgent in base.Mission.AttackerTeam.ActiveAgents)
                {
                    if (activeAgent.Character != null && activeAgent.MissionPeer == null)
                    {
                        num++;
                    }
                }

                if (num < MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue())
                {
                    SpawnBot(base.Mission.AttackerTeam, @object);
                }
            }

            if (base.Mission.DefenderTeam == null)
            {
                return;
            }

            int num2 = 0;
            foreach (Agent activeAgent2 in base.Mission.DefenderTeam.ActiveAgents)
            {
                if (activeAgent2.Character != null && activeAgent2.MissionPeer == null)
                {
                    num2++;
                }
            }

            if (num2 < MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue())
            {
                SpawnBot(base.Mission.DefenderTeam, object2);
            }
        }

        public override bool AllowEarlyAgentVisualsDespawning(MissionPeer lobbyPeer)
        {
            return true;
        }

        public override int GetMaximumReSpawnPeriodForPeer(MissionPeer peer)
        {
            if (GameMode.WarmupComponent != null && GameMode.WarmupComponent.IsInWarmup)
            {
                return 3;
            }

            if (peer.Team != null)
            {
                if (peer.Team.Side == BattleSideEnum.Attacker)
                {
                    return MultiplayerOptions.OptionType.RespawnPeriodTeam1.GetIntValue();
                }

                if (peer.Team.Side == BattleSideEnum.Defender)
                {
                    return MultiplayerOptions.OptionType.RespawnPeriodTeam2.GetIntValue();
                }
            }

            return -1;
        }

        protected override bool IsRoundInProgress()
        {
            return Mission.Current.CurrentState == Mission.State.Continuing;
        }

        private new void OnAllAgentsFromPeerSpawnedFromVisuals(MissionPeer peer)
        {
            bool flag = peer.Team == base.Mission.AttackerTeam;
            _ = base.Mission.DefenderTeam;
            MultiplayerClassDivisions.MPHeroClass mPHeroClass = MultiplayerClassDivisions.GetMPHeroClasses(MBObjectManager.Instance.GetObject<BasicCultureObject>(flag ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue())).ElementAt(peer.SelectedTroopIndex);
            GameMode.ChangeCurrentGoldForPeer(peer, GameMode.GetCurrentGoldForPeer(peer) - mPHeroClass.TroopCasualCost);
        }

        public static bool LockTroop(MissionPeer component, MultiplayerClassDivisions.MPHeroClass mpheroClassForPeer)
        {
            bool flag = false;
            int Sum = GetTroopTypeCountForTeam(component.Team)[0];
            int Infantry = GetTroopTypeCountForTeam(component.Team)[1];
            int Ranged = GetTroopTypeCountForTeam(component.Team)[2];
            int Cavalry = GetTroopTypeCountForTeam(component.Team)[3];
            int HorseArcher = GetTroopTypeCountForTeam(component.Team)[4];
            BasicCharacterObject Character = mpheroClassForPeer.TroopCharacter;
            if ((Character.IsRanged && !Character.IsMounted && Ranged > Sum / 4) || (Character.IsMounted && !Character.IsRanged && Cavalry > Sum / 4) || (Character.IsMounted && Character.IsRanged && HorseArcher > Sum / 4))
                flag = true;
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
