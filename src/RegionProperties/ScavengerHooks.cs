using CustomRegions.Mod;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using UnityEngine;
using static CustomRegions.RegionProperties.ScavengerHooks;
using static CustomRegions.Mod.AbstractObjectGeneration;

namespace CustomRegions.RegionProperties
{
    internal static class ScavengerHooks
    {
        public static void ApplyHooks()
        {

            IL.ScavengerAbstractAI.TryAssembleSquad += ScavengerAbstractAI_TryAssembleSquad;
            On.ScavengerAbstractAI.UpdateMissionAppropriateGear += ScavengerAbstractAI_UpdateMissionAppropriateGear;
            On.ScavengerAbstractAI.TradeItem += ScavengerAbstractAI_TradeItem;
            On.ScavengerAI.CollectScore_PhysicalObject_bool += ScavengerAI_CollectScore_PhysicalObject_bool;
            IL.ScavengerTreasury.ctor += ScavengerTreasury_ctor;
            IL.ScavengerAbstractAI.InitGearUp += ScavengerAbstractAI_InitGearUp;
            On.AbstractCreature.IsEnteringDen += AbstractCreature_IsEnteringDen;
            On.ScavengerAI.WeaponScore += ScavengerAI_WeaponScore;
            On.Scavenger.Throw += Scavenger_Throw;
            IL.ItemTracker.Update += ItemTracker_Update;
        }

        private static void ItemTracker_Update(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchIsinst<AbstractCreature>()))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((AbstractCreature c, ItemTracker self) =>
                {
                    if (c == null || self.AI is ScavengerAI && (c.creatureTemplate.type == CreatureTemplate.Type.Hazer || c.creatureTemplate.type == CreatureTemplate.Type.VultureGrub)) return null;
                    return c;
                });
            }
            else
            {
                CustomRegionsMod.BepLogError($"CustomRegions.RegionProperties.ScavengerHooks.ItemTracker_Update: IL Hook failed.");
            }
        }

        private static void Scavenger_Throw(On.Scavenger.orig_Throw orig, Scavenger self, Vector2 throwDir)
        {
            if (self.grasps[0].grabbed is Hazer hazer)
            { hazer.tossed = true; }

            if (self.grasps[0].grabbed is VultureGrub grub)
            { grub.InitiateSignalCountDown(); }

            orig(self, throwDir);
        }

        private static int ScavengerAI_WeaponScore(On.ScavengerAI.orig_WeaponScore orig, ScavengerAI self, PhysicalObject obj, bool pickupDropInsteadOfWeaponSelection, bool reallyWantsSpear)
        {
            var dict = self.scavenger.room?.world.region?.GetCRSProperties().scavScoreItems;
            if (dict != null && obj is Creature c)
            {
                if (c.dead) return 0;
                if (c.Template.type == CreatureTemplate.Type.Hazer && dict.ContainsKey(FakeHazer) && dict[FakeHazer] > 0)
                {
                    if (!pickupDropInsteadOfWeaponSelection && self.focusCreature != null && self.focusCreature.representedCreature.creatureTemplate.type != CreatureTemplate.Type.Slugcat && self.focusCreature.representedCreature.creatureTemplate.visualRadius == 0) return 0;
                    if (self.currentViolenceType == ScavengerAI.ViolenceType.Lethal) return 1;
                    return 3;
                }
                if (c.Template.type == CreatureTemplate.Type.VultureGrub && dict.ContainsKey(FakeVultureGrub) && dict[FakeVultureGrub] > 0)
                {
                    if (!pickupDropInsteadOfWeaponSelection && !self.creature.Room.AnySkyAccess) return 0;
                    if (self.currentViolenceType == ScavengerAI.ViolenceType.NonLethal) return 1;
                    return 3;
                }
            }
            return orig(self, obj, pickupDropInsteadOfWeaponSelection, reallyWantsSpear);
        }

        private static void AbstractCreature_IsEnteringDen(On.AbstractCreature.orig_IsEnteringDen orig, AbstractCreature self, WorldCoordinate den)
        {
            orig(self, den);

            if (self.creatureTemplate.TopAncestor().type != CreatureTemplate.Type.Scavenger) return;

            for (int i = self.stuckObjects.Count - 1; i >= 0; i--)
            {
                if (i >= self.stuckObjects.Count || self.stuckObjects[i] is not AbstractPhysicalObject.CreatureGripStick || self.stuckObjects[i].A != self)
                { continue; }
                if (self.stuckObjects[i].B is AbstractCreature creature && (creature.creatureTemplate.type == CreatureTemplate.Type.Hazer || creature.creatureTemplate.type == CreatureTemplate.Type.VultureGrub))
                {
                    creature.state.alive = true;
                }
            }
        }

        private static void ScavengerAbstractAI_InitGearUp(ILContext il)
        {
            var c = new ILCursor(il);
            int index = 0;
            if (c.TryGotoNext(MoveType.AfterLabel,
                x => x.MatchLdloc(out index),
                x => x.MatchLdcI4(0),
                x => x.MatchBlt(out _),
                x => x.MatchCall(typeof(UnityEngine.Random), "get_value"),
                x => x.MatchLdcR4(0.6f)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, index);
                c.EmitDelegate((ScavengerAbstractAI self, int i) =>
                {
                    var p = self.parent.world.region?.GetCRSProperties();
                    if (p == null) return i;
                    bool elite = self.parent.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.ScavengerElite;
                    var items = elite ? p.eliteScavGearItems : p.scavGearItems;
                    var room = self.world.GetAbstractRoom(self.parent.pos);

                    if (items == null || i < 0) return i;
                    foreach (AbstractPhysicalObject.AbstractObjectType type in items.Keys)
                    {
                        if (UnityEngine.Random.value < items[type])
                        {
                            var obj = GenerateDefaultObject(room.world, type, self.parent.pos);
                            if (obj == null) continue;

                            room.AddEntity(obj);
                            new AbstractPhysicalObject.CreatureGripStick(self.parent, obj, i, true);
                            i--;
                            if (i < 0) break;
                        }
                    }
                    return -1;
                });
                c.Emit(OpCodes.Stloc, index);
            }
            else
            {
                CustomRegionsMod.BepLogError($"CustomRegions.RegionProperties.ScavengerHooks.ScavengerAbstractAI_InitGearUp: IL Hook failed.");
            }
        }
        private static bool SpecialRequirements(ScavengerAI self, PhysicalObject obj)
        {
            if (self.scavenger.room != null)
            {
                SocialEventRecognizer.OwnedItemOnGround ownedItemOnGround = self.scavenger.room.socialEventRecognizer.ItemOwnership(obj);
                if (ownedItemOnGround != null && ownedItemOnGround.offeredTo != null && ownedItemOnGround.offeredTo != self.scavenger)
                {
                    return false;
                }
            }
            if (obj is Spear)
            {
                if (ModManager.MMF && MoreSlugcats.MMF.cfgHunterBackspearProtect.Value && (obj as Spear).onPlayerBack)
                    return false;

                if ((obj as Spear).mode == Weapon.Mode.StuckInWall)
                    return false;

                if (obj is ExplosiveSpear e && e.Ignited)
                    return false;

                else if (obj is MoreSlugcats.ElectricSpear l && l.abstractSpear.electricCharge >= 0)
                    return false;
            }
            else if (obj is FirecrackerPlant f && f.fuseCounter != 0)
                return false;

            else if (obj is JellyFish j && j.electricCounter >= 1)
                return false;

            else if (obj is SporePlant p && !p.UsableAsWeapon)
                return false;

            else if (obj is MoreSlugcats.LillyPuck l && l.BitesLeft != 3)
                return false;

            else if (obj is Hazer h && h.dead)
                return false;

            else if (obj is VultureGrub g && g.dead)
                return false;

            return true;
        }

        private static int ScavengerAI_CollectScore_PhysicalObject_bool(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
        {
            var dict = self.scavenger.room?.world.region?.GetCRSProperties().scavScoreItems;
            var type = FakeType(obj?.abstractPhysicalObject) ?? obj?.abstractPhysicalObject?.type;
            if (!(weaponFiltered && self.NeedAWeapon) && dict != null && type != null && dict.ContainsKey(type) && SpecialRequirements(self, obj))
            {
                return (int)dict[type];
            }

            return orig(self, obj, weaponFiltered);
        }

        private static AbstractPhysicalObject ScavengerAbstractAI_TradeItem(On.ScavengerAbstractAI.orig_TradeItem orig, ScavengerAbstractAI self, bool main)
        {
            if (main)
            {
                if (self.world?.region?.GetCRSProperties().scavMainTradeItem is AbstractPhysicalObject.AbstractObjectType type)
                {
                    var obj = GenerateDefaultObject(self.world, type, self.parent.pos);
                    if (obj != null) return obj;
                }
            }
            else
            {
                if (self.world?.region?.GetCRSProperties().scavTradeItems != null)
                {
                    foreach (KeyValuePair<AbstractPhysicalObject.AbstractObjectType, float> pair in self.world?.region?.GetCRSProperties().scavTradeItems)
                    {
                        if (UnityEngine.Random.value < pair.Value)
                        {
                            var obj = GenerateDefaultObject(self.world, pair.Key, self.parent.pos);
                            if (obj != null) return obj;
                        }
                    }
                }
            }
            return orig(self, main);

        }

        private static void ScavengerAbstractAI_UpdateMissionAppropriateGear(On.ScavengerAbstractAI.orig_UpdateMissionAppropriateGear orig, ScavengerAbstractAI self)
        {
            orig(self);

            if (self.squad == null || self.squad.missionType != ScavengerAbstractAI.ScavengerSquad.MissionID.Trade)
            {
                return;
            }
            if (self.world?.region?.GetCRSProperties().scavMainTradeItem is not AbstractPhysicalObject.AbstractObjectType type)
            {
                return;
            }
            self.missionAppropriateGear = false;
            foreach (AbstractPhysicalObject.AbstractObjectStick stick in self.parent.stuckObjects)
            {
                if (stick is AbstractPhysicalObject.CreatureGripStick && stick.A == self.parent && stick.B.type == type)
                {
                    self.missionAppropriateGear = true;
                    return;
                }
            }
        }

        private static void ScavengerAbstractAI_TryAssembleSquad(ILContext il)
        {
            var c = new ILCursor(il);
            int loc = 2;
            int loc2 = 2;
            ILLabel label = null;
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out loc),
                x => x.MatchLdcI4(2),
                x => x.MatchBge(out label)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, loc);
                c.EmitDelegate((ScavengerAbstractAI self, int scavsInDen) =>
                {
                    int? min = self.world.region?.GetCRSProperties().minScavSquad;
                    if (min is null) return false;
                    return scavsInDen < min - 1;
                });
                c.Emit(OpCodes.Brfalse, label);
            }
            else
            {
                CustomRegionsMod.BepLogError($"CustomRegions.RegionProperties.ScavengerHooks.ScavengerAbstractAI_TryAssembleSquad: IL Hook Part 1 failed.");
            }

            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out loc),
                x => x.MatchLdcI4(2),
                x => x.MatchLdloc(out loc2),
                x => x.MatchCall(typeof(UnityEngine.Random), "Range"),
                x => x.MatchCall(typeof(Math), "Min")
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, loc);
                c.Emit(OpCodes.Ldloc, loc2);
                c.EmitDelegate((int orig, ScavengerAbstractAI self, int scavsInDen, int origMax) =>
                {
                    int? min = self.world.region?.GetCRSProperties().minScavSquad;
                    int? max = self.world.region?.GetCRSProperties().maxScavSquad;
                    if (min != null || max != null)
                    { return Math.Min(scavsInDen, UnityEngine.Random.Range(min ?? 3, max ?? origMax + 1) - 1); }
                    return orig;
                });
            }
            else
            {
                CustomRegionsMod.BepLogError($"CustomRegions.RegionProperties.ScavengerHooks.ScavengerAbstractAI_TryAssembleSquad: IL Hook Part 2 failed.");
            }
        }

        private static void ScavengerTreasury_ctor(ILContext il)
        {
            var c = new ILCursor(il);
            int index = 8;
            if (c.TryGotoNext(MoveType.AfterLabel,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<ScavengerTreasury>(nameof(ScavengerTreasury.property)),
                x => x.MatchLdloc(out index)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloca, index);
                c.Emit(OpCodes.Ldloc, index - 1);
                c.EmitDelegate((ScavengerTreasury self, ref AbstractPhysicalObject obj, int i) =>
                {
                    var items = self.room.world.region?.GetCRSProperties().scavTreasuryItems;
                    if (items != null)
                    {
                        obj = null;
                        foreach (AbstractPhysicalObject.AbstractObjectType type in items.Keys)
                        {
                            if (UnityEngine.Random.value < items[type])
                            {
                                obj = GenerateDefaultObject(self.room.world, type, self.room.GetWorldCoordinate(self.tiles[i]));
                                break;
                            }
                        }
                    }

                });
            }
            else
            {
                CustomRegionsMod.BepLogError($"CustomRegions.RegionProperties.ScavengerHooks.ScavengerTreasury_ctor: IL Hook failed.");
            }
        }
    }
}
