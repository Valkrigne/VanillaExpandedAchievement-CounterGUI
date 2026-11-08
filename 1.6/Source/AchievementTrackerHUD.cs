using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace AchievementHUD
{
    public class AchievementTrackerHUD : GameComponent
    {
        private static bool typeLookupAttempted = false;
        private static Type managerType = null;
        private static PropertyInfo achievementListProperty = null;
        private static PropertyInfo unlockedProperty = null;

        private int lastLoggedTick = 0;


        private bool initialCheckDone = false;
        private int lastCheckedTick = 0;
        private const int CheckIntervalTicks = 250; // Roughly 4 seconds at 1x

        private int cachedUnlockedCount = 0;
        private int cachedTotalCount = 0;
        private bool hasValidData = false;

        public AchievementTrackerHUD(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            UpdateAndCacheCounts(checkTotal: true);
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();

            if (Current.Game == null || Find.CurrentMap == null)
                return;

            if (Find.World?.renderer?.wantedMode != WorldRenderMode.None)
                return;

            bool needTotalCount = !initialCheckDone || cachedTotalCount == 0;

            if (needTotalCount || Find.TickManager.TicksGame >= lastCheckedTick + CheckIntervalTicks)
            {
                UpdateAndCacheCounts(checkTotal: needTotalCount);
            }

            if (hasValidData)
            {
                if (AchievementHUDMod.settings.debugLogging && Find.TickManager.TicksGame > lastLoggedTick + 300)
                {
                    lastLoggedTick = Find.TickManager.TicksGame;
                    Log.Message($"[AchievementHUD Debug] Unlocked: {cachedUnlockedCount} | Total Tracked: {cachedTotalCount}");
                }

                float boxWidth = 160f;
                float boxHeight = 60f;
                float rightPadding = 20f;
                float topPadding = 20f;

                Rect hudRect = new Rect(
                    UI.screenWidth - boxWidth - rightPadding,
                    topPadding,
                    boxWidth,
                    boxHeight
                );

                Text.Font = AchievementHUDMod.settings.fontSize;
                Text.Anchor = TextAnchor.UpperRight;
                GUI.color = Color.white;

                string displayText = $"Achievements\n{cachedUnlockedCount} / {cachedTotalCount}";
                Widgets.Label(hudRect, displayText);

                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }

        private void UpdateAndCacheCounts(bool checkTotal)
        {
            if (Find.TickManager == null) return;

            lastCheckedTick = Find.TickManager.TicksGame;

            bool success = TryGetAchievementCounts(out int unlockedCount, out int totalCount, checkTotal);

            if (success)
            {
                hasValidData = true;
                cachedUnlockedCount = unlockedCount;

                if (checkTotal)
                {
                    cachedTotalCount = totalCount;
                    initialCheckDone = true; 
                }
            }
        }

        private bool TryGetAchievementCounts(out int unlockedCount, out int totalCount, bool calculateTotal)
        {
            unlockedCount = 0;
            totalCount = cachedTotalCount;

            if (!typeLookupAttempted)
            {
                typeLookupAttempted = true;
                try
                {
                    managerType = GenTypes.GetTypeInAnyAssembly("AchievementsExpanded.AchievementPointManager");
                    if (managerType == null)
                    {
                        Log.Warning("[AchievementHUD Debug] Type 'AchievementsExpanded.AchievementPointManager' not found in any loaded assembly.");
                        return false;
                    }

                    achievementListProperty = managerType.GetProperty("AchievementList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (achievementListProperty == null)
                    {
                        Log.Warning("[AchievementHUD Debug] Property 'AchievementList' could not be found on AchievementPointManager.");
                    }

                    Type cardType = GenTypes.GetTypeInAnyAssembly("AchievementsExpanded.AchievementCard");
                    if (cardType == null)
                    {
                        Log.Warning("[AchievementHUD Debug] Type 'AchievementsExpanded.AchievementCard' not found in any loaded assembly.");
                        return false;
                    }

                    unlockedProperty = cardType.GetProperty("Unlocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (unlockedProperty == null)
                    {
                        FieldInfo unlockedField = cardType.GetField("unlocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                               ?? cardType.GetField("Unlocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (unlockedField == null)
                        {
                            Log.Warning("[AchievementHUD Debug] Could not find an 'Unlocked' field OR property on AchievementCard.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[AchievementHUD] Exception during initial reflection setup: {ex.Message}");
                }
            }

            if (managerType == null || achievementListProperty == null)
            {
                return false;
            }

            try
            {
                object managerInstance = Current.Game.GetComponent(managerType);
                if (managerInstance == null)
                {
                    Log.Warning("[AchievementHUD Debug] Current.Game.GetComponent for AchievementPointManager returned null.");
                    return false;
                }

                IEnumerable achievementList = achievementListProperty.GetValue(managerInstance) as IEnumerable;
                if (achievementList == null)
                {
                    Log.Warning("[AchievementHUD Debug] Unable to cast AchievementList property value to IEnumerable.");
                    return false;
                }

                if (calculateTotal)
                {
                    totalCount = 0;
                    foreach (object card in achievementList)
                    {
                        if (card == null) continue;

                        totalCount++;

                        bool isUnlocked = false;
                        if (unlockedProperty != null)
                        {
                            isUnlocked = (bool)unlockedProperty.GetValue(card);
                        }
                        else
                        {
                            FieldInfo fallbackField = card.GetType().GetField("unlocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                                   ?? card.GetType().GetField("Unlocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (fallbackField != null)
                            {
                                isUnlocked = (bool)fallbackField.GetValue(card);
                            }
                        }

                        if (isUnlocked)
                        {
                            unlockedCount++;
                        }
                    }
                    return totalCount > 0;
                }
                else
                {
                    foreach (object card in achievementList)
                    {
                        if (card == null) continue;

                        bool isUnlocked = false;
                        if (unlockedProperty != null)
                        {
                            isUnlocked = (bool)unlockedProperty.GetValue(card);
                        }
                        else
                        {
                            FieldInfo fallbackField = card.GetType().GetField("unlocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                                   ?? card.GetType().GetField("Unlocked", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (fallbackField != null)
                            {
                                isUnlocked = (bool)fallbackField.GetValue(card);
                            }
                        }

                        if (isUnlocked)
                        {
                            unlockedCount++;
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[AchievementHUD Debug] Runtime tracking iteration failed: {ex.Message}");
                return false;
            }
        }
    }
}