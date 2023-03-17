using Messages.FromClient.ToLobbyServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using HarmonyLib;
using Mono.Cecil.Cil;

namespace Patches
{
    [HarmonyPatch(typeof(AgentStatCalculateModel), "SetAiRelatedProperties")]//强化近战AI（仅限攻城模式）
    internal class Patch_SetAiRelatedProperties
    {
        public static bool Prefix(Agent agent, ref AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedItem, WeaponComponentData secondaryItem, AgentStatCalculateModel __instance)
        {
            if (MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) != "Siege")
            { return true; }
            MethodInfo method = typeof(AgentStatCalculateModel).GetMethod("GetMeleeSkill", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo method1 = typeof(AgentStatCalculateModel).GetMethod("CalculateAILevel", BindingFlags.Instance | BindingFlags.NonPublic);
            SkillObject skill = (equippedItem == null) ? DefaultSkills.Athletics : equippedItem.RelevantSkill;
            int effectiveSkill = __instance.GetEffectiveSkill(agent.Character, agent.Origin, agent.Formation, skill);
            int meleeSkill = (int)method.Invoke(__instance, new object[] { agent, equippedItem, secondaryItem });
            float num = (float)method1.Invoke(__instance, new object[] { agent, meleeSkill });
            float num2 = (float)method1.Invoke(__instance, new object[] { agent, effectiveSkill });
            float defensiveness = agent.Defensiveness;
            agentDrivenProperties.AiRangedHorsebackMissileRange = 0.3f + 0.4f * num2;
            agentDrivenProperties.AiFacingMissileWatch = -0.96f + num * 0.06f;
            agentDrivenProperties.AiFlyingMissileCheckRadius = 8f - 6f * num;
            agentDrivenProperties.AiShootFreq = 0.3f + 0.7f * num2;
            agentDrivenProperties.AiWaitBeforeShootFactor = (agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0f : (1f - 0.5f * num2));
            agentDrivenProperties.AIBlockOnDecideAbility = 1f;
            agentDrivenProperties.AIParryOnDecideAbility = 1f;
            agentDrivenProperties.AiTryChamberAttackOnDecide = 1f;
            agentDrivenProperties.AIAttackOnParryChance = 1f;
            agentDrivenProperties.AiAttackOnParryTiming = 0f;
            agentDrivenProperties.AIDecideOnAttackChance = 1f;
            agentDrivenProperties.AIParryOnAttackAbility = 1f;
            agentDrivenProperties.AiKick = 1f;
            agentDrivenProperties.AiAttackCalculationMaxTimeFactor = 0f;
            agentDrivenProperties.AiDecideOnAttackWhenReceiveHitTiming = 0f;
            agentDrivenProperties.AiDecideOnAttackContinueAction = 1f;
            agentDrivenProperties.AiDecideOnAttackingContinue = 1f;
            agentDrivenProperties.AIParryOnAttackingContinueAbility = 1f;
            agentDrivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility = 0.33f;
            agentDrivenProperties.AIRealizeBlockingFromIncorrectSideAbility = 1f;
            agentDrivenProperties.AiAttackingShieldDefenseChance = 1f;
            agentDrivenProperties.AiAttackingShieldDefenseTimer = 0f;
            agentDrivenProperties.AiRandomizedDefendDirectionChance = 0f;
            agentDrivenProperties.AiShooterError = 0.001f;
            agentDrivenProperties.AISetNoAttackTimerAfterBeingHitAbility = 0f;
            agentDrivenProperties.AISetNoAttackTimerAfterBeingParriedAbility = 0f;
            agentDrivenProperties.AISetNoDefendTimerAfterHittingAbility = 0f;
            agentDrivenProperties.AISetNoDefendTimerAfterParryingAbility = 0f;
            agentDrivenProperties.AIEstimateStunDurationPrecision = 1f;
            agentDrivenProperties.AIHoldingReadyMaxDuration = 0.5f;
            agentDrivenProperties.AIHoldingReadyVariationPercentage = 0.3f;
            agentDrivenProperties.AiRaiseShieldDelayTimeBase = 0f;
            agentDrivenProperties.AiUseShieldAgainstEnemyMissileProbability = 1f;
            agentDrivenProperties.AiCheckMovementIntervalFactor = 0f;
            agentDrivenProperties.AiMovementDelayFactor = 0f;
            agentDrivenProperties.AiParryDecisionChangeValue = 1f;
            agentDrivenProperties.AiDefendWithShieldDecisionChanceValue = 1f;
            agentDrivenProperties.AiMoveEnemySideTimeValue = 0f;
            agentDrivenProperties.AiMinimumDistanceToContinueFactor = 0.75f;
            agentDrivenProperties.AiHearingDistanceFactor = 0.001f;
            agentDrivenProperties.AiChargeHorsebackTargetDistFactor = 1.5f;
            agentDrivenProperties.AiWaitBeforeShootFactor = (agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0f : (1f - 0.5f * num2));
            float num3 = 1f - num2;
            agentDrivenProperties.AiRangerLeadErrorMin = -num3 * 0.35f;
            agentDrivenProperties.AiRangerLeadErrorMax = num3 * 0.2f;
            agentDrivenProperties.AiRangerVerticalErrorMultiplier = num3 * 0.1f;
            agentDrivenProperties.AiRangerHorizontalErrorMultiplier = num3 * 0.034906585f;
            agentDrivenProperties.AIAttackOnDecideChance = 1f;
            agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, (agent.Controller != Agent.ControllerType.Player) ? 1f : 0f);
            return false;
        }
    }
}
