using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

// This code came from https://gist.github.com/lbmaian/cb1be0e3485443a704ea55adca77216c
namespace MedievalVanilla
{
    //[staticconstructoronstartup]
    //static class removepostmedievaldebuglog
    //{
    //    public static readonly assembly assembly = accesstools.typebyname("theendtimes.onstartup").assembly;

    //    static readonly stringbuilder debugstring;
    //    static readonly hashset<thingdef> thingstoremove;
    //    static readonly action<type, ienumerable<def>> removestufffromdatabase;

    //    static removepostmedievaldebuglog()
    //    {
    //        var typeof_onstartup = assembly.gettype("theendtimes.onstartup", throwonerror: true);
    //        var typeof_removepostmedieval = assembly.gettype("theendtimes.removepostmedieval", throwonerror: true);
    //        removestufffromdatabase = (action<type, ienumerable<def>>)delegate.createdelegate(typeof(action<type, ienumerable<def>>),
    //            accesstools.method(typeof_removepostmedieval, "removestufffromdatabase"));

    //        // remove non-medieval stuffed materials by setting their techlevel before the removepostmedieval static constructor is run.
    //        // rimworld of magic still uses uranium and plasteel, so leaving those in.
    //        if (!settings.includeuranium)
    //        {
    //            thingdef.named("uranium").techlevel = techlevel.spacer;
    //            thingdefof.uranium.stuffprops.commonality = 0.0f;
    //            thingdef.named("mineableuranium").building.mineablescattercommonality = 0.0f;
    //        }
    //        if (!settings.includeplasteel)
    //        {
    //            thingdef.named("plasteel").techlevel = techlevel.spacer;
    //            thingdefof.plasteel.stuffprops.commonality = 0.0f;
    //            thingdef.named("mineableplasteel").building.mineablescattercommonality = 0.0f;
    //        }

    //        thingdef.named("synthread").techlevel = techlevel.spacer;
    //        thingdef.named("hyperweave").techlevel = techlevel.ultra;

    //        // copied code to get the set of thingdefs to be removed (since can't patch removepostmedieval static constructor).
    //        thingdef.named("electricsmelter").techlevel = techlevel.industrial;
    //        thingdef.named("electriccrematorium").techlevel = techlevel.industrial;

    //        thingdef.named("componentindustrial").techlevel = techlevel.medieval;
    //        // remove mineable components.
    //        thingdef.named("mineablecomponentsindustrial").building.mineablescattercommonality = 0.0f;

    //        var projects = defdatabase<researchprojectdef>.alldefs.where(rpd => rpd.techlevel > techlevel.medieval);

    //        thingstoremove = new hashset<thingdef>(defdatabase<thingdef>.alldefs.where(td =>
    //            td.techlevel > techlevel.medieval ||
    //            (td.researchprerequisites?.any(rpd => projects.contains(rpd)) ?? false) || new[]
    //            {
    //                "gun_revolver",
    //                "vanometricpowercell",
    //                "psychicemanator",
    //                "infinitechemreactor",
    //                "joywire",
    //                "painstopper",
    //                "componentspacer",
    //                "apparel_flakpants",
    //                "apparel_flakjacket",
    //                "apparel_psychicfoilhelmet",
    //                "apparel_shieldbelt",
    //                "mechserumneurotrainer",
    //                "mechserumhealer",
    //                "mechserumresurrector",
    //                "tubetelevision",
    //                "flatscreentelevision",
    //                "megascreentelevision",
    //            }.contains(td.defname)));
    //        // end copied code.

    //        // ensure other static constructors are executed.
    //        runtimehelpers.runclassconstructor(typeof_onstartup.typehandle);
    //        runtimehelpers.runclassconstructor(typeof_removepostmedieval.typehandle);
    //        var fieldof_removepostmedieval_debugstring = accesstools.field(typeof_removepostmedieval, "debugstring");
    //        debugstring = (stringbuilder)fieldof_removepostmedieval_debugstring.getvalue(null);

    //        debugstring.appendline("thingsetmaker removing stuff");
    //        foreach (var thingsetmaker in defdatabase<thingsetmakerdef>.alldefs)
    //        {
    //            removethingsfromthingsetmaker(thingsetmaker.root, $"{thingsetmaker.defname}.root");
    //        }

    //        // this obviates the need for mv_removesleepingmechanoids.xml.
    //        debugstring.appendline("sitepartdef removing stuff");
    //        removestufffromdatabase(typeof(defdatabase<sitepartdef>), new[]
    //        {
    //            sitepartdefof.sleepingmechanoids,
    //        });

    //        debugstring.appendline("[medieval vanilla] - init complete (for realz).");

    //        log.message(debugstring.tostring());
    //    }

    //    static void removethingsfromthingsetmaker(thingsetmaker thingsetmaker, string path)
    //    {
    //        var thingfilter = thingsetmaker.fixedparams.filter;
    //        var tsmtypename = thingsetmaker.gettype().name.replace("thingsetmaker_", "");
    //        if (thingfilter != null)
    //        {
    //            removethingsfromthingfilter(thingfilter, $"{path}({tsmtypename})");
    //        }
    //        if (thingsetmaker is thingsetmaker_randomoption tsmrandomoption)
    //        {
    //            var options = tsmrandomoption.options;
    //            if (options != null)
    //            {
    //                for (int i = 0, count = options.count; i < count; i++)
    //                {
    //                    var option = options[i];
    //                    removethingsfromthingsetmaker(option.thingsetmaker, $"{path}({tsmtypename}).options[{i}]");
    //                }
    //            }
    //        }
    //        else if (thingsetmaker is thingsetmaker_sum tsmsum)
    //        {
    //            var options = tsmsum.options;
    //            if (options != null)
    //            {
    //                for (int i = 0, count = options.count; i < count; i++)
    //                {
    //                    var option = options[i];
    //                    removethingsfromthingsetmaker(option.thingsetmaker, $"{path}({tsmtypename}).options[{i}]");
    //                }
    //            }
    //        }
    //        else if (thingsetmaker is thingsetmaker_conditional tsmconditional)
    //        {
    //            if (tsmconditional.thingsetmaker != null)
    //            {
    //                removethingsfromthingsetmaker(tsmconditional.thingsetmaker, $"{path}({tsmtypename}).thingsetmaker");
    //            }
    //        }
    //    }

    //    static readonly accesstools.fieldref<thingfilter, list<thingdef>> thingdefsfield =
    //        accesstools.fieldrefaccess<thingfilter, list<thingdef>>("thingdefs");

    //    static void removethingsfromthingfilter(thingfilter thingfilter, string thingfilterlabel)
    //    {
    //        var origallowedthings = new hashset<thingdef>(thingfilter.allowedthingdefs);
    //        thingdefsfield(thingfilter)?.removeall(thingstoremove.contains);
    //        // always refresh the thingfilter (allowed thing defs aren't only from the thingdefs field).
    //        ((icollection<thingdef>)thingfilter.allowedthingdefs).clear();
    //        thingfilter.resolvereferences();
    //        origallowedthings.exceptwith(thingfilter.allowedthingdefs);
    //        if (origallowedthings.any())
    //        {
    //            debugstring.appendline($" - {thingfilterlabel}");
    //            foreach (var removedthing in origallowedthings)
    //                debugstring.appendline($"   - {removedthing.defname}");
    //        }
    //    }
    //}

    //[staticconstructoronstartup]
    //static class harmonypatches
    //{
    //    static harmonypatches()
    //    {
    //        harmonyinstance.debug = false;
    //        try
    //        {
    //            harmonyinstance.create("theendtimes.debuglog").patchall();
    //        }
    //        finally
    //        {
    //            harmonyinstance.debug = false;
    //        }
    //    }
    //}

    //// fix item stash quest sometimes attempting hard-coded mechanoid ambush.
    //[harmonypatch(typeof(pawngroupmakerutility), nameof(pawngroupmakerutility.cangenerateanynormalgroup))]
    //static class pawngroupmakerutility_cangenerateanynormalgroup_patch
    //{
    //    [harmonyprefix]
    //    static bool prefix(ref bool __result, faction faction)
    //    {
    //        if (faction is null)
    //        {
    //            __result = false;
    //            return false;
    //        }
    //        return true;
    //    }
    //}

    // removes hard-coding of uranium & plasteel replacement.
    // todo: remove hard-coding of stuffed uranium & plasteel replacement?
    //[harmonypatch]
    //static class removepostmedievalharmony_itemcollectiongeneratorgeneratepostfix_patch
    //{
    //    [harmonytargetmethod]
    //    static methodbase targetmethod(harmonyinstance _) =>
    //        removepostmedievaldebuglog.assembly.gettype("theendtimes.removepostmedievalharmony", throwonerror: true)
    //            .getmethod("itemcollectiongeneratorgeneratepostfix", accesstools.all);

    //    [harmonyprefix]
    //    static void prefix([harmonyargument("__result")] list<thing> result, ref list<thing> __state)
    //    {
    //        __state = result.where(thing => thing.getinnerifminified().def.defname is var defname &&
    //            ((!settings.includeuranium && defname is "uranium") || (!settings.includeplasteel && defname is "plasteel"))).tolist();

    //        result.removeall(__state.contains);
    //    }

    //    [harmonypostfix]
    //    static void postfix([harmonyargument("__result")] list<thing> result, list<thing> __state)
    //    {
    //        result.addrange(__state);
    //    }
    //}

    //    Could not execute post-long-event action.Exception: System.TypeInitializationException: The type initializer for 'TheEndTimes.HarmonyPatches' threw an exception. ---> System.ArgumentException: No target method specified for class TheEndTimes.RemovePostMedievalHarmony_GetIntegerFromTicks_Patch(declaringType=, methodName =, methodType=, argumentTypes= NULL)
    //  at Harmony.PatchProcessor.PrepareType() [0x001de] in <1b23547042994e96b8b6361dbe3791d9>:0 
    //  at Harmony.PatchProcessor..ctor(Harmony.HarmonyInstance instance, System.Type type, Harmony.HarmonyMethod attributes) [0x00065] in <1b23547042994e96b8b6361dbe3791d9>:0 
    //  at Harmony.HarmonyInstance.<PatchAll>b__9_0 (System.Type type)[0x00023] in <1b23547042994e96b8b6361dbe3791d9>:0 
    //  at Harmony.CollectionExtensions.Do[T] (System.Collections.Generic.IEnumerable`1[T] sequence, System.Action`1[T] action) [0x0001b] in <1b23547042994e96b8b6361dbe3791d9>:0 
    //  at Harmony.HarmonyInstance.PatchAll(System.Reflection.Assembly assembly) [0x00007] in <1b23547042994e96b8b6361dbe3791d9>:0 
    //  at Harmony.HarmonyInstance.PatchAll() [0x0001e] in <1b23547042994e96b8b6361dbe3791d9>:0 
    //  at TheEndTimes.HarmonyPatches..cctor() [0x00012] in <bb3c5858ede247db9ba6492e1cb76bc9>:0 
    //   --- End of inner exception stack trace ---
    //  at(wrapper managed-to-native) System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(intptr)
    // at System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(System.RuntimeTypeHandle type)[0x0002a] in <567df3e0919241ba98db88bec4c6696f>:0 
    //  at Verse.StaticConstructorOnStartupUtility.CallAll() [0x00018] in <b8131242147c4f5bbc580697f7726377>:0 
    //  at Verse.PlayDataLoader+<>c.<DoPlayLoad>b__4_2 ()[0x0000a] in <b8131242147c4f5bbc580697f7726377>:0 
    //  at Verse.LongEventHandler.ExecuteToExecuteWhenFinished ()[0x0007d] in <b8131242147c4f5bbc580697f7726377>:0 
    //Verse.Log:Error(String, Boolean)
    //Verse.LongEventHandler:ExecuteToExecuteWhenFinished()
    // Fix any other possible replace modern thing with nothing bugs.
    //[HarmonyPatch]
    //static class RemovePostMedievalHarmony_GetIntegerFromTicks_Patch
    //{
    //    [HarmonyTargetMethod]
    //    static MethodBase TargetMethod(HarmonyInstance _) =>
    //        RemovePostMedievalDebugLog.Assembly.GetType("TheEndTimes.RemovePostMedievalHarmony", throwOnError: true)
    //            .GetMethod("GetIntegerFromTicks", AccessTools.all);

    //    [HarmonyPrefix]
    //    static bool Prefix(long tickNumber, ref int __result)
    //    {
    //        __result = (int)(tickNumber % 10);
    //        //if ((int)tickNumber % 10 < 0)
    //        //	Log.Error($"GetIntegerFromTicks would've returned {(int)tickNumber % 10}, now returning {__result}");
    //        return false;
    //    }
    //}
}