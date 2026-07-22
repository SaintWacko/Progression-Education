using Verse;

namespace ProgressionEducation;

public enum PassiveProficiencyTrigger
{
    WeaponUse,
    Speech,
}

public class PassiveProficiencySource
{
    public PassiveProficiencyTrigger trigger;
    public ProficiencyTierDef targetTier;
    public ProficiencyTierDef minimumSourceTier;
    public ProficiencyTierDef minimumLearnerTier;
    public ProficiencyTierDef maximumLearnerTier;
    public float radius = -1f;
    public float gainAmount = -1f;
    public int cooldownTicks = -1;
    public bool requireOneTierBelowTarget = true;
}
