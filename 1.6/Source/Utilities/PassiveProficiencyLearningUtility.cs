using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

public static class PassiveProficiencyLearningUtility
{
    private const int CleanupIntervalTicks = 60000;
    private const int ThrottleExpiryTicks = 120000;

    private readonly struct PassiveLearningThrottleKey
    {
        public readonly int observerId;
        public readonly int sourceId;
        public readonly int trigger;
        public readonly string targetTierDefName;

        public PassiveLearningThrottleKey(int observerId, int sourceId, PassiveProficiencyTrigger trigger, string targetTierDefName)
        {
            this.observerId = observerId;
            this.sourceId = sourceId;
            this.trigger = (int)trigger;
            this.targetTierDefName = targetTierDefName;
        }
    }

    private static readonly Dictionary<PassiveLearningThrottleKey, int> lastGainByObserverAndSource = new();
    private static readonly List<PassiveLearningThrottleKey> cleanupBuffer = new();
    private static readonly List<ProficiencyDef> cachedSpeechTracks = new();
    private static int cachedSpeechTracksForDefCount = -1;
    private static int nextCleanupTick;

    public static void NotifyWeaponUse(Pawn sourcePawn, Thing weapon)
    {
        if (sourcePawn is not { Spawned: true } || weapon == null)
        {
            return;
        }

        if (!ProficiencyUtility.TryGetRequiredProficiencyTier(weapon, out var track, out var sourceTier, out _)
            || track == null
            || sourceTier == null
            || !ProficiencyUtility.IsTrackEnabled(track))
        {
            return;
        }

        ProcessTrackEvent(track, PassiveProficiencyTrigger.WeaponUse, sourcePawn, sourceTier);
    }

    public static void NotifySpeechInteraction(Pawn speaker)
    {
        if (speaker is not { Spawned: true })
        {
            return;
        }

        var speechTracks = GetSpeechTracks();
        for (var trackIndex = 0; trackIndex < speechTracks.Count; trackIndex++)
        {
            var track = speechTracks[trackIndex];
            if (!ProficiencyUtility.IsTrackEnabled(track))
            {
                continue;
            }

            var sourceTier = ProficiencyUtility.GetCurrentTier(speaker, track);
            if (sourceTier == null)
            {
                continue;
            }

            ProcessTrackEvent(track, PassiveProficiencyTrigger.Speech, speaker, sourceTier);
        }
    }

    private static void ProcessTrackEvent(ProficiencyDef track, PassiveProficiencyTrigger trigger, Pawn sourcePawn, ProficiencyTierDef sourceTier)
    {
        if (track == null
            || sourcePawn is not { Spawned: true, Map: not null }
            || sourceTier == null
            || track.passiveSources == null
            || track.passiveSources.Count == 0)
        {
            return;
        }

        var currentTick = Find.TickManager?.TicksGame ?? 0;
        TryCleanupThrottle(currentTick);
        var map = sourcePawn.Map;
        var sourcePos = sourcePawn.Position;
        var sourceTierIndex = track.tiers.IndexOf(sourceTier);
        if (sourceTierIndex < 0)
        {
            return;
        }

        var allPawns = map.mapPawns?.AllPawnsSpawned;
        if (allPawns == null || allPawns.Count == 0)
        {
            return;
        }

        for (var sourceIndex = 0; sourceIndex < track.passiveSources.Count; sourceIndex++)
        {
            var passiveSource = track.passiveSources[sourceIndex];
            if (passiveSource == null || passiveSource.trigger != trigger || passiveSource.targetTier == null)
            {
                continue;
            }

            var minSourceIdx = passiveSource.minimumSourceTier == null
                ? 0
                : track.tiers.IndexOf(passiveSource.minimumSourceTier);
            if (minSourceIdx < 0 || sourceTierIndex < minSourceIdx)
            {
                continue;
            }

            var targetTierIndex = track.tiers.IndexOf(passiveSource.targetTier);
            // Tier index 0 is the baseline tier and should not be passively "promoted into".
            if (targetTierIndex <= 0)
            {
                continue;
            }

            var minLearnerIdx = passiveSource.minimumLearnerTier == null
                ? 0
                : track.tiers.IndexOf(passiveSource.minimumLearnerTier);
            var maxLearnerIdx = passiveSource.maximumLearnerTier == null
                ? track.tiers.Count - 1
                : track.tiers.IndexOf(passiveSource.maximumLearnerTier);
            if (minLearnerIdx < 0 || maxLearnerIdx < 0 || minLearnerIdx > maxLearnerIdx)
            {
                continue;
            }

            var radius = passiveSource.radius > 0f
                ? passiveSource.radius
                : Mathf.Max(EducationSettings.MinPassiveLearningRadius, EducationMod.settings.passiveLearningRadius);
            var radiusSq = radius * radius;
            var baseGain = passiveSource.gainAmount > 0f
                ? passiveSource.gainAmount
                : trigger == PassiveProficiencyTrigger.WeaponUse
                    ? EducationMod.settings.passiveWeaponGainPerEvent
                    : EducationMod.settings.passiveSpeechGainPerEvent;
            if (baseGain <= 0f)
            {
                continue;
            }

            var cooldownTicks = passiveSource.cooldownTicks >= 0
                ? passiveSource.cooldownTicks
                : EducationMod.settings.passiveLearningCooldownTicks;

            for (var i = 0; i < allPawns.Count; i++)
            {
                var observer = allPawns[i];
                if (observer == null
                    || observer == sourcePawn
                    || !observer.Spawned
                    || observer.Map != map
                    || observer.Dead
                    || observer.Downed
                    || !observer.Awake()
                    || observer.Position.DistanceToSquared(sourcePos) > radiusSq
                    || !observer.CanHaveProficiencies())
                {
                    continue;
                }

                var observerTier = ProficiencyUtility.GetCurrentTier(observer, track);
                var observerTierIndex = observerTier != null ? track.tiers.IndexOf(observerTier) : -1;
                if (observerTierIndex < minLearnerIdx
                    || observerTierIndex > maxLearnerIdx
                    || observerTierIndex >= targetTierIndex)
                {
                    continue;
                }

                if (passiveSource.requireOneTierBelowTarget
                    && !ProficiencyUtility.IsOneTierBelow(observer, track, passiveSource.targetTier))
                {
                    continue;
                }

                var throttleKey = new PassiveLearningThrottleKey(
                    observer.thingIDNumber,
                    sourcePawn.thingIDNumber,
                    trigger,
                    passiveSource.targetTier.defName);
                if (cooldownTicks > 0
                    && lastGainByObserverAndSource.TryGetValue(throttleKey, out var lastTick)
                    && currentTick - lastTick < cooldownTicks)
                {
                    continue;
                }

                var gain = baseGain * Mathf.Max(0f, observer.GetStatValue(StatDefOf.GlobalLearningFactor));
                if (gain <= 0f)
                {
                    continue;
                }

                // Semester goals must be positive to avoid divide-by-zero style progression behavior.
                var goal = Mathf.Max(1f, passiveSource.targetTier.semesterGoal);
                var progress = EducationManager.Instance.AddProficiencyClassProgress(observer, track, passiveSource.targetTier, gain, goal);
                if (progress >= goal)
                {
                    ProficiencyUtility.GrantTier(observer, track, passiveSource.targetTier);
                    EducationManager.Instance.ClearProficiencyClassProgress(observer, track, passiveSource.targetTier);
                }

                if (cooldownTicks > 0)
                {
                    lastGainByObserverAndSource[throttleKey] = currentTick;
                }
            }
        }
    }

    private static List<ProficiencyDef> GetSpeechTracks()
    {
        var allDefs = DefDatabase<ProficiencyDef>.AllDefsListForReading;
        if (cachedSpeechTracksForDefCount == allDefs.Count)
        {
            return cachedSpeechTracks;
        }

        cachedSpeechTracks.Clear();
        for (var i = 0; i < allDefs.Count; i++)
        {
            var track = allDefs[i];
            if (track.passiveSources == null || track.passiveSources.Count == 0)
            {
                continue;
            }

            for (var j = 0; j < track.passiveSources.Count; j++)
            {
                if (track.passiveSources[j]?.trigger == PassiveProficiencyTrigger.Speech)
                {
                    cachedSpeechTracks.Add(track);
                    break;
                }
            }
        }

        cachedSpeechTracksForDefCount = allDefs.Count;
        return cachedSpeechTracks;
    }

    private static void TryCleanupThrottle(int currentTick)
    {
        if (currentTick < nextCleanupTick)
        {
            return;
        }

        nextCleanupTick = currentTick + CleanupIntervalTicks;
        if (lastGainByObserverAndSource.Count == 0)
        {
            return;
        }

        cleanupBuffer.Clear();
        foreach (var pair in lastGainByObserverAndSource)
        {
            if (currentTick - pair.Value > ThrottleExpiryTicks)
            {
                cleanupBuffer.Add(pair.Key);
            }
        }

        for (var i = 0; i < cleanupBuffer.Count; i++)
        {
            lastGainByObserverAndSource.Remove(cleanupBuffer[i]);
        }
    }
}
