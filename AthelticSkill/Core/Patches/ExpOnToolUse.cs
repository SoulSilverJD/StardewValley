using System.Collections.Generic;
using HarmonyLib;
using MoonShared.Attributes;
using SpaceCore;
using StardewValley;
using StardewValley.Tools;

namespace AthleticSkill.Core.Patches
{
    /// <summary>
    /// Adds custom context tags to tools for Athletics skill categorization.
    /// </summary>
    [HarmonyPatch(typeof(StardewValley.Item), "_PopulateContextTags")]
    class PopulateContextTags_patch
    {
        [HarmonyPostfix]
        public static void Postfix(StardewValley.Item __instance, ref HashSet<string> tags)
        {
            // Categorize tools based on their "weight" or effort required
            switch (__instance)
            {
                // Heavy tools: Pickaxe and Axe
                case Pickaxe:
                case Axe:
                    tags.Add("moonslime.Athletics.heavy_tool");
                    break;

                // Light tools: FishingRod, Hoe, MilkPail, Shears, WateringCan
                case FishingRod:
                case Hoe:
                case MilkPail:
                case Shears:
                case WateringCan:
                    tags.Add("moonslime.Athletics.light_tool");
                    break;
            }
        }
    }

    /// <summary>
    /// Awards Athletics skill experience when using tools.
    /// </summary>
    [HarmonyPatch(typeof(Tool), nameof(Tool.DoFunction))]
    public static class ToolExpPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Tool __instance, Farmer who)
        {
            // --- Validate inputs ---
            if (who == null || __instance == null)
                return;

            bool isLightTool = __instance.HasContextTag("moonslime.Athletics.light_tool");
            bool isHeavyTool = __instance.HasContextTag("moonslime.Athletics.heavy_tool");

            // Only continue if the tool has either tag
            if (!isLightTool && !isHeavyTool)
                return;

            // --- Debug logging (optional) ---
            Log.Debug($"-------------------------------");
            Log.Debug($"Athletic's tool use exp tracker");

            // Random roll for chance-based EXP gain
            double checkValue = Game1.random.NextDouble();
            Log.Debug($"The check value is: {checkValue}");

            // Base chance to gain EXP from any tool use
            int baseExpChance = ModEntry.Config.ExpChanceFromTools;
            Log.Debug($"The base chance to get exp from using an atheltic's tool is: {baseExpChance}");

            // Determine difference between tool upgrade level and player's athletic level
            int difference = (__instance.UpgradeLevel * 2) - who.GetCustomSkillLevel(ModEntry.SkillID);
            Log.Debug($"The player's tool level is: {__instance.UpgradeLevel}");
            Log.Debug($"The player's athletic level is: {who.GetCustomSkillLevel(ModEntry.SkillID)}");
            Log.Debug($"The difference between player's tools and level is {difference}");

            // Final chance = base chance + 10% per level difference, then normalized to 0-1
            double finalChance = (baseExpChance + (difference * 10)) / 100.0;
            Log.Debug($"Final chance is: {finalChance}");

            // --- Check if player fails the roll ---
            Log.Trace($"Does the check fail? {checkValue >= finalChance}");
            if (checkValue >= finalChance)
                return; // Exit if EXP roll fails

            // --- EXP roll passed ---
            Log.Debug($"Check passed, EXP is gained!");
            int expToAdd = 0;

            // Determine EXP based on tool type
            if (isLightTool)
                expToAdd = ModEntry.Config.ExpFromLightToolUse;
            else if (isHeavyTool)
                expToAdd = ModEntry.Config.ExpFromHeavyToolUse;

            Log.Debug($"Total exp gained is: {expToAdd}");
            Log.Debug($"-------------------------------");

            // Apply EXP to player if any
            if (expToAdd > 0)
                Utilities.AddEXP(who, expToAdd);
        }
    }
}
