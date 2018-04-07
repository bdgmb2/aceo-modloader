using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;

/*  STOP! Before building this project, make SURE you have all required
 *  references in the project's root!
 */

namespace RealisticVehicles
{
    /// <summary>
    /// This mod changes the bus company and model names to more realistic, real-world ones.
    /// </summary>
    public class Main
    {
        // This function gets called by ModLoader at the start of the game
        public static void GameInitializing()
        {
            // As per the harmony Wiki, I give this mod a unique identifier, starting with my username and then
            // the name of the mod.
            var harmony = HarmonyInstance.Create("com.catcherben.RealisticVehicles");

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        // This function gets called by ModLoader on game shutdown
        public static void GameExiting()
        {
            // This mod doesn't need to do anything special when the game shuts down, so it's empty.
        }
    }

    // Here I'm rewriting the newCompanies and newTypes variables with some more realistic names
    [HarmonyPatch(typeof(BusModel))]
    [HarmonyPatch("Initialize")]
    public class OverrideBusInformation
    {
        private static string[] newCompanies =
        {
            "AirportConnect",
            "InterCity",
            "City Bus",
            "Airport Express",
            "Airport Shuttle"
        };

        private static string[] newTypes =
        {
            "Alexander Dennis",
            "Van Hool",
            "GOLAZ",
            "Scania",
            "Prevost",
            "Mercedes-Benz",
            "MAZ",
            "Autosan"
        };

        /// <summary>
        /// Check out the Harmony Wiki (https://github.com/pardeike/Harmony/wiki) for more information on how this works.
        /// </summary>
        /// <param name="__instance">The BusModel instance we are overriding</param>
        [HarmonyPostfix]
        static void Postfix(BusModel __instance)
        {
            __instance.company = newCompanies[Utils.RandomRangeI(0f, (float) newCompanies.Length - 1)];
            __instance.vehicleModelName = newTypes[Utils.RandomRangeI(0f, (float) newTypes.Length - 1)];
        }
    }
}