using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using CustomRegions.Mod;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace CustomRegions.Arena
{
    internal static class Properties
    {
        public static void ApplyHooks()
        {
            On.Region.ctor_string_int_int_RainWorldGame_Timeline += Region_ctor_string_int_int_Timeline;
        }

        private static void Region_ctor_string_int_int_Timeline(On.Region.orig_ctor_string_int_int_RainWorldGame_Timeline orig, Region self, string name, int firstRoomIndex, int regionNumber, RainWorldGame game, SlugcatStats.Timeline timelineIndex)
        {
            orig(self, name, firstRoomIndex, regionNumber, game, timelineIndex);

            try
            {
                if (Region.GetFullRegionOrder().Contains(name)) return;

                string properties = WorldLoader.FindRoomFile(name, false, "_Properties.txt");
                if (!File.Exists(properties)) return;

                CustomRegionsMod.CustomLog($"loading arena properties for room [{name}]");
                RegionProperties.RegionProperties.GenerateProperties(File.ReadAllLines(properties), self, null, game, timelineIndex);
            }
            catch (Exception e) { CustomRegionsMod.CustomLog($"[ERROR] while loading arena properties, aborting...\n{e}", true); }
        }
    }
}
