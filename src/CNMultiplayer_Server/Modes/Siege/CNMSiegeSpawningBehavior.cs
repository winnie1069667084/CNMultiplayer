using CNMultiplayer.Common;
using NetworkMessages.FromServer;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer.Modes.Siege
{
    internal class CNMSiegeSpawningBehavior : CNMSpawningBehaviorBase
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
            if (IsSpawningEnabled && _spawnCheckTimer.Check(Mission.CurrentTime))
            {
                SpawnAgents();
                SpawnBotAgents();
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
                    .BodyProperties(GetBodyProperties(component, (component.Team == Mission.AttackerTeam) ? @object : object2))
                    .VisualsIndex(0)
                    .ClothingColor1((component.Team == Mission.AttackerTeam) ? basicCultureObject.Color : basicCultureObject.ClothAlternativeColor)
                    .ClothingColor2((component.Team == Mission.AttackerTeam) ? basicCultureObject.Color2 : basicCultureObject.ClothAlternativeColor2);
                if (this.GameMode.ShouldSpawnVisualsForServer(networkPeer) && agentBuildData.AgentVisualsIndex == 0)
                {
                    component.HasSpawnedAgentVisuals = true;
                    component.EquipmentUpdatingExpired = false;
                }

                GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData);
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
            bool flag = peer.Team == Mission.AttackerTeam;
            MultiplayerClassDivisions.MPHeroClass mPHeroClass = MultiplayerClassDivisions.GetMPHeroClasses(MBObjectManager.Instance.GetObject<BasicCultureObject>(flag ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue())).ElementAt(peer.SelectedTroopIndex);
            GameMode.ChangeCurrentGoldForPeer(peer, GameMode.GetCurrentGoldForPeer(peer) - mPHeroClass.TroopCasualCost);
        }

        public static bool LockTroop(MissionPeer component, MultiplayerClassDivisions.MPHeroClass mpheroClassForPeer) //锁定兵种比例上限
        {
            bool flag = false;
            int Sum = GetTroopTypeCountForTeam(component.Team)[0];
            //int Infantry = GetTroopTypeCountForTeam(component.Team)[1];
            int Ranged = GetTroopTypeCountForTeam(component.Team)[2];
            int Cavalry = GetTroopTypeCountForTeam(component.Team)[3];
            int HorseArcher = GetTroopTypeCountForTeam(component.Team)[4];
            BasicCharacterObject Character = mpheroClassForPeer.TroopCharacter;
            if ((Character.IsRanged && !Character.IsMounted && Ranged > Sum * 0.3) || (Character.IsMounted && !Character.IsRanged && Cavalry > Sum * 0.2) || (Character.IsMounted && Character.IsRanged && HorseArcher > Sum * 0.1))
                flag = true;
            return flag;
        }

        public static int[] GetTroopTypeCountForTeam(Team team) //统计某方战场存活兵种数，0总数；1步兵；2射手；3骑兵；4骑射
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
