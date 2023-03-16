using HarmonyLib;
using NetworkMessages.FromServer;
using Patches;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ServerPatches
{
    [HarmonyPatch(typeof(MissionNetworkComponent), "SendSpawnedMissionObjectsToPeer")]//ServerPatch @HornsGuy
    public class PatchMissionNetworkComponent
    {
        static bool hitOnce = false;
        public static bool Prefix(MissionNetworkComponent __instance, NetworkCommunicator networkPeer)
        {
            if (!hitOnce)
            {
                Logging.Instance.Info("PatchMissionNetworkComponent.Prefix has been hit once");
                hitOnce = true;
            }

            if (networkPeer != null)
            {
                using (IEnumerator<MissionObject> enumerator = __instance.Mission.MissionObjects.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        MissionObject missionObject = enumerator.Current;
                        SpawnedItemEntity spawnedItemEntity;
                        if ((spawnedItemEntity = (missionObject as SpawnedItemEntity)) != null)
                        {
                            GameEntity gameEntity = spawnedItemEntity.GameEntity;
                            if (gameEntity.Parent == null || !gameEntity.Parent.HasScriptOfType<SpawnedItemEntity>())
                            {
                                MissionObject missionObject2 = null;
                                if (spawnedItemEntity.GameEntity.Parent != null)
                                {
                                    missionObject2 = gameEntity.Parent.GetFirstScriptOfType<MissionObject>();
                                }
                                MatrixFrame matrixFrame = gameEntity.GetGlobalFrame();
                                if (missionObject2 != null)
                                {
                                    matrixFrame = missionObject2.GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(ref matrixFrame);
                                }
                                matrixFrame.origin.z = MathF.Max(matrixFrame.origin.z, CompressionBasic.PositionCompressionInfo.GetMinimumValue() + 1f);
                                Mission.WeaponSpawnFlags weaponSpawnFlags = spawnedItemEntity.SpawnFlags;
                                if (weaponSpawnFlags.HasAnyFlag(Mission.WeaponSpawnFlags.WithPhysics) && !gameEntity.GetPhysicsState())
                                {
                                    weaponSpawnFlags = ((weaponSpawnFlags & ~Mission.WeaponSpawnFlags.WithPhysics) | Mission.WeaponSpawnFlags.WithStaticPhysics);
                                }
                                bool hasLifeTime = true;
                                bool isVisible = gameEntity.Parent == null || missionObject2 != null;
                                GameNetwork.BeginModuleEventAsServer(networkPeer);
                                GameNetwork.WriteMessage(new SpawnWeaponWithNewEntity(spawnedItemEntity.WeaponCopy, weaponSpawnFlags, spawnedItemEntity.Id.Id, matrixFrame, missionObject2, isVisible, hasLifeTime));
                                GameNetwork.EndModuleEventAsServer();
                                for (int i = 0; i < spawnedItemEntity.WeaponCopy.GetAttachedWeaponsCount(); i++)
                                {
                                    GameNetwork.BeginModuleEventAsServer(networkPeer);
                                    GameNetwork.WriteMessage(new AttachWeaponToSpawnedWeapon(spawnedItemEntity.WeaponCopy.GetAttachedWeapon(i), spawnedItemEntity, spawnedItemEntity.WeaponCopy.GetAttachedWeaponFrame(i)));
                                    GameNetwork.EndModuleEventAsServer();

                                    // Whole load of null checks to see what the issue is
                                    if (spawnedItemEntity.WeaponCopy.GetAttachedWeapon(i).Item.ItemFlags.HasAnyFlag(ItemFlags.CanBePickedUpFromCorpse))
                                    {
                                        if (gameEntity.GetChild(i) != null)
                                        {
                                            if (gameEntity.GetChild(i).GetFirstScriptOfType<SpawnedItemEntity>() != null)
                                            {
                                                if (gameEntity.GetChild(i).GetFirstScriptOfType<SpawnedItemEntity>().Id != null)
                                                {
                                                    // OG Code Begin
                                                    GameNetwork.BeginModuleEventAsServer(networkPeer);
                                                    GameNetwork.WriteMessage(new SpawnAttachedWeaponOnSpawnedWeapon(spawnedItemEntity, i, gameEntity.GetChild(i).GetFirstScriptOfType<SpawnedItemEntity>().Id.Id));
                                                    GameNetwork.EndModuleEventAsServer();
                                                    // OG Code End
                                                }
                                                else
                                                {
                                                    Logging.Instance.Error("gameEntity.GetChild(i).GetFirstScriptOfType<SpawnedItemEntity>().Id was null in PatchMissionNetworkComponent");
                                                }
                                            }
                                            else
                                            {
                                                Logging.Instance.Error("gameEntity.GetChild(i).GetFirstScriptOfType<SpawnedItemEntity>() was null in PatchMissionNetworkComponent");
                                            }
                                        }
                                        else
                                        {
                                            Logging.Instance.Error("gameEntity.GetChild(i) was null in PatchMissionNetworkComponent");
                                        }

                                    }
                                }
                            }
                        }

                        else if (missionObject.CreatedAtRuntime)
                        {
                            Mission.DynamicallyCreatedEntity dynamicallyCreatedEntity = __instance.Mission.AddedEntitiesInfo.SingleOrDefault((Mission.DynamicallyCreatedEntity x) => x.ObjectId == missionObject.Id);
                            if (dynamicallyCreatedEntity != null)
                            {
                                GameNetwork.BeginModuleEventAsServer(networkPeer);
                                GameNetwork.WriteMessage(new CreateMissionObject(dynamicallyCreatedEntity.ObjectId, dynamicallyCreatedEntity.Prefab, dynamicallyCreatedEntity.Frame, dynamicallyCreatedEntity.ChildObjectIds));
                                GameNetwork.EndModuleEventAsServer();
                            }
                        }
                    }
                }
            }
            else
            {
                Logging.Instance.Error("PatchMissionNetworkComponent: networkPeer was null");
            }
            return false;
        }


    }
}
