using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Patches
{
    [HarmonyPatch(typeof(MultiplayerStrikeMagnitudeModel), "ComputeRawDamage")]//调整伤害系数（仅限攻城模式）
    internal class Patch_ComputeRawDamage
    {
        public static bool Prefix(ref float __result, MultiplayerStrikeMagnitudeModel __instance, DamageTypes damageType, float magnitude, float armorEffectiveness, float absorbedDamageRatio)
        {
            if (MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) != "Siege")
            { return true; }
            float bluntDamageFactorByDamageType = __instance.GetBluntDamageFactorByDamageType(damageType);
            float num = 85f / (85f + armorEffectiveness);
            float num2 = magnitude * num;
            float num3 = bluntDamageFactorByDamageType * num2;
            if (damageType != DamageTypes.Blunt)
            {
                float num4;
                if (damageType != DamageTypes.Cut)
                {
                    if (damageType != DamageTypes.Pierce)
                    {
                        Debug.FailedAssert("Given damage type is invalid.", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\ComponentInterfaces\\MultiplayerStrikeMagnitudeModel.cs", "ComputeRawDamage", 45);
                        __result = 0f;
                    }
                    num4 = MathF.Max(0f, magnitude * (30f / (30f + armorEffectiveness)));
                }
                else
                {
                    num4 = MathF.Max(0f, magnitude * (1f - 0.6f * armorEffectiveness / (20f + 0.4f * armorEffectiveness)));
                }
                num3 += (1f - bluntDamageFactorByDamageType) * num4;
            }
            __result = num3 * absorbedDamageRatio;
            return false;
        }
    }

    [HarmonyPatch(typeof(MultiplayerStrikeMagnitudeModel), "GetBluntDamageFactorByDamageType")]//调整伤害系数（钝）
    internal class Patch_GetBluntDamageFactorByDamageType
    {
        public static bool Prefix(ref float __result, DamageTypes damageType)
        {
            if (MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) != "Siege")
            { return true; }
            float result = 0f;
            switch (damageType)
            {
                case DamageTypes.Blunt:
                    result = 0.95f;
                    break;
                case DamageTypes.Cut:
                    result = 0.1f;
                    break;
                case DamageTypes.Pierce:
                    result = 0.25f;
                    break;
            }
            __result = result;
            return false;
        }
    }
}
