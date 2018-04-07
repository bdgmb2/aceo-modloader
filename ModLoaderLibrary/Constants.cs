using System;
using System.Collections.Generic;
using System.Reflection;

namespace ModLoaderLibrary
{
    class Constants
    {
        public static readonly string verMajor = "0";
        public static readonly string verMinor = "0";
        public static readonly string verRevision = "1";

        public static int numMods = 0;

        public static List<Tuple<string, Assembly>> modAssemblies = new List<Tuple<string, Assembly>>();
    }
}
