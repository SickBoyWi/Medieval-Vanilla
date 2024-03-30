using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimWorld.QuestGen;
//using SRTS;
using UnityEngine;
using Verse;

namespace MedievalVanilla
{
    [StaticConstructorOnStartup]
    public static class RemovePostMedievalHarmony
    {
        private const int START_DATE = 2519;

        static RemovePostMedievalHarmony()
        {
            Harmony harmony = new Harmony("rimworld.removepostmedieval");

            harmony.Patch(AccessTools.Method(typeof(PawnUtility), "IsTravelingInTransportPodWorldObject"),
                new HarmonyMethod(typeof(RemovePostMedievalHarmony), nameof(IsTravelingInTransportPodWorldObject)));

            //Changes the starting date of RimWorld.
            //harmony.Patch(AccessTools.Property(typeof(TickManager), "StartingYear").GetGetMethod(), null,
            //    new HarmonyMethod(typeof(RemovePostMedievalHarmony), nameof(StartingYear_PostFix)), null);
            //harmony.Patch(AccessTools.Method(typeof(GenDate), "Year"), null,
            //    new HarmonyMethod(typeof(RemovePostMedievalHarmony), nameof(Year_PostFix)), null);

            // Fix item collection generation.
            //foreach (Type type in typeof(ThingSetMaker).AllSubclassesNonAbstract())
            //{ 
            harmony.Patch(original: AccessTools.Method(
                type: typeof(ThingSetMaker),
                name: "Generate",
                parameters: new[] { typeof(ThingSetMakerParams) }),
                prefix: new HarmonyMethod(typeof(RemovePostMedievalHarmony), nameof(ItemCollectionGeneratorGeneratePrefix)),
                postfix: new HarmonyMethod(typeof(RemovePostMedievalHarmony), nameof(ItemCollectionGeneratorGeneratePostfix)));
            //}

            // Fix meteorite falling with nonsuitable materials.
            harmony.Patch(original: AccessTools.Method(
                    type: typeof(ThingSetMaker_Meteorite),
                    name: "FindRandomMineableDef",
                    parameters: null),
                    prefix: null,
                    postfix: new HarmonyMethod(typeof(RemovePostMedievalHarmony), nameof(FindRandomMineableGeneratorGeneratePostfix)));

            // Fix pawn generation with nonsuitable materials.
            harmony.Patch(original: AccessTools.Method(
                    type: typeof(PawnGenerator),
                    name: "GeneratePawn",
                    parameters: new[] { typeof(PawnGenerationRequest) }),
                    //prefix: new HarmonyMethod(type: typeof(RemovePostMedievalHarmony), name: nameof(PawnGeneratorGeneratePawnPrefix)),
                    prefix: null,
                    postfix: new HarmonyMethod(typeof(RemovePostMedievalHarmony), nameof(PawnGeneratorGeneratePawnPostfix)));

            IEnumerable<MethodInfo> mis = AgeInjuryUtilityNamesHandler();
            if (mis != null && mis.Any())
            {
                foreach (MethodInfo mi in mis)
                {
                    harmony.Patch(original: mi,
                                  prefix: null,
                                  postfix: new HarmonyMethod(typeof(RemovePostMedievalHarmony), nameof(RandomPermanentInjuryDamageTypePostfix)));
                }
            }

            harmony.Patch(original: AccessTools.Method(
                type: typeof(PawnUtility),
                name: "IsTravelingInTransportPodWorldObject",
                parameters: null),
                prefix: new HarmonyMethod(typeof(RemovePostMedievalHarmony), nameof(PawnUtility_IsTravelingInPod_Prefix)),
                postfix: null);

            //harmony.Patch(original: AccessTools.Method(
            //    type: typeof(CacheData),
            //    name: "GetApparelFromApparelProps",
            //    parameters: null),
            //    prefix: new HarmonyMethod(typeof(RemovePostMedievalHarmony), nameof(AlienRaceCacheDataGetApparelFromApparelPropsfix)),
            //    postfix: null);

        }

        //[HarmonyPriority(2000)]
        //public static bool AlienRaceCacheDataGetApparelFromApparelPropsfix(ApparelProperties __instance, Pawn pawn, ref bool __result)
        //{

        //    if (!ModsConfig.IsActive("SickBoyWi.TheEndTimes.Skaven"))
        //    { 
        //        if (!apparelPropsToApparelDict.ContainsKey(props))
        //            apparelPropsToApparelDict.Add(props, DefDatabase<ThingDef>.AllDefsListForReading.First(td => td.apparel == props));

        //        return apparelPropsToApparelDict[props];
        //    }

        //    return true;
        //}

        [HarmonyPriority(2000)]
        public static bool PawnUtility_IsTravelingInPod_Prefix(Pawn pawn, ref bool __result)
        {
            if (!pawn.IsColonist && (pawn.Faction == null || !pawn.Faction.IsPlayer))
                return true;
            __result = pawn.IsWorldPawn() && ThingOwnerUtility.AnyParentIs<ActiveDropPodInfo>((Thing)pawn);
            return false;
        }

        // Make sure pawns that are generated do not have improper clothing, or clothing made of improper items (synthread/hyperweave). 
        private static void PawnGeneratorGeneratePawnPostfix(ref PawnGenerationRequest request, ref Pawn __result)
        {
            if (__result != null)
            {
                if (__result.kindDef != null && (__result.kindDef.defName.Contains("Space") || __result.kindDef.defName.Equals("Villager") ))
                {
                    if (Find.WorldPawns.Contains(__result))
                        Find.WorldPawns.RemoveAndDiscardPawnViaGC(__result);

                    Slate slate = RimWorld.QuestGen.QuestGen.slate;
                    Gender? fixedGender = new Gender?();
                    ref PawnGenerationRequest local = ref request;
                    if (!slate.TryGet<PawnGenerationRequest>("overridePawnGenParams", out local, false))
                        request = new PawnGenerationRequest(PawnKindDefOf.Slave, (Faction)null, PawnGenerationContext.NonPlayer, -1, true, false, false, true, false, 20f, true, false, true, true, true, false, false, false, false, 0.0f, 0.0f, (Pawn)null, 1f, (Predicate<Pawn>)null, (Predicate<Pawn>)null, (IEnumerable<TraitDef>)null, (IEnumerable<TraitDef>)null, new float?(), new float?(), new float?(), fixedGender);
                    Pawn pawn = PawnGenerator.GeneratePawn(request);
                    if (!pawn.IsWorldPawn())
                        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
                    __result = pawn;
                }
                else if (__result.apparel != null && __result.apparel.WornApparelCount > 0)
                {
                    foreach (Apparel apparel in __result.apparel.WornApparel)
                    {
                        if (apparel != null && apparel.Stuff != null && apparel.Stuff.defName != null)
                        { 
                            switch (apparel.Stuff.defName)
                            {
                                case "Synthread":
                                case "Hyperweave":
                                    //Replace with devilstrand since it's an expensive textile as well.
                                    apparel.SetStuffDirect(ThingDef.Named("DevilstrandCloth"));
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        // Replace any meteors of plasteel, components, or uranium with something more fitting.
        private static void FindRandomMineableGeneratorGeneratePostfix(ThingSetMaker __instance, ref ThingDef __result)
        {
            bool replaceInd = false;

            switch (__result.defName)
            {
                case "MineableComponentsIndustrial":
                    replaceInd = true;
                    break;
                case "MineableUranium":
                    if (!Settings.IncludeUranium)
                        replaceInd = true;
                    break;
                case "MineablePlasteel":
                    if (!Settings.IncludePlasteel)
                        replaceInd = true;
                    break;
                default:
                    replaceInd = false;
                    break;
            }

            if (replaceInd)
            {
                __result = CORE_FindRandomMineableDef();

                FindRandomMineableGeneratorGeneratePostfix(__instance, ref __result);
            }
        }

        private static ThingDef CORE_FindRandomMineableDef()
        {
            return (from x in ThingSetMaker_Meteorite.nonSmoothedMineables
                    where x.building.isResourceRock && x.building.mineableThing.BaseMarketValue >= 5f
                    select x).RandomElement<ThingDef>();
        }

        ////TickManager
        //public static void StartingYear_PostFix(ref int __result)
        //{
        //    __result = START_DATE; // The year that the end times started.
        //}

        ////GenDate
        //public static void Year_PostFix(long absTicks, float longitude, ref int __result)
        //{
        //    long num = absTicks + ((long)GenDate.TimeZoneAt(longitude) * 2500L);
        //    __result = START_DATE + Mathf.FloorToInt((float)num / 3600000f);
        //}

        ////GenDate
        //public static void DateFullStringAt_PostFix(long absTicks, Vector2 location, ref string __result)
        //{
        //    int num = GenDate.DayOfSeason(absTicks, location.x) + 1;
        //    string value = Find.ActiveLanguageWorker.OrdinalNumber(num, Gender.None);
        //    __result = "TET_FullDate".Translate(value, GenDate.Quadrum(absTicks, location.x).Label(), GenDate.Year(absTicks, location.x), num);
        //}

        ////GenDate
        //public static void DateReadoutStringAt_PostFix(long absTicks, Vector2 location, ref string __result)
        //{
        //    int num = GenDate.DayOfSeason(absTicks, location.x) + 1;
        //    string value = Find.ActiveLanguageWorker.OrdinalNumber(num, Gender.None);
        //    __result = "TET_DateReadout".Translate(value, GenDate.Quadrum(absTicks, location.x).Label(), GenDate.Year(absTicks, location.x), num);
        //}

        public static IEnumerable<MethodInfo> AgeInjuryUtilityNamesHandler()
        {
            try
            { 
                //Log.Message("Looking for AgeInjuryUtility...");
                return (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                    from type in assembly.GetTypes()
                                    from method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                    where method.Name == "RandomPermanentInjuryDamageType"
                                    select method);
            }
            catch
            {
                Log.Warning("Squashing an error in MedievalVanilla.RemovePostMedievalHarmony.AgeInjuryUtilityNamesHandler. Pawns may have injuries that aren't medieval appropriate because of this failure, but it won't break anything.");
            }

            return null;
        }

        public static void RandomPermanentInjuryDamageTypePostfix(ref DamageDef __result)
        {
            if (__result == DamageDefOf.Bullet) {
                __result = DamageDefOf.Scratch;
            }
        }

        public static void ItemCollectionGeneratorGeneratePrefix(ref ThingSetMakerParams parms)
        {
            if (!parms.techLevel.HasValue || parms.techLevel > MedievalVanillaMod.MAX_TECHLEVEL)
                parms.techLevel = MedievalVanillaMod.MAX_TECHLEVEL;
        }

        public static void ItemCollectionGeneratorGeneratePostfix(ThingSetMaker __instance, ref ThingSetMakerParams parms, ref List<Thing> __result)
        {
            bool artifact = false;
            if (__result.NullOrEmpty())
            {
                artifact = true;
                if (__result == null)
                    __result = new List<Thing>();
            }

            // Manually remove future items. Replace with new stuff.
            List <Thing> tempList = new List<Thing>();
            int foodRemovedCount = 0;
            
            foreach (Thing thing in __result)
            {
                if (thing != null)
                { 
                    Thing tempThing = thing.GetInnerIfMinified();
                    foodRemovedCount = 0;

                    switch (tempThing.def.defName)
                    {
                        case "Hyperweave":
                            tempList.Add(thing);
                            break;
                        case "Neutroamine":
                            tempList.Add(thing);
                            break;
                        case "SunLamp":
                            tempList.Add(thing);
                            break;
                        case "Apparel_FlakPants":
                            tempList.Add(thing);
                            break;
                        case "Apparel_FlakJacket":
                            tempList.Add(thing);
                            break;
                        case "SmokepopBelt":
                            tempList.Add(thing);
                            break;
                        case "Apparel_PsychicFoilHelmet":
                            tempList.Add(thing);
                            break;
                        case "Apparel_ShieldBelt":
                            tempList.Add(thing);
                            break;
                        case "PsychicEmanator":
                            tempList.Add(thing);
                            break;
                        case "VanometricPowerCell":
                            tempList.Add(thing);
                            break;
                        case "InfiniteChemreactor":
                            tempList.Add(thing);
                            break;
                        case "TubeTelevision":
                            tempList.Add(thing);
                            break;
                        case "FlatscreenTelevision":
                            tempList.Add(thing);
                            break; 
                        case "MegascreenTelevision":
                            tempList.Add(thing);
                            break;
                        case "Telescope":
                            tempList.Add(thing);
                            break; 
                        case "ComponentSpacer":
                            tempList.Add(thing);
                            break;
                        case "Uranium":
                            if (!Settings.IncludeUranium)
                                tempList.Add(thing);
                            break;
                        case "Plasteel":
                            if (!Settings.IncludePlasteel)
                                tempList.Add(thing);
                            break;
                        case "Synthread":
                            tempList.Add(thing);
                            break;
                        case "OrbitalTargeterBombardment":
                            tempList.Add(thing);
                            break;
                        case "OrbitalTargeterPowerBeam":
                            tempList.Add(thing);
                            break;
                        case "TornadoGenerator":
                            tempList.Add(thing);
                            break;
                        case "Apparel_PowerArmorHelmet":
                            tempList.Add(thing);
                            break;
                        case "Joywire":
                            tempList.Add(thing);
                            break;
                        case "Painstopper":
                            tempList.Add(thing);
                            break;
                        case "MealSurvivalPack":
                            tempList.Add(thing);
                            foodRemovedCount = thing.stackCount;
                            break;
                        case "MedicineUltratech":
                            tempList.Add(thing);
                            foodRemovedCount = thing.stackCount;
                            break;
                        default:
                            break;
                    }               
                }
            }
            
            if (!tempList.NullOrEmpty())
            {
                int listCount = tempList.Count;

                foreach (Thing thing in tempList)
                {
                    __result.Remove(thing);
                }
                
                if (foodRemovedCount > 0)
                {
                    __result.Add(GenerateFoodReplacementMeals(foodRemovedCount));
                    listCount--;
                }

                for (int i = 0; i < listCount; i++)
                {
                    __result.AddRange(GenerateRandomFuturisticItemReplacement());
                }                
            }
            else if (artifact)
            {
                int listCount = MedievalVanillaMod.random.Next(2, 4);
                
                for (int i = 0; i < listCount; i++)
                {
                    //__result.AddRange(GenerateRandomArtifact());
                    __result.AddRange(GenerateRandomFuturisticItemReplacement());
                }
            }

            // Check all items for future materials.
            tempList = new List<Thing>();
            List<Thing> replacementList = new List<Thing>();

            // Be sure nothing is made of uranium, hyperweave, or plasteel.
            foreach (Thing thing in __result)
            {
                Thing tempThing = thing.GetInnerIfMinified();
                
                if (tempThing.Stuff != null)
                {
                    if ((tempThing.Stuff.defName.Equals("Uranium") && !Settings.IncludeUranium) || (tempThing.Stuff.defName.Equals("Plasteel") && !Settings.IncludePlasteel) || tempThing.Stuff.defName.Equals("Hyperweave") || tempThing.Stuff.defName.Equals("Synthread"))
                    {
                        if ((tempThing.Stuff.defName.Equals("Uranium") && !Settings.IncludeUranium) || !tempThing.Stuff.defName.Equals("Uranium"))
                        { 
                            int rInt = MedievalVanillaMod.random.Next(0, 10);

                            ThingDef stuff = ThingDefOf.Gold;

                            if (tempThing.Stuff.defName.Equals("Uranium") || (tempThing.Stuff.defName.Equals("Plasteel") && !Settings.IncludePlasteel))
                            {
                                if (rInt <= 1)
                                {
                                    stuff = ThingDefOf.Gold;
                                }
                                else if (rInt <= 2)
                                {
                                    stuff = ThingDefOf.Silver;
                                }
                            }
                            else
                            {
                                stuff = ThingDef.Named("WoolMegasloth");
                                if (rInt <= 1)
                                {
                                    stuff = ThingDef.Named("Leather_Thrumbo");
                                }
                                else if (rInt <= 3)
                                {
                                    stuff = ThingDef.Named("DevilstrandCloth");
                                }
                            }
                        
                            Thing replacementThing = ThingMaker.MakeThing(tempThing.def, stuff);
                            CompQuality compQuality = tempThing.TryGetComp<CompQuality>();
                            CompQuality replacementCompQuality = compQuality;
                            replacementCompQuality.SetQuality(compQuality.Quality, ArtGenerationContext.Outsider);
                            replacementThing = replacementThing.TryMakeMinified();

                            replacementList.Add(replacementThing);
                            tempList.Add(thing);
                        }
                    }
                }
            }
            
            foreach (Thing thing in tempList)
            {
                __result.Remove(thing);
            }
            
            __result.AddRange(replacementList);
        }

        private static Thing GenerateFoodReplacementPemmican(int count)
        {
            Thing thing = ThingMaker.MakeThing(ThingDefOf.Pemmican, null);
            thing.stackCount = 16 * count;

            return thing;
        }

        private static Thing GenerateFoodReplacementMeals(int count)
        {
            Thing thing = ThingMaker.MakeThing(ThingDefOf.MealSimple, null);
            thing.stackCount = count;

            return thing;
        }

        // Generate a random non future item or two to replace the ones taken out.
        // Slight chance for artifact.
        private static List<Thing> GenerateRandomFuturisticItemReplacement()
        {
            int rInt = MedievalVanillaMod.random.Next(0, 10);

            switch (rInt)
            {
                case 0:
                    // Artifact NO MEDIEVAL ARTIFACTS
                    //return GenerateRandomArtifact();
                    return GenerateRandomWeaponArmor();
                case 1:
                case 2:
                case 3:
                    // Apparel
                    return GenerateRandomApparel();
                case 4:
                case 5:
                case 6:
                    // Furniture
                    return GenerateRandomFurniture();
                case 7:
                case 8:
                case 9:
                    // Weapon / Armor
                    return GenerateRandomWeaponArmor();
                default:
                    // Artifact NO MEDIEVAL ARTIFACTS
                    //return GenerateRandomArtifact();
                    return GenerateRandomWeaponArmor();
            }
        }

        // Generate random artifact.
        private static List<Thing> GenerateRandomArtifact()
        {
            int rInt = MedievalVanillaMod.random.Next(0, 10);

            switch (rInt)
            {
                case 0:
                case 1:
                case 2:
                    // Flaming Rod
                    return GenerateArtifact(ThingDef.Named("RH_TET_Rod_FlamingDeath"));
                case 3:
                case 4:
                case 5:
                case 6:
                    // Healer - Slightly more common.
                    return GenerateArtifact(ThingDef.Named("RH_TET_Potion_Healing"));
                case 7:
                case 8:
                case 9:
                    // Resurrection Wand
                    return GenerateArtifact(ThingDef.Named("RH_TET_Wand_Resurrection"));
                default:
                    // Healer - Slightly more common.
                    return GenerateArtifact(ThingDef.Named("RH_TET_Potion_Healing"));
            }
        }
        
        private static List<Thing> GenerateRandomWeaponArmor()
        {
            int rInt = MedievalVanillaMod.random.Next(0, 10);
            List<Thing> retList = new List<Thing>();

            // Completely different rando. 10% chance for excellent, 10% chance for legendary, else masterwork.
            int rQuality = MedievalVanillaMod.random.Next(0, 9);
            
            switch (rInt)
            {
                case 0:
                case 1:
                    retList.Add(GenerateThingWithQuality(ThingDef.Named("MeleeWeapon_Mace"), ThingDefOf.Steel, rQuality));
                    break;
                case 2:
                case 3:
                case 4:
                    retList.Add(GenerateThingWithQuality(ThingDef.Named("Bow_Great"), null, rQuality));
                    break;
                case 5:
                case 6:
                    retList.Add(GenerateThingWithQuality(ThingDef.Named("MeleeWeapon_Spear"), ThingDefOf.Gold, rQuality));
                    break;
                case 7:
                case 8:
                case 9:
                    retList.Add(GenerateThingWithQuality(ThingDef.Named("MeleeWeapon_LongSword"), ThingDefOf.Steel, rQuality));
                    break;
                default:
                    break;
            }

            return retList;
        }

        private static List<Thing> GenerateRandomApparel()
        {
            int rInt = MedievalVanillaMod.random.Next(0, 10);
            List<Thing> retList = new List<Thing>();
            
            // Completely different rando. 10% chance for excellent, 10% chance for legendary, else masterwork.
            int rQuality = MedievalVanillaMod.random.Next(0, 9);
            
            switch (rInt)
            {
                case 0:
                case 1:
                    retList.Add(GenerateThingWithQuality(ThingDef.Named("Apparel_Jacket"), ThingDef.Named("Leather_Thrumbo"), rQuality));
                    break;
                case 2:
                case 3:
                case 4:
                    retList.Add(GenerateThingWithQuality(ThingDefOf.Apparel_Parka, ThingDef.Named("WoolMegasloth"), rQuality));
                    break;
                case 5:
                case 6:
                    retList.Add(GenerateThingWithQuality(ThingDef.Named("Apparel_CollarShirt"), ThingDef.Named("Leather_Thrumbo"), rQuality));
                    break;
                case 7:
                case 8:
                case 9:
                    retList.Add(GenerateThingWithQuality(ThingDef.Named("Apparel_Duster"), ThingDef.Named("WoolSheep"), rQuality));
                    break;
                default:
                    break;
            }

            return retList;
        }

        private static List<Thing> GenerateRandomFurniture()
        {
            int rInt = MedievalVanillaMod.random.Next(0, 10);
            List<Thing> retList = new List<Thing>();
            
            // Completely different rando. 10% chance for excellent, 10% chance for legendary, else masterwork.
            int rQuality = MedievalVanillaMod.random.Next(0, 9);
            
            switch (rInt)
            { 
                case 0:
                case 1:
                    retList.Add(GenerateThingWithQuality(ThingDefOf.Bed, ThingDefOf.Gold, rQuality));
                    break;
                case 2:
                case 3:
                    retList.Add(GenerateThingWithQuality(ThingDefOf.DiningChair, ThingDefOf.Gold, rQuality));
                    break;
                case 4:
                case 5:
                case 6:
                    retList.Add(GenerateThingWithQuality(ThingDefOf.RoyalBed, ThingDefOf.Gold, rQuality));
                    break;
                case 7:
                case 8:
                case 9:
                    retList.Add(GenerateThingWithQuality(ThingDef.Named("Dresser"), ThingDef.Named("Jade"), rQuality));
                    break;
                default:
                    break;
            }

            return retList;
        }

        private static Thing GenerateThingWithQuality(ThingDef thingDef, ThingDef materialDef, int rando)
        {
            Thing thing = ThingMaker.MakeThing(thingDef, materialDef);
            CompQuality compQuality = thing.TryGetComp<CompQuality>();

            QualityCategory qc = QualityCategory.Masterwork;

            if (rando < 1)
            {
                qc = QualityCategory.Excellent;
            }
            else if (rando > 8)
            {
                qc = QualityCategory.Legendary;
            }

            compQuality.SetQuality(qc, ArtGenerationContext.Outsider);

            return thing;
        }

        private static List<Thing> GenerateArtifact(ThingDef thingDef)
        {
            Thing thing = ThingMaker.MakeThing(thingDef);
            CompQuality compQuality = thing.TryGetComp<CompQuality>();
            int rQuality = MedievalVanillaMod.random.Next(0, 9);
            List<Thing> retList = new List<Thing>();

            try
            {
                if (compQuality != null)
                {
                    QualityCategory qc = QualityCategory.Masterwork;

                    if (rQuality < 1)
                    {
                        qc = QualityCategory.Excellent;
                    }
                    else if (rQuality > 8)
                    {
                        qc = QualityCategory.Legendary;
                    }

                    compQuality.SetQuality(qc, ArtGenerationContext.Outsider);
                }
            }
            catch
            {
                // Ignore. There was on compQuality on the artifact.
                //Log.Warning("No comp quality found on the artifact thing.");
            }

            retList.Add(thing);

            return retList;
        }

        public static bool IsTravelingInTransportPodWorldObject(Pawn pawn, ref bool __result)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (MedievalVanillaMod.MAX_TECHLEVEL <= TechLevel.Industrial)
            {
                __result = false;
                return false;
            }
            // ReSharper disable once HeuristicUnreachableCode
            #pragma warning disable 162
            return true;
            #pragma warning restore 162
        }
    }
}