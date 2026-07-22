using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(Verb), nameof(Verb.TryCastNextBurstShot))]
public static class Verb_TryCastNextBurstShot_Patch
{
    private static readonly Dictionary<int, int> lastProcessedTickByShooter = new();

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

        var currentTick = Find.TickManager?.TicksGame ?? 0;
        var minIntervalTicks = EducationMod.settings.passiveLearningCooldownTicks <= 0
            ? 1
            : Mathf.Min(10, EducationMod.settings.passiveLearningCooldownTicks);
        if (lastProcessedTickByShooter.TryGetValue(casterPawn.thingIDNumber, out var lastTick)
            && currentTick - lastTick < minIntervalTicks)
        {
            return;
        }

        lastProcessedTickByShooter[casterPawn.thingIDNumber] = currentTick;
        PassiveProficiencyLearningUtility.NotifyWeaponUse(casterPawn, equipmentSource);
    }
}
