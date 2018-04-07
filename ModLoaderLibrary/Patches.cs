using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using UnityEngine.UI;

namespace ModLoaderLibrary
{
    /// <summary>
    /// This harmony patch adds on to the end of the game version string that displays in the upper-right corner of the game.
    /// This function only adds the ModLoader version as well as the number of mods currently loaded in the game.
    /// </summary>
    [HarmonyPatch(typeof(GameVersionLabelUI))]
    [HarmonyPatch("Awake")]
    class GameVersionLabelPatch
    {
        static void Postfix(GameVersionLabelUI __instance)
        {
            // GameVersionLabelUI.versionLabelText is a private method, so we have to do some fancy reflection to get to it.
            var versionLabelTextField = typeof(GameVersionLabelUI).GetField("versionLabelText", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            var versionLabelText = versionLabelTextField.GetValue(__instance) as Text;
            // Add on to the text on the next line with our ModLoader information
            versionLabelText.text += "\nModLoader " + Constants.verMajor + "." + Constants.verMinor + "." + Constants.verRevision + " with " + Constants.numMods + " mods loaded.";
        }
    }
}
