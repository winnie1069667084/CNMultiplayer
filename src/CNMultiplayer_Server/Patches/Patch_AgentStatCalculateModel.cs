using HarmonyLib;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HarmonyPatches
{
    /*[HarmonyPatch(typeof(AgentStatCalculateModel), "SetAiRelatedProperties")]//强化近战AI（仅限攻城模式）
    internal class Patch_SetAiRelatedProperties//变态AI
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
    */
    [HarmonyPatch(typeof(AgentStatCalculateModel), "SetAiRelatedProperties")]//强化近战AI（仅限攻城模式）
    internal class Patch_SetAiRelatedProperties
    {
        public static bool Prefix(Agent agent, ref AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedItem, WeaponComponentData secondaryItem, AgentStatCalculateModel __instance)
        {
            string gameType = MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            if ( gameType != "CNMSiege" || gameType != "IndividualDeathMatch")
            { return true; }
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
            SkillObject skill = (equippedItem == null) ? DefaultSkills.Athletics : equippedItem.RelevantSkill;
            int effectiveSkill = __instance.GetEffectiveSkill(agent.Character, agent.Origin, agent.Formation, skill);
            int melee_Ai = MBMath.ClampInt(mPHeroClassForCharacter.MeleeAI, 0, 100);//设定近战AI为0-100之间的数值
            int ranged_Ai = MBMath.ClampInt(mPHeroClassForCharacter.RangedAI, 0, 100);//设定远程AI为0-100之间的数值
            float num = melee_Ai * 0.01f;
            float num2 = ranged_Ai * 0.01f; ;
            float num3 = num + agent.Defensiveness;
            agentDrivenProperties.AiRangedHorsebackMissileRange = 0.3f + 0.4f * num2;
            agentDrivenProperties.AiFacingMissileWatch = -0.96f + num * 0.06f;
            agentDrivenProperties.AiFlyingMissileCheckRadius = 8f - 6f * num;
            agentDrivenProperties.AiShootFreq = 0.3f + 0.7f * num2;
            agentDrivenProperties.AiWaitBeforeShootFactor = (agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0f : (1f - 0.5f * num2));
            agentDrivenProperties.AIBlockOnDecideAbility = 1f * MBMath.ClampFloat(num, 0f, 1f); //加强AI格挡概率
            agentDrivenProperties.AIParryOnDecideAbility = MBMath.Lerp(0.01f, 0.95f, MBMath.ClampFloat(TaleWorlds.Library.MathF.Pow(num, 1.5f), 0f, 1f));
            agentDrivenProperties.AiTryChamberAttackOnDecide = (num - 0.15f) * 0.1f;
            agentDrivenProperties.AIAttackOnParryChance = 0.3f - 0.1f * agent.Defensiveness;
            agentDrivenProperties.AiAttackOnParryTiming = -0.2f + 0.3f * num;
            agentDrivenProperties.AIDecideOnAttackChance = 0.15f * agent.Defensiveness;
            agentDrivenProperties.AIParryOnAttackAbility = MBMath.ClampFloat(num * num * num, 0f, 1f);
            agentDrivenProperties.AiKick = -0.1f + ((num > 0.4f) ? 0.4f : num);
            agentDrivenProperties.AiAttackCalculationMaxTimeFactor = num;
            agentDrivenProperties.AiDecideOnAttackWhenReceiveHitTiming = -0.05f * (1f - num); //降低AI受到伤害后依然进攻概率
            agentDrivenProperties.AiDecideOnAttackContinueAction = -0.5f * (1f - num);
            agentDrivenProperties.AiDecideOnAttackingContinue = 0.1f * num;
            agentDrivenProperties.AIParryOnAttackingContinueAbility = MBMath.Lerp(0.05f, 0.95f, MBMath.ClampFloat(num * num * num, 0f, 1f));
            agentDrivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility = 1f * MBMath.ClampFloat(num, 0f, 1f); //加强AI格挡概率
            agentDrivenProperties.AIRealizeBlockingFromIncorrectSideAbility = 1f * MBMath.ClampFloat(num, 0f, 1f); //加强AI应对变招的防御概率
            agentDrivenProperties.AiAttackingShieldDefenseChance = 0.2f + 0.3f * num;
            agentDrivenProperties.AiAttackingShieldDefenseTimer = -0.3f + 0.3f * num;
            agentDrivenProperties.AiRandomizedDefendDirectionChance = 1f - TaleWorlds.Library.MathF.Log(num * 7f + 1f, 2f) * 0.33333f;
            agentDrivenProperties.AiShooterError = 0.008f;
            agentDrivenProperties.AISetNoAttackTimerAfterBeingHitAbility = MBMath.ClampFloat(num * num, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoAttackTimerAfterBeingParriedAbility = MBMath.ClampFloat(num * num, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoDefendTimerAfterHittingAbility = MBMath.ClampFloat(num * num, 0.05f, 0.95f);
            agentDrivenProperties.AISetNoDefendTimerAfterParryingAbility = MBMath.ClampFloat(num * num, 0.05f, 0.95f);
            agentDrivenProperties.AIEstimateStunDurationPrecision = 1f - MBMath.ClampFloat(num * num, 0.05f, 0.95f);
            agentDrivenProperties.AIHoldingReadyMaxDuration = MBMath.Lerp(0.25f, 0f, TaleWorlds.Library.MathF.Min(1f, num * 1.2f));
            agentDrivenProperties.AIHoldingReadyVariationPercentage = num;
            agentDrivenProperties.AiRaiseShieldDelayTimeBase = -0.75f + 0.5f * num;
            agentDrivenProperties.AiUseShieldAgainstEnemyMissileProbability = 0.1f + num * 0.6f + num3 * 0.2f;
            agentDrivenProperties.AiCheckMovementIntervalFactor = 0.005f * (1.1f - num);
            agentDrivenProperties.AiMovementDelayFactor = 4f / (3f + num2);
            agentDrivenProperties.AiParryDecisionChangeValue = 0.05f + 0.7f * num;
            agentDrivenProperties.AiDefendWithShieldDecisionChanceValue = TaleWorlds.Library.MathF.Min(1f, 0.2f + 0.5f * num + 0.2f * num3);
            agentDrivenProperties.AiMoveEnemySideTimeValue = -2.5f + 0.5f * num;
            agentDrivenProperties.AiMinimumDistanceToContinueFactor = 2f + 0.3f * (3f - num);
            agentDrivenProperties.AiHearingDistanceFactor = 1f + num;
            agentDrivenProperties.AiChargeHorsebackTargetDistFactor = 1.5f * (3f - num);
            agentDrivenProperties.AiWaitBeforeShootFactor = (agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0f : (1f - 0.5f * num2));
            float num4 = 1f - num2;
            agentDrivenProperties.AiRangerLeadErrorMin = (0f - num4) * 0.35f;
            agentDrivenProperties.AiRangerLeadErrorMax = num4 * 0.2f;
            agentDrivenProperties.AiRangerVerticalErrorMultiplier = num4 * 0.1f;
            agentDrivenProperties.AiRangerHorizontalErrorMultiplier = num4 * ((float)Math.PI / 90f);
            agentDrivenProperties.AIAttackOnDecideChance = TaleWorlds.Library.MathF.Clamp(0.23f * __instance.CalculateAIAttackOnDecideMaxValue() * (3f - agent.Defensiveness), 0.05f, 1f);
            agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, (agent.Controller != Agent.ControllerType.Player) ? 1f : 0f);
            return false;
        }
    }
}
