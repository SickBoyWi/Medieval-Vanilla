using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using HarmonyLib;

namespace MedievalVanilla
{

    [StaticConstructorOnStartup]
    public static class RemovePostMedieval
    {
        private static int removedDefs;
        private static StringBuilder DebugString = new StringBuilder();
        private static readonly HashSet<ThingDef> thingsToRemove;
        private static bool ceActive = false;
 //       private static bool alphaBiomesActive = false;

        static RemovePostMedieval()
        {
            DebugString.AppendLine("[Medieval Vanilla] - Initializing.");
            DebugString.AppendLine("Tech Limiter Active: Max Level = " + MedievalVanillaMod.MAX_TECHLEVEL.ToString());
            GiveApproppriateTechLevels();

            removedDefs = 0;

            IEnumerable<ResearchProjectDef> projects =
                DefDatabase<ResearchProjectDef>.AllDefs.Where(rpd => rpd.techLevel > MedievalVanillaMod.MAX_TECHLEVEL);

            thingsToRemove = new HashSet<ThingDef>(DefDatabase<ThingDef>.AllDefs.Where(td =>
                td.techLevel > MedievalVanillaMod.MAX_TECHLEVEL ||
                (td.researchPrerequisites?.Any(rpd => projects.Contains(rpd)) ?? false) || new[]
                {
                            "Gun_Revolver",
                            "VanometricPowerCell",
                            "PsychicEmanator",
                            "InfiniteChemreactor",
                            "Joywire",
                            "Painstopper",
                            "ComponentSpacer",
                            "Apparel_FlakPants",
                            "Apparel_FlakJacket",
                            "Apparel_PsychicFoilHelmet",
                            "Apparel_ShieldBelt",
                            "MechSerumNeurotrainer",
                            "MechSerumHealer",
                            "MechSerumResurrector",
                            "TubeTelevision",
                            "FlatscreenTelevision",
                            "MegascreenTelevision",
                }.Contains(td.defName)));

            //if (ModsConfig.IsActive("sarg.alphabiomes"))
            //{
            //    alphaBiomesActive = true;
            //}
            //else
            //    alphaBiomesActive = false;

            //if (alphaBiomesActive)
            //{
            //    string[] thingsToKeep = new[]
            //    {
            //        "VCEF_AbyssalEel",
            //        "VCEF_BloodLeech",
            //        "VCEF_BorealSardine",
            //        "VCEF_DuskySprat",
            //        "VCEF_ForsakenAnglerfish",
            //        "VCEF_FrigidSwimmer",
            //        "VCEF_GiantPolarPrawn",
            //        "VCEF_HabdakLongfin",
            //        "VCEF_Jellyfungus",
            //        "VCEF_MudSplasherCarp",
            //        "VCEF_MutantFlatfish",
            //        "VCEF_OcularFish",
            //        "VCEF_PinkBlobfish",
            //        "VCEF_PropaneBag",
            //        "VCEF_ShadowFry",
            //        "VCEF_Slimefish",
            //        "VCEF_Spiderfish",
            //        "VCEF_Spinyfish",
            //        "VCEF_Sturgeon"
            //    };

            //    List<ThingDef> removeFromRemoveThingDefList = new List<ThingDef>();
            //    // Save CE ammo creation recipes from getting axed from the game. 
            //    foreach (ThingDef thiDef in thingsToRemove)
            //    {
            //        foreach (string s in thingsToKeep)
            //        {
            //            if (thiDef.defName.StartsWith(s))
            //                removeFromRemoveThingDefList.Add(thiDef);
            //        }
            //    }

            //    foreach (ThingDef td in removeFromRemoveThingDefList)
            //    {
            //        thingsToRemove.Remove(td);
            //    }
            //}

            ThingDef[] things = thingsToRemove.ToArray();

            DebugString.AppendLine("RecipeDef Removal List:");

            // Stop the below source from scouring CE neolithic ammo from the game.
            string[] recipesToKeep = new[]
                {
                    "MakeAmmo_Pilum",
                    "MakeAmmo_Sling",
                    "MakeAmmo_Arrow",
                    "MakeAmmo_GreatArrow",
                    "MakeAmmo_CrossbowBolt",
                    "MakeAmmo_Musket"
                };

            if (ModsConfig.IsActive("CETeam.CombatExtended"))
            {
                ceActive = true;
            }
            else
                ceActive = false;

            var recipeDefsToRemove = DefDatabase<RecipeDef>.AllDefs.Where(
                      recipeDef =>
                          // Recipe products are something in the list of things to remove. 
                          recipeDef.products.Any(thingDefCountClass => things.Contains(thingDefCountClass.thingDef))
                         // Recipe user is a thing in the things to remove list. 
                         || recipeDef.AllRecipeUsers.All(thingDef => things.Contains(thingDef))
                         // Recipe contains a removed research project as a requisite.
                         || projects.Contains(recipeDef.researchPrerequisite))
                .Cast<Def>().ToList();

            if (ceActive)
            {
                List<Def> removeFromRemoveRecipeDefList = new List<Def>();
                // Save CE ammo creation recipes from getting axed from the game. 
                foreach (Def recDef in recipeDefsToRemove)
                {
                    foreach (string s in recipesToKeep)
                    {
                        if (recDef.defName.StartsWith(s))
                            removeFromRemoveRecipeDefList.Add(recDef);
                    }
                }

                if (removeFromRemoveRecipeDefList.Count > 0)
                {
                    List<Def> temp = recipeDefsToRemove.Except<Def>(removeFromRemoveRecipeDefList).ToList();
                    recipeDefsToRemove = temp;
                }
            }

            RemoveStuffFromDatabase(typeof(DefDatabase<RecipeDef>), recipeDefsToRemove);
            
            DebugString.AppendLine("ResearchProjectDef Removing Stuff");
            RemoveStuffFromDatabase(typeof(DefDatabase<ResearchProjectDef>), projects.Cast<Def>());
            
            DebugString.AppendLine("ScenarioPart Removing Stuff");
            FieldInfo getThingInfo =
                typeof(ScenPart_ThingCount).GetField("thingDef", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (ScenarioDef def in DefDatabase<ScenarioDef>.AllDefs)
                foreach (ScenPart sp in def.scenario.AllParts)
                    if (sp is ScenPart_ThingCount && things.Contains((ThingDef)getThingInfo?.GetValue(sp)))
                    {
                        def.scenario.RemovePart(sp);
                        DebugString.AppendLine("- " + sp.Label + " " + ((ThingDef)getThingInfo?.GetValue(sp)).label +
                                               " from " + def.label);
                    }

            foreach (ThingCategoryDef thingCategoryDef in DefDatabase<ThingCategoryDef>.AllDefs)
                thingCategoryDef.childThingDefs.RemoveAll(things.Contains);

            DebugString.AppendLine("StockGenerator Cleanup");
            foreach (TraderKindDef tkd in DefDatabase<TraderKindDef>.AllDefs)
            {
                for (int i = tkd.stockGenerators.Count - 1; i >= 0; i--)
                {
                    StockGenerator stockGenerator = tkd.stockGenerators[i];

                    switch (stockGenerator)
                    {
                        case StockGenerator_SingleDef sd when things.Contains(Traverse.Create(sd).Field("thingDef")
                            .GetValue<ThingDef>()):
                            ThingDef def = Traverse.Create(sd).Field("thingDef")
                                .GetValue<ThingDef>();
                            tkd.stockGenerators.Remove(stockGenerator);
                            DebugString.AppendLine("- " + def.label + " from " + tkd.label +
                                                   "'s StockGenerator_SingleDef");
                            break;
                        case StockGenerator_MultiDef md:
                            Traverse thingListTraverse = Traverse.Create(md).Field("thingDefs");
                            List<ThingDef> thingList = thingListTraverse.GetValue<List<ThingDef>>();
                            var removeList = thingList.FindAll(things.Contains);
                            removeList?.ForEach(x =>
                                DebugString.AppendLine("- " + x.label + " from " + tkd.label +
                                                       "'s StockGenerator_MultiDef"));
                            thingList.RemoveAll(things.Contains);

                            if (thingList.NullOrEmpty())
                                tkd.stockGenerators.Remove(stockGenerator);
                            else
                                thingListTraverse.SetValue(thingList);
                            break;
                    }
                }
            }

            DebugString.AppendLine("IncidentDef Removing Stuff");

            RemoveStuffFromDatabase(typeof(DefDatabase<IncidentDef>),
                DefDatabase<IncidentDef>.AllDefs
                    .Where(id => new[]
                                     {
                                         typeof
                                         (IncidentWorker_ShipChunkDrop
                                         ),
                                         AccessTools
                                             .TypeByName(
                                                 "IncidentWorker_CrashedShipPart"),
                                         //typeof
                                         //(IncidentWorker_QuestJourneyOffer
                                         //),
                                         typeof
                                         (IncidentWorker_ResourcePodCrash
                                         ),
                                         //typeof(IncidentWorker_TransportPodCrash),TODO JEH 1.3 SEE FOUR LINES UP, WHAT DID I DO THERE?
                                         //typeof
                                         //(IncidentWorker_PsychicDrone
                                         //),
                                         typeof
                                         (IncidentWorker_RansomDemand
                                         ),
                                         typeof
                                         (IncidentWorker_ShortCircuit
                                         ),
                                         typeof
                                         (IncidentWorker_OrbitalTraderArrival
                                         ),
                                         //typeof
                                         //(IncidentWorker_PsychicSoothe
                                         //),
                                         typeof
                                         (IncidentWorker_MechCluster
                                         ),
                                         //typeof
                                         //(IncidentWorker_QuestItemStashAICore
                                         //)
                                         

                                     }.SelectMany(
                                         it =>
                                             it
                                                 .AllSubclassesNonAbstract()
                                                 .Concat(
                                                     it))
                                     .ToArray()
                                     .Contains(
                                         id
                                             .workerClass) ||
                                 new[]
                                 {
                                     "Disease_FibrousMechanites",
                                     "Disease_SensoryMechanites",
                                     "RaidEnemyEscapeShip",
                                     "StrangerInBlackJoin",
                                     "ShipChunkDrop",
                                     "OrbitalTraderArrival",
                                     "JourneyOffer",
                                     "QuestItemStashGuaranteedCore"
                                 }.Contains(id.defName))
                    .Cast<Def>());
            
            DebugString.AppendLine("Replace Ancient Asphalt Road / Ancient Asphalt Highway with Stone Road.");
            RoadDef[] targetRoads = { RoadDefOf.AncientAsphaltRoad, RoadDefOf.AncientAsphaltHighway };
            RoadDef originalRoad = DefDatabase<RoadDef>.GetNamed("StoneRoad");

            List<string> fieldNames = AccessTools.GetFieldNames(typeof(RoadDef));
            fieldNames.Remove("defName");
            foreach (FieldInfo fi in fieldNames.Select(name => AccessTools.Field(typeof(RoadDef), name)))
            {
                object fieldValue = fi.GetValue(originalRoad);
                foreach (RoadDef targetRoad in targetRoads) fi.SetValue(targetRoad, fieldValue);
            }
            
            // This obviates the need for MV_RemoveSleepingMechanoids.xml.
            DebugString.AppendLine("SitePartDef Removing Stuff");
            SitePartDef sleepingMechs = DefDatabase<SitePartDef>.GetNamed("SleepingMechanoids");
            RemoveStuffFromDatabase(typeof(DefDatabase<SitePartDef>), new[]
            {
                sleepingMechs,
            });

            DebugString.AppendLine("RaidStrategyDef Removing Stuff");
            RemoveStuffFromDatabase(typeof(DefDatabase<RaidStrategyDef>),
                DefDatabase<RaidStrategyDef>.AllDefs
                    .Where(rs => typeof(ScenPart_ThingCount).IsAssignableFrom(rs.workerClass)).Cast<Def>());

            DebugString.AppendLine("ThingDef Removing Stuff");
            RemoveStuffFromDatabase(typeof(DefDatabase<ThingDef>), things);

            //DebugString.AppendLine("ThingSetMaker Reset");
            //ThingSetMakerUtility.Reset();
            DebugString.AppendLine("ThingSetMaker Removing Stuff");
            foreach (var thingSetMaker in DefDatabase<ThingSetMakerDef>.AllDefs)
            {
                RemoveThingsFromThingSetMaker(thingSetMaker.root, $"{thingSetMaker.defName}.root");
            }

            //DebugString.AppendLine("TraitDef Removing Stuff");
            //RemoveStuffFromDatabase(typeof(DefDatabase<TraitDef>),
            //    DefDatabase<TraitDef>.AllDefs
            //        .Where(td => new[] { nameof(TraitDefOf.BodyPurist), "Transhumanist" }.Contains(td.defName))
            //        .Cast<Def>());

            DebugString.AppendLine("Designators Resolved Again");
            MethodInfo resolveDesignatorsAgain = typeof(DesignationCategoryDef).GetMethod("ResolveDesignators",
                BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (DesignationCategoryDef dcd in DefDatabase<DesignationCategoryDef>.AllDefs.Where<DesignationCategoryDef>((Func<DesignationCategoryDef, bool>)(def => def.defName != "Pathing")))
                resolveDesignatorsAgain?.Invoke(dcd, null);

            DebugString.AppendLine("PawnKindDef Removing Stuff");
            RemoveStuffFromDatabase(typeof(DefDatabase<PawnKindDef>),
                DefDatabase<PawnKindDef>.AllDefs
                    .Where(pkd =>
                        (!pkd.defaultFactionDef?.isPlayer ?? false) &&
                        (pkd.race.techLevel > MedievalVanillaMod.MAX_TECHLEVEL || pkd.defaultFactionDef?.techLevel > MedievalVanillaMod.MAX_TECHLEVEL))
                    .Cast<Def>());

            DebugString.AppendLine("FactionDef Removing Stuff");
            RemoveStuffFromDatabase(typeof(DefDatabase<FactionDef>),
                DefDatabase<FactionDef>.AllDefs.Where(fd => !fd.isPlayer && fd.techLevel > MedievalVanillaMod.MAX_TECHLEVEL).Cast<Def>());

            DebugString.AppendLine("MapGeneratorDef Removal List");
            DebugString.AppendLine("- GenStep_SleepingMechanoids");
            DebugString.AppendLine("- GenStep_Turrets");
            DebugString.AppendLine("- GenStep_Power");
            foreach (MapGeneratorDef mgd in DefDatabase<MapGeneratorDef>.AllDefs)
                mgd.genSteps.RemoveAll(gs =>
                    gs.genStep is GenStep_SleepingMechanoids || gs.genStep is GenStep_Turrets ||
                    gs.genStep is GenStep_Power);

            DebugString.AppendLine("RuleDef Removing Stuff");
            DebugString.AppendLine("- SymbolResolver_AncientCryptosleepCasket");
            DebugString.AppendLine("- SymbolResolver_ChargeBatteries");
            DebugString.AppendLine("- SymbolResolver_EdgeMannedMortor");
            DebugString.AppendLine("- SymbolResolver_FirefoamPopper");
            DebugString.AppendLine("- SymbolResolver_MannedMortar");
            DebugString.AppendLine("- SymbolResolver_OutdoorLighting");
            foreach (RuleDef rd in DefDatabase<RuleDef>.AllDefs)
            {
                rd.resolvers.RemoveAll(sr =>
                    sr is SymbolResolver_AncientCryptosleepCasket || sr is SymbolResolver_ChargeBatteries ||
                    sr is SymbolResolver_EdgeMannedMortar || sr is SymbolResolver_FirefoamPopper ||
                    sr is SymbolResolver_MannedMortar || sr is SymbolResolver_OutdoorLighting);
                if (rd.resolvers.Count == 0)
                    rd.resolvers.Add(new SymbolResolver_AddWortToFermentingBarrels());
            }

            DebugString.AppendLine("Removed " + removedDefs + " modern defs");
            
            // Fix incidents with copies of no longer existing pawnkinds.
            IncidentDef wandererJoin = IncidentDefOf.WandererJoin;
            wandererJoin.pawnKind = PawnKindDefOf.Colonist;
            IncidentDef shipChunkDrop = IncidentDefOf.ShipChunkDrop;
            shipChunkDrop.baseChance = 0.0f;

            PawnApparelGenerator.Reset();

            DebugString.AppendLine("[Medieval Vanilla] - Init complete.");

            //Log.Message(DebugString.ToString());
        }

        static void RemoveThingsFromThingSetMaker(ThingSetMaker thingSetMaker, string path)
        {
            var thingFilter = thingSetMaker.fixedParams.filter;
            var tsmTypeName = thingSetMaker.GetType().Name.Replace("ThingSetMaker_", "");
            if (thingFilter != null)
            {
                RemoveThingsFromThingFilter(thingFilter, $"{path}({tsmTypeName})");
            }
            if (thingSetMaker is ThingSetMaker_RandomOption tsmRandomOption)
            {
                var options = tsmRandomOption.options;
                if (options != null)
                {
                    for (int i = 0, count = options.Count; i < count; i++)
                    {
                        var option = options[i];
                        RemoveThingsFromThingSetMaker(option.thingSetMaker, $"{path}({tsmTypeName}).options[{i}]");
                    }
                }
            }
            else if (thingSetMaker is ThingSetMaker_Sum tsmSum)
            {
                var options = tsmSum.options;
                if (options != null)
                {
                    for (int i = 0, count = options.Count; i < count; i++)
                    {
                        var option = options[i];
                        RemoveThingsFromThingSetMaker(option.thingSetMaker, $"{path}({tsmTypeName}).options[{i}]");
                    }
                }
            }
            else if (thingSetMaker is ThingSetMaker_Conditional tsmConditional)
            {
                if (tsmConditional.thingSetMaker != null)
                {
                    RemoveThingsFromThingSetMaker(tsmConditional.thingSetMaker, $"{path}({tsmTypeName}).thingSetMaker");
                }
            }
        }

        static readonly AccessTools.FieldRef<ThingFilter, List<ThingDef>> thingDefsField =
            AccessTools.FieldRefAccess<ThingFilter, List<ThingDef>>("thingDefs");

        static void RemoveThingsFromThingFilter(ThingFilter thingFilter, string thingFilterLabel)
        {
            var origAllowedThings = new HashSet<ThingDef>(thingFilter.AllowedThingDefs);
            thingDefsField(thingFilter)?.RemoveAll(thingsToRemove.Contains);
            // Always refresh the ThingFilter (allowed thing defs aren't only from the thingDefs field).
            ((ICollection<ThingDef>)thingFilter.AllowedThingDefs).Clear();
            thingFilter.ResolveReferences();
            origAllowedThings.ExceptWith(thingFilter.AllowedThingDefs);
            if (origAllowedThings.Any())
            {
                DebugString.AppendLine($" - {thingFilterLabel}");
                foreach (var removedThing in origAllowedThings)
                    DebugString.AppendLine($"   - {removedThing.defName}");
            }
        }

        private static void GiveApproppriateTechLevels()
        {
            DebugString.AppendLine("ElectricSmelter's tech level changed to Industrial");
            ThingDef.Named("ElectricSmelter").techLevel = TechLevel.Industrial;

            DebugString.AppendLine("ElectricCrematorium's tech level changed to Industrial");
            ThingDef.Named("ElectricCrematorium").techLevel = TechLevel.Industrial;

            //DebugString.AppendLine("FueledSmithy's tech level changed to Industrial");
            //ThingDef.Named("FueledSmithy").techLevel = TechLevel.Industrial;

            DebugString.AppendLine("ComponentIndustrial's tech level changed to Medieval");
            ThingDef.Named("ComponentIndustrial").techLevel = TechLevel.Medieval;

            DebugString.AppendLine("DrugProduction's tech level changed to Medieval");
            ResearchProjectDef.Named("DrugProduction").techLevel = TechLevel.Medieval;

            ThingDef.Named("Synthread").techLevel = TechLevel.Spacer;
            ThingDef.Named("Hyperweave").techLevel = TechLevel.Ultra;

            if (ceActive)
                ThingDef.Named("Apparel_Backpack").techLevel = TechLevel.Medieval;

            // Remove non-medieval stuffed materials by setting their techLevel before the RemovePostMedieval static constructor is run.
            // RimWorld of Magic still uses Uranium and Plasteel, so leaving those in.
            if (!Settings.IncludeUranium)
            {
                ThingDef.Named("Uranium").techLevel = TechLevel.Spacer;
                ThingDefOf.Uranium.stuffProps.commonality = 0.0f;
                ThingDef.Named("MineableUranium").building.mineableScatterCommonality = 0.0f;
            }
            if (!Settings.IncludePlasteel)
            {
                ThingDef.Named("Plasteel").techLevel = TechLevel.Spacer;
                ThingDefOf.Plasteel.stuffProps.commonality = 0.0f;
                ThingDef.Named("MineablePlasteel").building.mineableScatterCommonality = 0.0f;
            }
            
            ThingDef.Named("MineableComponentsIndustrial").building.mineableScatterCommonality = 0.0f;

            // Ideology
            if (ModsConfig.IdeologyActive)
            {
                //DebugString.AppendLine("Ideology RitualPatternDef CelebrationPartyDanceTech tech levels changed to Medieval");
                //RitualPatternDef danceTechDef = DefDatabase<RitualPatternDef>.GetNamed("CelebrationPartyDanceTech");
                //danceTechDef.settech

                DebugString.AppendLine("Ideology Relic tech levels changed to Medieval");
                ThingDef.Named("RelicInertCup").techLevel = TechLevel.Medieval;
                ThingDef.Named("RelicInertPendant").techLevel = TechLevel.Medieval;
                ThingDef.Named("RelicInertBox").techLevel = TechLevel.Medieval;
                ThingDef.Named("RelicInertTablet").techLevel = TechLevel.Medieval;
                ThingDef.Named("RelicInertFragment").techLevel = TechLevel.Medieval;
                ThingDef.Named("RelicInertSwordHandle").techLevel = TechLevel.Medieval;
                ThingDef.Named("RelicInertArk").techLevel = TechLevel.Medieval;
                ThingDef.Named("RelicInertCube").techLevel = TechLevel.Medieval;         
            }
        }

        private static void RemoveStuffFromDatabase(Type databaseType, [NotNull] IEnumerable<Def> defs)
        {
            IEnumerable<Def> enumerable = defs as Def[] ?? defs.ToArray();
            if (!enumerable.Any()) return;
            Traverse rm = Traverse.Create(databaseType).Method("Remove", enumerable.First());
            foreach (Def def in enumerable)
            {
                removedDefs++;
                DebugString.AppendLine("- " + def.label);
                rm.GetValue(def);
            }
        }
    }

    [UsedImplicitly]
    public class PatchOperationRemovePostMedievalStuff : PatchOperation
    {
        protected override bool ApplyWorker(XmlDocument xml)
        {
            return true;
        }
    }
}