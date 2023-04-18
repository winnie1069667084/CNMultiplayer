using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace CNMultiplayer.Common
{
    internal abstract class CNMSpawningBehaviorBase : SpawningBehaviorBase
    {
        private const float FemaleAiPossibility = 0.25f; //女性AI比例

        protected void SpawnBotAgents()
        {
            int botsTeam1 = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
            int botsTeam2 = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
            if (botsTeam1 <= 0 && botsTeam2 <= 0)
                return;

            foreach (Team team in Mission.Teams)
            {
                if (Mission.AttackerTeam != team && Mission.DefenderTeam != team)
                    continue;

                BasicCultureObject teamCulture;
                int numberOfBots;
                BasicCultureObject cultureTeam1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
                BasicCultureObject cultureTeam2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
                if (team.Side == BattleSideEnum.Attacker)
                {
                    teamCulture = cultureTeam1;
                    numberOfBots = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
                }
                else
                {
                    teamCulture = cultureTeam2;
                    numberOfBots = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
                }

                int botsAlive = team.ActiveAgents.Count(a => a.IsAIControlled && a.IsHuman);
                if (botsAlive < numberOfBots)
                {
                    var troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses()
                        .GetRandomElementWithPredicate<MultiplayerClassDivisions.MPHeroClass>(x => !x.TroopCharacter.IsMounted && x.Culture == teamCulture).TroopCharacter; //禁用骑兵AI
                    MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(team, troopCharacter.HasMount(), true);
                    AgentBuildData agentBuildData = new AgentBuildData(troopCharacter).Team(team).InitialPosition(spawnFrame.origin).VisualsIndex(0);
                    Vec2 vec = spawnFrame.rotation.f.AsVec2;
                    vec = vec.Normalized();
                    AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).TroopOrigin(new BasicBattleAgentOrigin(troopCharacter)).EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(troopCharacter, 0))
                        .ClothingColor1((team.Side == BattleSideEnum.Attacker) ? teamCulture.Color : teamCulture.ClothAlternativeColor)
                        .ClothingColor2((team.Side == BattleSideEnum.Attacker) ? teamCulture.Color2 : teamCulture.ClothAlternativeColor2)
                        .IsFemale(GenerateFemaleAIRandom(FemaleAiPossibility)); //AI性别控制
                    agentBuildData2.Equipment(Equipment.GetRandomEquipmentElements(troopCharacter, !(Game.Current.GameType is MultiplayerGame), false, agentBuildData2.AgentEquipmentSeed));
                    agentBuildData2.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData2.AgentRace, agentBuildData2.AgentIsFemale, troopCharacter.GetBodyPropertiesMin(false), troopCharacter.GetBodyPropertiesMax(), (int)agentBuildData2.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData2.AgentEquipmentSeed, troopCharacter.HairTags, troopCharacter.BeardTags, troopCharacter.TattooTags));
                    Agent agent = Mission.SpawnAgent(agentBuildData2, false);
                    MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
                    agent.AIStateFlags |= Agent.AIStateFlag.Alarmed;
                    agent.AgentDrivenProperties.ArmorHead = mPHeroClassForCharacter.ArmorValue; //为AI添加护甲，与MPClassDivision相匹配
                    agent.AgentDrivenProperties.ArmorTorso = mPHeroClassForCharacter.ArmorValue;
                    agent.AgentDrivenProperties.ArmorArms = mPHeroClassForCharacter.ArmorValue;
                    agent.AgentDrivenProperties.ArmorLegs = mPHeroClassForCharacter.ArmorValue;
                }
            }
        }

        private static bool GenerateFemaleAIRandom(float t)
        {
            Random ran = new Random();
            if (ran.NextFloat() < t)
                return true;
            else
                return false;
        }

        public override bool AllowEarlyAgentVisualsDespawning(MissionPeer missionPeer)
        {
            return false;
        }
    }
}