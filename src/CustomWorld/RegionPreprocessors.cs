using System.Collections.Generic;
using System.Linq;
using BepInEx;

namespace CustomRegions.CustomWorld
{
    internal static class RegionPreprocessors
    {

        public class RegionInfo
        {
            public string RegionID { get; internal set; }

            public SlugcatStats.Name playerCharacter { get; internal set; }
            public SlugcatStats.Timeline timeline { get; internal set; }
            /// <summary>
            /// World lines from world_XX.txt file.
            /// </summary>

            private List<string> _lines;
            public List<string> Lines {
                get { return _lines; }

                internal set
                {
                    _lines = value;
                    InitiateSectionBounds();
                }
            }

            public List<string> LinesSection(string section)
            {
                if (!sectionBounds.ContainsKey(section) || sectionBounds[section][0] == -1 || sectionBounds[section][1] == -1 || sectionBounds[section][1] < sectionBounds[section][0]) return null;

                return Lines.GetRange(sectionBounds[section][0], sectionBounds[section][1] - sectionBounds[section][0] + 1);
            }

            internal void InitiateSectionBounds()
            {
                int[] defBounds = new int[2] { -1, -1 };
                sectionBounds = new();

                for (int i = 0; i < Lines.Count; i++)
                {
                    string line = Lines[i];
                    if (line.IsNullOrWhiteSpace() || line.Length < 4) continue;
                    bool end = false;
                    if (line.Substring(0, 4) == "END ")
                    { end = true; line = line.Substring(4); }

                    if (sections.Contains(line))
                    {
                        if (!sectionBounds.ContainsKey(line))
                        { sectionBounds[line] = defBounds.ToArray(); }

                        if (sectionBounds.ContainsKey(line))
                        { sectionBounds[line][end ? 1 : 0] = end ? i - 1 : i + 1; }
                    }

                }
            }

            public Dictionary<string, int[]> sectionBounds
            {
                get;
                internal set;
            }

            private List<string> sections = new()
            {
            "CONDITIONAL LINKS",
            "ROOMS",
            "CREATURES",
            "BAT MIGRATION BLOCKAGES"
            };
        }

        public static List<RegionPreprocessor> regionPreprocessors;

        public delegate void RegionPreprocessor(RegionInfo info);

        public static bool? MSCCondition(string condition, RainWorldGame _)
        {
            if (condition != "MSC") return null;
            return ModManager.MSC;
        }

        public static bool? RegionExistsCondition(string condition, RainWorldGame _)
        {
            if (condition.StartsWith("region:") && condition.Length > "region:".Length)
            {
                return Region.GetFullRegionOrder().Contains(condition.Substring("region:".Length));
            }

            if (condition.Count() != 2) return null;
            return Region.GetFullRegionOrder().Contains(condition);
        }


        public static bool? ModIDCondition(string condition, RainWorldGame _)
        {
            if (condition[0] != '#') return null;
            condition = condition.Substring(1);

            foreach (ModManager.Mod mod in ModManager.ActiveMods)
            {
                if (mod.id == condition)
                {
                    return true;
                }
            }

            return false;
        }

        public static void InitializeBuiltinPreprocessors()
        {
            regionPreprocessors = new List<RegionPreprocessor>();

            regionPreprocessors.Add(ReplaceRoomPreprocessor.ReplaceRoom);
            regionPreprocessors.Add(IndexedEntranceClass.IndexedEntrance);

            WorldLoader.Preprocessing.preprocessorConditions.Add(MSCCondition);
            WorldLoader.Preprocessing.preprocessorConditions.Add(ModIDCondition);
            WorldLoader.Preprocessing.preprocessorConditions.Add(RegionExistsCondition); //this should be last to avoid any false detections
        }
    }
}
