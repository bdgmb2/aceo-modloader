using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Harmony;
using UnityEngine.UI;
using UnityEngine;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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

    /// <summary>
    /// This harmony patch adds an update-checker to another early main menu function that gets called. If this function
    /// detects a newer version of ModLoader, it will send the default Airport CEO MessageDialog to the user telling them
    /// there is an update available.
    /// 
    /// NOTE: THIS CODE DOES NOT WORK! I'm doing something wrong!
    /// </summary>
    [HarmonyPatch(typeof(MainMenuUI))]
    [HarmonyPatch("Start")]
    class CheckforUpdatesPatch
    {
        // If there is an update to ModLoader, trigger a message dialog to the player
        static void Postfix()
        {
            // Mono SSL validation always fails, this line does NOT solve the problem!
            ServicePointManager.ServerCertificateValidationCallback += (object sender, X509Certificate cert,
                X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
            // Get the list of releases from GitHub
            var req = WebRequest.Create(Constants.githubReleaseURL) as HttpWebRequest;
            List<GitHubRelease> releases = new List<GitHubRelease>();
            try
            {
                releases = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GitHubRelease>>(new Func<string>(() =>
                {
                    using (StreamReader r = new StreamReader(req.GetResponse().GetResponseStream()))
                    {
                        string output = r.ReadToEnd();
                        LogOutput.Log("Received response from GitHub: " + output);
                        return output;
                    }
                })());
            }
            catch (Exception e)
            {
                LogOutput.Log("Something went wrong when connecting to the internet to find ModLoader releases: " + e.Message, LogOutput.LogType.WARNING);
                while (e.InnerException != null)
                {
                    LogOutput.Log("Inner Exception: " + e.Message, LogOutput.LogType.WARNING);
                    e = e.InnerException;
                }
            }
            foreach (GitHubRelease rel in releases)
            {
                try
                {
                    string version = rel.tag_name;
                    var verValues = version.Split('.');
                    LogOutput.Log("Checking ver " + verValues[0] + "." + verValues[1] + "." + verValues[2]);
                    if (Convert.ToInt32(verValues[0]) > Constants.verMajor ||  //Major Version
                        Convert.ToInt32(verValues[1]) > Constants.verMinor ||  // Minor Version
                        Convert.ToInt32(verValues[2]) > Constants.verRevision) // Revision
                    {
                        // If any of the above is true, there is a release with a higher version number than what the player is running!
                        DialogPanel.Instance.ShowMessagePanel("There is an update for ModLoader: " + verValues[0] + "." + verValues[1] + "." + verValues[2]);
                        LogOutput.Log("Newer version of ModLoader on the web: " + verValues[0] + "." + verValues[1] + "." + verValues[2]);
                    }
                }
                catch (Exception e)
                {
                    LogOutput.Log("Something went wrong when getting latest ModLoader release: " + e.Message, LogOutput.LogType.WARNING);
                }
            }
        }
    }
}
