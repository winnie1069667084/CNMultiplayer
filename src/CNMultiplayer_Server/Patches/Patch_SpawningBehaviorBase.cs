using HarmonyLib;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HarmonyPatches
{
    [HarmonyPatch(typeof(SpawningBehaviorBase), "SpawnBot")]//修改Bot重生规则（仅限攻城模式）
    internal class Patch_SpawnBot
    {
        private const float FemaleAiPossibility = 0.25f;//女性AI比例

        public static bool Prefix(Team agentTeam, BasicCultureObject cultureLimit, SpawnComponent ___SpawnComponent, MissionLobbyComponent ___MissionLobbyComponent)
        {
            BasicCharacterObject troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(cultureLimit).ToMBList<MultiplayerClassDivisions.MPHeroClass>().GetRandomElement<MultiplayerClassDivisions.MPHeroClass>()
            .TroopCharacter;
            if (troopCharacter.IsMounted)//删除骑兵AI
                return false;
            MatrixFrame spawnFrame = ___SpawnComponent.GetSpawnFrame(agentTeam, troopCharacter.HasMount(), true);
            AgentBuildData agentBuildData = new AgentBuildData(troopCharacter).Team(agentTeam).InitialPosition(spawnFrame.origin);
            Vec2 vec = spawnFrame.rotation.f.AsVec2;
            vec = vec.Normalized();
            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).TroopOrigin(new BasicBattleAgentOrigin(troopCharacter)).EquipmentSeed(___MissionLobbyComponent.GetRandomFaceSeedForCharacter(troopCharacter, 0))
                .ClothingColor1((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
                .ClothingColor2((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2)
                .IsFemale(GenerateFemaleAIRandom(FemaleAiPossibility));//AI性别控制
            agentBuildData2.Equipment(Equipment.GetRandomEquipmentElements(troopCharacter, !(Game.Current.GameType is MultiplayerGame), false, agentBuildData2.AgentEquipmentSeed));
            agentBuildData2.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData2.AgentRace, agentBuildData2.AgentIsFemale, troopCharacter.GetBodyPropertiesMin(false), troopCharacter.GetBodyPropertiesMax(), (int)agentBuildData2.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData2.AgentEquipmentSeed, troopCharacter.HairTags, troopCharacter.BeardTags, troopCharacter.TattooTags));
            Agent agent = Mission.Current.SpawnAgent(agentBuildData2, false);
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
            agent.AIStateFlags |= Agent.AIStateFlag.Alarmed;
            agent.AgentDrivenProperties.ArmorHead = mPHeroClassForCharacter.ArmorValue;//为AI添加护甲，与MPClassDivision相匹配
            agent.AgentDrivenProperties.ArmorTorso = mPHeroClassForCharacter.ArmorValue;
            agent.AgentDrivenProperties.ArmorArms = mPHeroClassForCharacter.ArmorValue;
            agent.AgentDrivenProperties.ArmorLegs = mPHeroClassForCharacter.ArmorValue;
            return false;
        }

        private static bool GenerateFemaleAIRandom(float t)
        {
            Random ran = new Random();
            if (ran.NextDouble() < t)
                return true;
            else
                return false;
        }
    }
}
