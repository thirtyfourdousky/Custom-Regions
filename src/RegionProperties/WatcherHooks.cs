using CustomRegions.Mod;

namespace CustomRegions.RegionProperties
{
    internal static class WatcherHooks
    {
        public static void ApplyHooks()
        {
            On.Region.IsWatcherVanillaRegion += Region_IsWatcherVanillaRegion;
            On.Region.IsVanillaSentientRotRegion += Region_IsVanillaSentientRotRegion;
            On.Region.HasSentientRotResistance += Region_HasSentientRotResistance;
            On.Watcher.WatcherRoomSpecificScript.AddRoomSpecificScript += WatcherRoomSpecificScript_AddRoomSpecificScript;
        }

        private static void WatcherRoomSpecificScript_AddRoomSpecificScript(On.Watcher.WatcherRoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            orig(room);
            var props = room.world?.region?.GetCRSProperties();
            if (props != null && room.abstractRoom.firstTimeRealized && props.watcherSentientRotRooms.Contains(room.abstractRoom.name))
            {
                room.AddObject(new Watcher.WatcherRoomSpecificScript.InfectSentientRot(room));
            }
        }

        private static bool Region_HasSentientRotResistance(On.Region.orig_HasSentientRotResistance orig, string name)
        {
            return orig(name) || CustomStaticCache.WatcherRotImmuneRegions.Contains(name.ToLowerInvariant());
        }

        private static bool Region_IsVanillaSentientRotRegion(On.Region.orig_IsVanillaSentientRotRegion orig, string name)
        {
            return orig(name) || CustomStaticCache.WatcherSentientRotRegions.Contains(name.ToLowerInvariant());
        }

        private static bool Region_IsWatcherVanillaRegion(On.Region.orig_IsWatcherVanillaRegion orig, string name)
        {
            return orig(name) || CustomStaticCache.WatcherVanillaRegions.Contains(name.ToLowerInvariant());
        }
    }
}
