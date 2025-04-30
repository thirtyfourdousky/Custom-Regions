using CustomRegions.Mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using static CustomRegions.CustomWorld.RegionPreprocessors;
using MonoMod.Cil;

namespace CustomRegions.CustomWorld
{
    internal static class ReplaceRoomPreprocessor
    {
        public static void Apply()
        {
            On.WorldLoader.LoadAbstractRoom += WorldLoader_LoadAbstractRoom;
            try
            {
                IL.RoomCamera.MoveCamera_int += RoomNameILHook;
                IL.RoomCamera.MoveCamera_Room_int += RoomNameILHook;
                IL.RoomCamera.PreLoadTexture += RoomNameILHook;
                IL.RoomPreparer.ctor += RoomNameILHook;
                IL.Room.ctor += RoomNameILHook;
            }
            catch (Exception e) { CustomRegionsMod.BepLogError($"ReplaceRoom IL hook error!\n" + e); }
        }

        private static void RoomNameILHook(ILContext il)
        {
            int count = 0;
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.Before, x => x.MatchLdfld<AbstractRoom>("name")))
            {
                cursor.MoveAfterLabels();
                cursor.EmitDelegate((AbstractRoom room) => room.AltFileName().Value);
                cursor.Remove();
                count++;
            }
            if(count == 0)
            {
                CustomRegionsMod.BepLogError($"CustomRegions.CustomWorld.ReplaceRoomPreprocessor.RoomNameILHook: IL Hook failed for [{il.Method.Name}].");
            }
        }

        private static void WorldLoader_LoadAbstractRoom(On.WorldLoader.orig_LoadAbstractRoom orig, World world, string roomName, AbstractRoom room, RainWorldGame.SetupValues setupValues)
        {
            if (WorldLoaderReplaceRooms(world.name).ContainsKey(roomName))
            {
                room.AltFileName().Value = WorldLoaderReplaceRooms(world.name)[roomName];
                roomName = room.AltFileName().Value;
            }

            orig(world, roomName, room, setupValues);
        }

        private static readonly ConditionalWeakTable<AbstractRoom, StrongBox<string>> _AltFileName = new();
        public static StrongBox<string> AltFileName(this AbstractRoom p) => _AltFileName.GetValue(p, p => new(p.name));


        private static readonly ConditionalWeakTable<string, Dictionary<string, string>> _LoaderReplaceRooms = new();
        public static Dictionary<string, string> WorldLoaderReplaceRooms(string name)
        {
            return _LoaderReplaceRooms.GetValue(name, _ => new());
        }

        public static void ReplaceRoom(RegionInfo info)
        {
            WorldLoaderReplaceRooms(info.RegionID).Clear();

            string CL = "CONDITIONAL LINKS";

            for (int i = 0; i < info.LinesSection(CL)?.Count; i++)
            {
                if (!string.IsNullOrEmpty(info.LinesSection(CL)[i]))
                {
                    string[] array = Regex.Split(info.LinesSection(CL)[i], " : ");
                    if (array.Length >= 4 && array[1] == "REPLACEROOM" && WorldLoader.Preprocessing.TimelineMatch(array[0], info.timeline))
                    {
                        CustomRegionsMod.CustomLog($"adding line [{string.Join(" : ", array)}]");
                        WorldLoaderReplaceRooms(info.RegionID).Add(array[2], array[3]);
                        //info.LinesSection(CL)[i] = "//";
                        info.Lines[i + info.sectionBounds[CL][0]] = "//";
                    }
                }
            }
            info.Lines.RemoveAll(str => str == "//");
            return;
        }
    }
}
