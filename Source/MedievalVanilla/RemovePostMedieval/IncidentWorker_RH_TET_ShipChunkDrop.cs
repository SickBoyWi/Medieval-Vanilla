using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MedievalVanilla
{
    public class IncidentWorker_RH_TET_ShipChunkDrop : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return false;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            return false;
        }
    }
}
