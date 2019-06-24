using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Logger = ModLoaderLibrary.Logger;
using Random = System.Random;

namespace BusNamesMod
{
    public class Main
    {
        public static void GameLoading()
        {
            var harmony = new Harmony("com.catcherben.busnamesmod");
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            Logger.Log("Hello from inside Bus Names Mod!");
        }
    }

    [HarmonyPatch(typeof(VehicleModel), "GenerateLicensePlateNbr")]
    static class LicensePlatePatch
    {
        private static Random gen;
        static LicensePlatePatch()
        {
            gen = new Random();
        }
        public static char GetRandomLetter()
        {
            return (char)('A' + gen.Next(0, 26));
        }

        [HarmonyPrefix]
        public static bool Prefix(ref string ___licensePlateNbr)
        {
            ___licensePlateNbr = "";
            if (Convert.ToBoolean(gen.Next(0, 2)))
            {
                // Regular Style
                for (int i = 0; i < 3; i++)
                {
                    ___licensePlateNbr += Utils.RandomItemInCollection(Utils.alphabet);
                }
                ___licensePlateNbr += "-";
                for (int i = 0; i < 3; i++)
                {
                    ___licensePlateNbr += Utils.RandomRangeI(0f, 9f);
                }
            }
            else
            {
                // Truck/heavy style
                for (int i = 0; i < 7; i++)
                {
                    if (Convert.ToBoolean(gen.Next(0, 2)))
                    {
                        ___licensePlateNbr += Utils.RandomRangeI(0f, 9f);
                    }
                    else
                    {
                        ___licensePlateNbr += Utils.RandomItemInCollection(Utils.alphabet);
                    }

                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(BusModel), "Initialize")]
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

        [HarmonyPostfix]
        public static void Postfix(BusModel __instance)
        {
            __instance.company = availCompaniesNew[Utils.RandomRangeI(0f, availCompaniesNew.Count - 1)];
            __instance.vehicleModelName = availTypesNew[Utils.RandomRangeI(0f, availTypesNew.Count - 1)];
        }
    }
}