using UnityEngine;
using Verse;

namespace ProgressionEducation;

public class EducationSettings : ModSettings
{
    public const float MinPassiveLearningRadius = 1f;
    private const float MaxPassiveCooldownTicks = 12000f;

    public float daycareClassesLearningSpeedModifier = 1f;
    public bool debugMode;
    public bool enableProficiencySystem = true;
    public float globalLearningSpeedModifier = 1f;
    public float proficiencyClassesLearningSpeedModifier = 1f;
    public float skillClassesLearningSpeedModifier = 1f;
    public bool enableKnowledgePanel = true;
    public bool bestowProficiencyToAll = false;
    public bool enableWeaponProficiency = true;
    public bool enableVehicleProficiency = true;
    public bool enableSpeechProficiency = true;
    public float passiveLearningRadius = 12f;
    public float passiveWeaponGainPerEvent = 6f;
    public float passiveSpeechGainPerEvent = 4f;
    public int passiveLearningCooldownTicks = 2500;

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new();
        listing.Begin(inRect);
        listing.Label("PE_GlobalLearningSpeed".Translate()
                      + ": "
                      + (globalLearningSpeedModifier * 100).ToString("F0")
                      + "%");
        globalLearningSpeedModifier =
            listing.Slider(globalLearningSpeedModifier, 0.1f, 10f);
        listing.Label("PE_SkillClassesLearningSpeed".Translate()
                      + ": "
                      + (skillClassesLearningSpeedModifier * 100).ToString("F0")
                      + "%");
        skillClassesLearningSpeedModifier =
            listing.Slider(skillClassesLearningSpeedModifier, 0.1f, 10f);
        listing.Label("PE_ProficiencyClassesLearningSpeed".Translate()
                      + ": "
                      + (proficiencyClassesLearningSpeedModifier * 100).ToString("F0")
                      + "%");
        proficiencyClassesLearningSpeedModifier =
            listing.Slider(proficiencyClassesLearningSpeedModifier, 0.1f, 10f);
        listing.Label("PE_DaycareClassesLearningSpeed".Translate()
                      + ": "
                      + (daycareClassesLearningSpeedModifier * 100).ToString("F0")
                      + "%");
        daycareClassesLearningSpeedModifier =
            listing.Slider(daycareClassesLearningSpeedModifier, 0.1f, 10f);
        listing.CheckboxLabeled("PE_EnableProficiencySystem".Translate(),
            ref enableProficiencySystem);

        listing.Gap();
        listing.CheckboxLabeled("PE_EnableKnowledgePanel".Translate(), ref enableKnowledgePanel);
        listing.CheckboxLabeled("PE_BestowProficiencyToAll".Translate(), ref bestowProficiencyToAll);
        listing.CheckboxLabeled("PE_EnableWeaponProficiency".Translate(), ref enableWeaponProficiency);
        listing.CheckboxLabeled("PE_EnableVehicleProficiency".Translate(), ref enableVehicleProficiency);
        listing.CheckboxLabeled("PE_EnableSpeechProficiency".Translate(), ref enableSpeechProficiency);
        listing.GapLine();
        listing.Label("PE_PassiveLearningRadius".Translate()
                      + ": "
                      + passiveLearningRadius.ToString("F1"));
        passiveLearningRadius = listing.Slider(passiveLearningRadius, MinPassiveLearningRadius, 40f);
        listing.Label("PE_PassiveWeaponGainPerEvent".Translate()
                      + ": "
                      + passiveWeaponGainPerEvent.ToString("F1"));
        passiveWeaponGainPerEvent = listing.Slider(passiveWeaponGainPerEvent, 0f, 50f);
        listing.Label("PE_PassiveSpeechGainPerEvent".Translate()
                      + ": "
                      + passiveSpeechGainPerEvent.ToString("F1"));
        passiveSpeechGainPerEvent = listing.Slider(passiveSpeechGainPerEvent, 0f, 50f);
        listing.Label("PE_PassiveLearningCooldownSeconds".Translate()
                      + ": "
                      + (passiveLearningCooldownTicks / 60f).ToString("F1"));
        passiveLearningCooldownTicks = Mathf.RoundToInt(listing.Slider(passiveLearningCooldownTicks, 0f, MaxPassiveCooldownTicks));
        listing.Gap();
        listing.CheckboxLabeled("PE_EnableDebugMode".Translate(), ref debugMode);
        listing.End();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref globalLearningSpeedModifier,
            "globalLearningSpeedModifier", 1f);
        Scribe_Values.Look(ref skillClassesLearningSpeedModifier,
            "skillClassesLearningSpeedModifier", 1f);
        Scribe_Values.Look(ref proficiencyClassesLearningSpeedModifier,
            "proficiencyClassesLearningSpeedModifier", 1f);
        Scribe_Values.Look(ref daycareClassesLearningSpeedModifier,
            "daycareClassesLearningSpeedModifier", 1f);
        Scribe_Values.Look(ref enableProficiencySystem, "enableProficiencySystem",
            true);
        Scribe_Values.Look(ref debugMode, "debugMode");
        Scribe_Values.Look(ref enableKnowledgePanel, "enableKnowledgePanel", true);
        Scribe_Values.Look(ref bestowProficiencyToAll, "bestowProficiencyToAll", false);
        Scribe_Values.Look(ref enableWeaponProficiency, "enableWeaponProficiency", true);
        Scribe_Values.Look(ref enableVehicleProficiency, "enableVehicleProficiency", true);
        Scribe_Values.Look(ref enableSpeechProficiency, "enableSpeechProficiency", true);
        Scribe_Values.Look(ref passiveLearningRadius, "passiveLearningRadius", 12f);
        Scribe_Values.Look(ref passiveWeaponGainPerEvent, "passiveWeaponGainPerEvent", 6f);
        Scribe_Values.Look(ref passiveSpeechGainPerEvent, "passiveSpeechGainPerEvent", 4f);
        Scribe_Values.Look(ref passiveLearningCooldownTicks, "passiveLearningCooldownTicks", 2500);
    }
}
