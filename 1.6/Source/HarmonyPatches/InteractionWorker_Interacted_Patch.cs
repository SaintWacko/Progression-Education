using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(InteractionWorker), nameof(InteractionWorker.Interacted))]
public static class InteractionWorker_Interacted_Patch
{
    public static void Postfix(Pawn initiator)
    {
        if (initiator == null)
        {
            return;
        }

        PassiveProficiencyLearningUtility.NotifySpeechInteraction(initiator);
    }
}
