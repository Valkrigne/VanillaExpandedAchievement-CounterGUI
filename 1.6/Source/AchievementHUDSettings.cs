using Verse;

namespace AchievementHUD
{
    public class AchievementHUDSettings : ModSettings
    {
        public bool debugLogging = false;
        public GameFont fontSize = GameFont.Medium;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
            Scribe_Values.Look(ref fontSize, "fontSize", GameFont.Medium);
        }
    }
}
