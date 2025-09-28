using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomRegions.Mod
{
    public static class AbstractObjectGeneration
    {
        public static Dictionary<AbstractPhysicalObject.AbstractObjectType, AbstractObjectGenerator> AbstractObjectGenerators { get; internal set; } = new Dictionary<AbstractPhysicalObject.AbstractObjectType, AbstractObjectGenerator>();

        public delegate AbstractPhysicalObject AbstractObjectGenerator(World world, AbstractPhysicalObject.AbstractObjectType type, WorldCoordinate pos, EntityID id);

        public static void RegisterAbstractObjectGenerator(AbstractPhysicalObject.AbstractObjectType type, AbstractObjectGenerator createAbstractObject)
        {
            AbstractObjectGenerators[type] = createAbstractObject;
        }

        public static AbstractPhysicalObject GenerateDefaultObject(World world, AbstractPhysicalObject.AbstractObjectType type, WorldCoordinate pos)
        {
            var id = world.game.GetNewID();
            if (AbstractObjectGenerators.TryGetValue(type, out var del))
            {
                return del(world, type, pos, id);
            }

            if (AbstractConsumable.IsTypeConsumable(type))
            {
                return new AbstractConsumable(world, type, null, pos, world.game.GetNewID(), -1, -1, null);
            }
            if (type.index == -1) return null;
            return new AbstractPhysicalObject(world, type, null, pos, world.game.GetNewID());
        }

        public static AbstractPhysicalObject.AbstractObjectType FakeType(AbstractPhysicalObject obj)
        {
            if (obj is AbstractSpear s)
            {
                if (s.explosive) return FakeExplosiveSpear;
                if (s.electric) return FakeElectricSpear;
                if (s.hue != 0) return FakeHellSpear;
            }
            if (obj is AbstractCreature c)
            {
                if (c.creatureTemplate.type == CreatureTemplate.Type.Hazer) return FakeHazer;
                if (c.creatureTemplate.type == CreatureTemplate.Type.VultureGrub) return FakeVultureGrub;
            }
            if (obj is VultureMask.AbstractVultureMask m && m.king)
            {
                return FakeKingVultureMask;
            }
            return null;
        }

        public static AbstractPhysicalObject.AbstractObjectType FakeKingVultureMask = new("KingVultureMask", false);
        public static AbstractPhysicalObject.AbstractObjectType FakeExplosiveSpear = new("ExplosiveSpear", false);
        public static AbstractPhysicalObject.AbstractObjectType FakeElectricSpear = new("ElectricSpear", false);
        public static AbstractPhysicalObject.AbstractObjectType FakeHellSpear = new("HellSpear", false);
        public static AbstractPhysicalObject.AbstractObjectType FakePoisonSpear = new("PoisonSpear", false);
        public static AbstractPhysicalObject.AbstractObjectType FakeHazer = new("Hazer", false);
        public static AbstractPhysicalObject.AbstractObjectType FakeVultureGrub = new("VultureGrub", false);
        public static AbstractPhysicalObject.AbstractObjectType FakeRottenDangleFruit = new("RottenDangleFruit", false);
        public static AbstractPhysicalObject.AbstractObjectType FakeNone = new("None", false);

        internal static void RegisterVanillaAbstractObjectCreations()
        {
            AbstractObjectGenerators.Clear();

            RegisterAbstractObjectGenerator(FakeNone, (world, _, pos, id) => null);
            RegisterAbstractObjectGenerator(FakeKingVultureMask, (world, _, pos, id) => new VultureMask.AbstractVultureMask(world, null, pos, id, id.RandomSeed, true));
            RegisterAbstractObjectGenerator(FakeExplosiveSpear, (world, _, pos, id) => new AbstractSpear(world, null, pos, id, true));
            RegisterAbstractObjectGenerator(FakeElectricSpear, (world, _, pos, id) => new AbstractSpear(world, null, pos, id, false, true));
            RegisterAbstractObjectGenerator(FakeHellSpear, (world, _, pos, id) => new AbstractSpear(world, null, pos, id, true, Mathf.Lerp(0.35f, 0.6f, UnityEngine.Random.value)));
            RegisterAbstractObjectGenerator(FakePoisonSpear, (world, _, pos, id) => new AbstractSpear(world, null, pos, id, false)
            {
                poison = 1f,
                poisonHue = 0.3f + UnityEngine.Random.value * 0.6f //same hue range as Tardigrades                                                  
            }
                );
            RegisterAbstractObjectGenerator(FakeRottenDangleFruit, (world, _, pos, id) => new DangleFruit.AbstractDangleFruit(world, null, pos, id, -1, -1, true, null));
            RegisterAbstractObjectGenerator(FakeHazer, (world, _, pos, id) => new AbstractCreature(world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Hazer), null, pos, id));
            RegisterAbstractObjectGenerator(FakeVultureGrub, (world, _, pos, id) => new AbstractCreature(world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.VultureGrub), null, pos, id));
            RegisterAbstractObjectGenerator(FakeVultureGrub, (world, _, pos, id) => new AbstractCreature(world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.VultureGrub), null, pos, id));

            RegisterAbstractObjectGenerator(AbstractPhysicalObject.AbstractObjectType.Spear, (world, _, pos, id) => new AbstractSpear(world, null, pos, id, false));
            RegisterAbstractObjectGenerator(AbstractPhysicalObject.AbstractObjectType.DangleFruit, (world, _, pos, id) => new DangleFruit.AbstractDangleFruit(world, null, pos, id, -1, -1, false, null));
            RegisterAbstractObjectGenerator(AbstractPhysicalObject.AbstractObjectType.WaterNut, (world, _, pos, id) => new WaterNut.AbstractWaterNut(world, null, pos, id, -1, -1, null, false));
            RegisterAbstractObjectGenerator(AbstractPhysicalObject.AbstractObjectType.SporePlant, (world, _, pos, id) => new SporePlant.AbstractSporePlant(world, null, pos, id, -1, -1, null, false, true));
            RegisterAbstractObjectGenerator(AbstractPhysicalObject.AbstractObjectType.BubbleGrass, (world, _, pos, id) => new BubbleGrass.AbstractBubbleGrass(world, null, pos, id, 1f, -1, -1, null));
            RegisterAbstractObjectGenerator(AbstractPhysicalObject.AbstractObjectType.DataPearl, (world, type, pos, id) => new DataPearl.AbstractDataPearl(world, type, null, pos, id, -1, -1, null, DataPearl.AbstractDataPearl.DataPearlType.Misc));
            RegisterAbstractObjectGenerator(AbstractPhysicalObject.AbstractObjectType.PebblesPearl, (world, type, pos, id) => new DataPearl.AbstractDataPearl(world, type, null, pos, id, -1, -1, null, DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl));
            RegisterAbstractObjectGenerator(AbstractPhysicalObject.AbstractObjectType.VultureMask, (world, _, pos, id) => new VultureMask.AbstractVultureMask(world, null, pos, id, id.RandomSeed, false));

            if (ModManager.MSC)
            {
                RegisterAbstractObjectGenerator(MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.FireEgg, (world, _, pos, id) => new MoreSlugcats.FireEgg.AbstractBugEgg(world, null, pos, id, Mathf.Lerp(0.35f, 0.6f, RWCustom.Custom.ClampedRandomVariation(0.5f, 0.5f, 2f))));
                RegisterAbstractObjectGenerator(MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.JokeRifle, (world, _, pos, id) => new JokeRifle.AbstractRifle(world, null, pos, id, JokeRifle.AbstractRifle.AmmoType.Rock));
            }

            if (ModManager.DLCShared)
            {
                RegisterAbstractObjectGenerator(DLCSharedEnums.AbstractObjectType.LillyPuck, (world, _, pos, id) => new MoreSlugcats.LillyPuck.AbstractLillyPuck(world, null, pos, id, 3, -1, -1, null));
            }
        }
    }
}
