using UnityEngine;
using Verse;

namespace AchievementHUD
{
    public class AchievementHUDMod : Mod
    {
        public static AchievementHUDSettings settings;

        public AchievementHUDMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<AchievementHUDSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled("Enable Debug Logging", ref settings.debugLogging, "Outputs achievement calculations directly to the developer console log.");
            
            listingStandard.Gap();

            listingStandard.Label($"Text Font Size: {settings.fontSize}");
            if (listingStandard.ButtonText("Cycle Font Size"))
            {
                switch (settings.fontSize)
                {
                    case GameFont.Tiny: settings.fontSize = GameFont.Small; break;
                    case GameFont.Small: settings.fontSize = GameFont.Medium; break;
                    case GameFont.Medium: settings.fontSize = GameFont.Tiny; break;
                }
            }

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Achievement HUD Overlay";
        }
    }
}
