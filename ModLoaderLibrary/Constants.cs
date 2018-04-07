using System;
using System.Collections.Generic;
using System.Reflection;

namespace ModLoaderLibrary
{
    class Constants
    {
        public static readonly int verMajor = 0;
        public static readonly int verMinor = 0;
        public static readonly int verRevision = 2;

        public static readonly string githubReleaseURL = "https://api.github.com/repos/bdgmb2/aceo-modloader/releases";

        public static int numMods = 0;

        public static List<Tuple<string, Assembly>> modAssemblies = new List<Tuple<string, Assembly>>();
    }
}
