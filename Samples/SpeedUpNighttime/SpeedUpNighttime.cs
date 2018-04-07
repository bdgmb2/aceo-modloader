using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;

/*  STOP! Before building this project, make SURE you have all required
 *  references in the project's root!
 */

namespace SpeedUpNighttime
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
            var harmony = HarmonyInstance.Create("com.catcherben.SpeedUpNighttime");

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        // This function gets called by ModLoader on game shutdown
        public static void GameExiting()
        {
            // This mod doesn't need to do anything special when the game shuts down, so it's empty.
        }
    }

    /// <summary>
    /// This class changes the speed up time at night to be twice as fast as the defualt
    /// </summary>
    [HarmonyPatch(typeof(TimeController))]
    [HarmonyPatch("AdvanceTimeFast")]
    public class ModifySpeedUpValue
    {
        [HarmonyPostfix]
        static void Postfix(TimeController __instance)
        {
            // The default advance time speed is 100f, this makes it go twice as fast!
            __instance.currentSpeed = 200f;
        }
    }
}
