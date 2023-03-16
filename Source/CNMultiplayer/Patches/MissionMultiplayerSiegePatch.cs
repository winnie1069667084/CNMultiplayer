using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.ObjectSystem;
using TaleWorlds.DotNet;
using System.Reflection;
using NetworkMessages.FromServer;

namespace Patches
{
    internal class OnBehaviorInitializePatch : MissionMultiplayerSiege
    {
        public static bool Prefix(MissionMultiplayerSiege __instance)
        {
            __instance.OnBehaviorInitialize();
            this._objectiveSystem = new MissionMultiplayerSiege.ObjectiveSystem();
            this._childDestructableComponents = new Dictionary<GameEntity, List<DestructableComponent>>();
            this._gameModeSiegeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerSiegeClient>();
            this._warmupComponent = Mission.Current.GetMissionBehavior<MultiplayerWarmupComponent>();
            this._capturePointOwners = new Team[7];
            this._capturePointRemainingMoraleGains = new int[7];
            this._morales = new int[2];
            this._morales[1] = 360;
            this._morales[0] = 360;
            this.AllCapturePoints = new MBReadOnlyList<FlagCapturePoint>(Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>().ToListQ<FlagCapturePoint>());
            foreach (FlagCapturePoint flagCapturePoint in this.AllCapturePoints)
            {
                flagCapturePoint.SetTeamColorsSynched(4284111450U, uint.MaxValue);
                this._capturePointOwners[flagCapturePoint.FlagIndex] = null;
                this._capturePointRemainingMoraleGains[flagCapturePoint.FlagIndex] = 90;
                if (flagCapturePoint.GameEntity.HasTag("keep_capture_point"))
                {
                    this._masterFlag = flagCapturePoint;
                }
            }
            foreach (DestructableComponent destructableComponent in Mission.Current.MissionObjects.FindAllWithType<DestructableComponent>())
            {
                if (destructableComponent.BattleSide != BattleSideEnum.None)
                {
                    GameEntity root = destructableComponent.GameEntity.Root;
                    if (this._objectiveSystem.RegisterObjective(root))
                    {
                        this._childDestructableComponents.Add(root, new List<DestructableComponent>());
                        MissionMultiplayerSiege.GetDestructableCompoenentClosestToTheRoot(root).OnDestroyed += new DestructableComponent.OnHitTakenAndDestroyedDelegate(this.DestructableComponentOnDestroyed);
                    }
                    this._childDestructableComponents[root].Add(destructableComponent);
                    destructableComponent.OnHitTaken += new DestructableComponent.OnHitTakenAndDestroyedDelegate(this.DestructableComponentOnHitTaken);
                }
            }
            List<RangedSiegeWeapon> list = new List<RangedSiegeWeapon>();
            List<IMoveableSiegeWeapon> list2 = new List<IMoveableSiegeWeapon>();
            foreach (UsableMachine usableMachine in Mission.Current.MissionObjects.FindAllWithType<UsableMachine>())
            {
                RangedSiegeWeapon rangedSiegeWeapon = usableMachine as RangedSiegeWeapon;
                if (rangedSiegeWeapon != null)
                {
                    list.Add(rangedSiegeWeapon);
                    rangedSiegeWeapon.OnAgentLoadsMachine += this.RangedSiegeMachineOnAgentLoadsMachine;
                }
                else
                {
                    IMoveableSiegeWeapon moveableSiegeWeapon = usableMachine as IMoveableSiegeWeapon;
                    if (moveableSiegeWeapon != null)
                    {
                        list2.Add(moveableSiegeWeapon);
                        this._objectiveSystem.RegisterObjective(usableMachine.GameEntity.Root);
                    }
                }
            }
            this._lastReloadingAgentPerRangedSiegeMachine = new ValueTuple<RangedSiegeWeapon, Agent>[list.Count];
            for (int i = 0; i < this._lastReloadingAgentPerRangedSiegeMachine.Length; i++)
            {
                this._lastReloadingAgentPerRangedSiegeMachine[i] = ValueTuple.Create<RangedSiegeWeapon, Agent>(list[i], null);
            }
            this._movingObjectives = new ValueTuple<IMoveableSiegeWeapon, Vec3>[list2.Count];
            for (int j = 0; j < this._movingObjectives.Length; j++)
            {
                SiegeWeapon siegeWeapon = list2[j] as SiegeWeapon;
                this._movingObjectives[j] = ValueTuple.Create<IMoveableSiegeWeapon, Vec3>(list2[j], siegeWeapon.GameEntity.GlobalPosition);
            }
        }
            return false;
        }
    }
}
