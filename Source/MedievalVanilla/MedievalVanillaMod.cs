using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace MedievalVanilla
{
    public class MedievalVanillaMod : Mod
    {
        public static System.Random random = new System.Random(Guid.NewGuid().GetHashCode());
        public static TechLevel MAX_TECHLEVEL = TechLevel.Medieval;

        public MedievalVanillaMod(ModContentPack content) : base(content)
        {
            Harmony harmony = new Harmony("medieval.vanilla");
            
            //harmony.Patch(AccessTools.Method(typeof(Verse.Pawn), nameof(Verse.Pawn.ButcherProducts)), null,
            //new HarmonyMethod(typeof(MedievalVanillaMod), nameof(ButcherProducts_PostFix)), null);

            //harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.CaravanEnterMapUtility), nameof(RimWorld.Planet.CaravanEnterMapUtility.Enter)), null,
            //new HarmonyMethod(typeof(ModStuff), nameof(Enter_PostFix)), null);
        }

        //static void Enter_PostFix(Caravan caravan, Map map, CaravanEnterMode enterMode)
        //{
        //    if (map.fogGrid.IsFogged(caravan.pawns[0].DrawPos.ToIntVec3()))
        //    {
        //        // Something went wrong with map gen. 
        //        Log.Error("MapGen Error. RH_TET Mod is attempting to recover.");
        //        map.fogGrid.Unfog(caravan.pawns[0].DrawPos.ToIntVec3());
        //    }
        //}

        //static void ButcherProducts_PostFix(Verse.Pawn __instance, ref IEnumerable<Thing> __result, float efficiency)
        //{
        //    // Setting fat amount to 1/3 the meat, divided by two, reduced by efficiency number.
        //    int fatAmount = GenMath.RoundRandom(__instance.GetStatValue(DefDatabase<StatDef>.GetNamed("MeatAmount", true), true) * .33F * efficiency);
        //    if (fatAmount > 0)
        //    {
        //        List<Thing> NewList = new List<Thing>();
        //        foreach (Thing entry in __result)
        //        {
        //            NewList.Add(entry);
        //        }
        //        Thing animalFatStack = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RH_TET_AnimalFat"), null);
        //        animalFatStack.stackCount = fatAmount;
        //        NewList.Add(animalFatStack);
                
        //        IEnumerable<Thing> output = NewList;
        //        __result = output;
        //    }
        //}
    }
}