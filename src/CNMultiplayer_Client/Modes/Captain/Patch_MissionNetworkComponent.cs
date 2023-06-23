using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace HarmonyPatches
{
    internal class Patch_MissionNetworkComponent
    {
        [HarmonyPatch(typeof(MissionNetworkComponent), "HandleServerEventCreateAgent")]
        internal class Patch_HandleServerEventCreateAgent
        {
            public static bool Prefix(Mission __instance, GameNetworkMessage baseMessage)
            {
                CreateAgent createAgent = (CreateAgent)baseMessage;
                BasicCharacterObject character = createAgent.Character;
                NetworkCommunicator peer = createAgent.Peer;
                MissionPeer missionPeer = ((peer != null) ? peer.GetComponent<MissionPeer>() : null);
                AgentBuildData agentBuildData = new AgentBuildData(character).MissionPeer(createAgent.IsPlayerAgent ? missionPeer : null).Monster(createAgent.Monster).TroopOrigin(new BasicBattleAgentOrigin(character))
                .Equipment(createAgent.SpawnEquipment)
                .EquipmentSeed(createAgent.BodyPropertiesSeed);
                Vec3 position = createAgent.Position;
                AgentBuildData agentBuildData2 = agentBuildData.InitialPosition(position);
                Vec2 vec = createAgent.Direction;
                vec = vec.Normalized();
                AgentBuildData agentBuildData3 = agentBuildData2.InitialDirection(vec).MissionEquipment(createAgent.SpawnMissionEquipment).Team(createAgent.Team)
                .Index(createAgent.AgentIndex)
                .MountIndex(createAgent.MountAgentIndex)
                .IsFemale(createAgent.IsFemale)
                .ClothingColor1(createAgent.ClothingColor1)
                .ClothingColor2(createAgent.ClothingColor2);
                Formation formation = null;

                // Test
                int maxedFormationIndex = Math.Min((int)FormationClass.NumberOfAllFormations, createAgent.FormationIndex);
                if (createAgent.Team != null && createAgent.FormationIndex >= 0 && !GameNetwork.IsReplay)
                {
                    formation = createAgent.Team.GetFormation((FormationClass)maxedFormationIndex);
                    agentBuildData3.Formation(formation);
                }

                if (createAgent.IsPlayerAgent)
                {
                    agentBuildData3.BodyProperties(missionPeer.Peer.BodyProperties);
                    agentBuildData3.Age((int)agentBuildData3.AgentBodyProperties.Age);
                }
                else
                {
                    agentBuildData3.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData3.AgentRace, agentBuildData3.AgentIsFemale, character.GetBodyPropertiesMin(false), character.GetBodyPropertiesMax(), (int)agentBuildData3.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData3.AgentEquipmentSeed, character.HairTags, character.BeardTags, character.TattooTags));
                }

                Banner banner = null;
                if (formation != null)
                {
                    if (!string.IsNullOrEmpty(formation.BannerCode))
                    {
                        if (formation.Banner == null)
                        {
                            banner = new Banner(formation.BannerCode, createAgent.Team.Color, createAgent.Team.Color2);
                            formation.Banner = banner;
                        }
                        else
                        {
                            banner = formation.Banner;
                        }
                    }
                }
                else if (missionPeer != null)
                {
                    banner = new Banner(missionPeer.Peer.BannerCode, createAgent.Team.Color, createAgent.Team.Color2);
                }
                agentBuildData3.Banner(banner);

                Agent mountAgent = Mission.Current.SpawnAgent(agentBuildData3, false).MountAgent;

                return false;
            }
        }
    }
}
