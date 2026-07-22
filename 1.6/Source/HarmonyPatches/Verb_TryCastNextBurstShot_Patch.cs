using HarmonyLib;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(Verb), nameof(Verb.TryCastNextBurstShot))]
public static class Verb_TryCastNextBurstShot_Patch
{
    public static void Postfix(Verb __instance, bool __result)
    {
        if (!__result)
        {
            return;
        }

        var casterPawn = __instance.CasterPawn;
        var equipmentSource = __instance.EquipmentSource;
        if (casterPawn == null || equipmentSource == null)
        {
            return;
        }

        PassiveProficiencyLearningUtility.NotifyWeaponUse(casterPawn, equipmentSource);
    }
}
