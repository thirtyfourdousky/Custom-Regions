using CustomRegions.Mod;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;

namespace CustomRegions.Progression
{
    internal static class SafariEnums
    {
        public static List<MultiplayerUnlocks.SafariUnlockID> CustomSafariUnlocks = new List<MultiplayerUnlocks.SafariUnlockID>();

        public static void Refresh()
        {
            Unregister();
            Register();
        }

        public static void ApplyHooks()
        {
            IL.Menu.MultiplayerMenu.Singal += MultiplayerMenu_Singal;
        }

        private static void MultiplayerMenu_Singal(MonoMod.Cil.ILContext il)
        {
            var c = new ILCursor(il);
            int hooks = 0;
            while (c.TryGotoNext(MoveType.After, x => x.MatchCallvirt<ArenaSetup.GameTypeSetup>("get_safariID"), 
                x => x.MatchCallvirt(typeof(List<string>).GetProperty("Item").GetGetMethod())))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((string orig, Menu.MultiplayerMenu self) => self.SafariUnlocksWithButtons[self.GetGameTypeSetup.safariID].value);
                hooks++;
            }
            if (hooks < 4)
                CustomRegionsMod.BepLogError($"failed to il hook MultiplayerMenu.Signal! hook count: [{hooks}].");
        }

        public static void Register()
        {
            CustomRegionsMod.CustomLog("\n[SAFARI UNLOCKS] CRS Registering safari unlocks...");
            if (Region.GetFullRegionOrder() == null) { return; }

            CustomStaticCache.CheckForRefresh();
            foreach (string regionName in CustomStaticCache.SafariRegions)
            {
                if (MultiplayerUnlocks.SafariUnlockID.values?.entries.Contains(regionName) ?? false)
                {
                    CustomRegionsMod.CustomLog($"[SAFARI UNLOCKS] region [{regionName}] already has safari unlock");
                    continue;
                }

                CustomRegionsMod.CustomLog($"[SAFARI UNLOCKS] unlock is found for [{regionName}]");
                CustomSafariUnlocks.Add(new MultiplayerUnlocks.SafariUnlockID(regionName, true));
            }
        }

        public static void Unregister()
        {
            foreach (MultiplayerUnlocks.SafariUnlockID unlock in CustomSafariUnlocks) { unlock?.Unregister(); }
            CustomSafariUnlocks = new List<MultiplayerUnlocks.SafariUnlockID>();
        }
    }
}
