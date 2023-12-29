using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace MedievalVanilla
{
    public class SettingsController : Mod
    {
        public SettingsController(ModContentPack content) : base(content)
        {
            base.GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "Medieval Vanilla";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }
    }

    public class Settings : ModSettings
    {
        private static Vector2 scrollPosition = Vector2.zero;
        private const int GAP_SIZE = 24;

        public static bool IncludeUranium = true;
        public static bool IncludePlasteel = true;

        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_Values.Look<bool>(ref IncludeUranium, "RH_TET_Include_Uranium", false, false);
            Scribe_Values.Look<bool>(ref IncludePlasteel, "RH_TET_Include_Plasteel", false, false);
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Rect scroll = new Rect(5f, 45f, 430, rect.height - 40);
            Rect view = new Rect(0, 45, 400, 1200);

            Widgets.BeginScrollView(scroll, ref scrollPosition, view, true);
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(view);

            ls.CheckboxLabeled("RH_TET_Include_Uranium".Translate(), ref IncludeUranium);
            ls.Label("RH_TET_Include_UraniumMessage".Translate());
            ls.Gap(GAP_SIZE);

            ls.CheckboxLabeled("RH_TET_Include_Plasteel".Translate(), ref IncludePlasteel);
            ls.Label("RH_TET_Include_UraniumMessage".Translate());
            ls.Gap(GAP_SIZE);

            ls.End();
            Widgets.EndScrollView();
        }
    }
}