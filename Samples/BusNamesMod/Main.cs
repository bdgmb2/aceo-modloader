using System.Collections.Generic;
using Harmony;
using UnityEngine;
using Logger = ModLoaderLibrary.Logger;
using Random = System.Random;

namespace BusNamesMod
{
    public class Main
    {
        public static void GameLoading()
        {
            var harmony = HarmonyInstance.Create("com.catcherben.busnamesmod");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            Logger.Log("Bus Names Mod is Loaded!");
        }
    }

    [HarmonyPatch(typeof(BusUI))]
    [HarmonyPatch("UpdatePanel")]
    class MainMenuPatch
    {
        [HarmonyPostfix]
        public static void Postfix(BusUI __instance, BusModel ___bm, ref string ___licenseNbrText)
        {
            ___licenseNbrText = $"LICENSE PLATE: {Traverse.Create(___bm).Field("licensePlateNbr").GetValue()}";
        }
    }

    [HarmonyPatch(typeof(BusModel))]
    [HarmonyPatch("Initialize")]
    class BusModelPatch
    {
        private static readonly List<string> availTypesNew = new List<string>
        {
            "Volvo",
            "Skania",
            "VanHool",
            "MAN",
            "Alexander Dennis"
        };

        private static readonly List<string> availCompaniesNew = new List<string>
        {
            "CapitalConnect",
            "AirBus",
            "Enterprise Rental Car Shuttle",
            "Lot A Shuttle",
            "Lot B Shuttle"
        };

        private static readonly Random Gen = new Random();

        private static char GetRandomLetter()
        {
            return (char)('A' + Gen.Next(0, 26));
        }

        [HarmonyPostfix]
        public static void Postfix(BusModel __instance)
        {
            Random gen = new Random();
            __instance.company = availCompaniesNew[Utils.RandomRangeI(0f, availCompaniesNew.Count - 1)];
            __instance.vehicleModelName = availTypesNew[Utils.RandomRangeI(0f, availTypesNew.Count - 1)];
            __instance.licensePlateNbr = GetRandomLetter().ToString() + GetRandomLetter() + GetRandomLetter() + gen.Next(0, 9) + gen.Next(0, 9) + gen.Next(0, 9);
        }
    }
}