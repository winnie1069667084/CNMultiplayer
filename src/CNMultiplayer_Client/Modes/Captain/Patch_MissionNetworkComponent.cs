using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HarmonyPatches
{
    internal class Patch_MissionNetworkComponent
    {
        [HarmonyPatch(typeof(MissionNetworkComponent), "HandleServerEventCreateAgent")]
        internal class Patch_HandleServerEventCreateAgent
        {
            public static bool Prefix(Mission __instance, CreateAgent message)
            {
                BasicCharacterObject character = message.Character;
                NetworkCommunicator peer = message.Peer;
                MissionPeer missionPeer = ((peer != null) ? peer.GetComponent<MissionPeer>() : null);
                AgentBuildData agentBuildData = new AgentBuildData(character).MissionPeer(message.IsPlayerAgent ? missionPeer : null).Monster(message.Monster).TroopOrigin(new BasicBattleAgentOrigin(character))
                .Equipment(message.SpawnEquipment)
                    .EquipmentSeed(message.BodyPropertiesSeed);
                Vec3 position = message.Position;
                AgentBuildData agentBuildData2 = agentBuildData.InitialPosition(position);
                Vec2 vec = message.Direction;
                vec = vec.Normalized();
                AgentBuildData agentBuildData3 = agentBuildData2.InitialDirection(vec).MissionEquipment(message.SpawnMissionEquipment).Team(message.Team)
                .Index(message.AgentIndex)
                .MountIndex(message.MountAgentIndex)
                .IsFemale(message.IsFemale)
                    .ClothingColor1(message.ClothingColor1)
                    .ClothingColor2(message.ClothingColor2);
                Formation formation = null;

                // Test
                int maxedFormationIndex = Math.Min((int)FormationClass.NumberOfAllFormations, message.FormationIndex);
                if (message.Team != null && message.FormationIndex >= 0 && !GameNetwork.IsReplay)
                {
                    formation = message.Team.GetFormation((FormationClass)maxedFormationIndex);
                    agentBuildData3.Formation(formation);
                }

                if (message.IsPlayerAgent)
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
                            banner = new Banner(formation.BannerCode, message.Team.Color, message.Team.Color2);
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
                    banner = new Banner(missionPeer.Peer.BannerCode, message.Team.Color, message.Team.Color2);
                }
                agentBuildData3.Banner(banner);

                Agent mountAgent = Mission.Current.SpawnAgent(agentBuildData3, false).MountAgent;

                return false;
            }
        }
    }
}
