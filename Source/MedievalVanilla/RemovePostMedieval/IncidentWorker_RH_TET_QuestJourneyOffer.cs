using RimWorld;
using RimWorld.Planet;
using System;
using Verse;

namespace MedievalVanilla
{
    public class IncidentWorker_RH_TET_QuestJourneyOffer : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return false;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            return false;
        }

        private bool TryFindRootTile(out int tile)
        {
            tile = 0;
            return false;
        }

        private bool TryFindDestinationTile(int rootTile, out int tile)
        {
            tile = 0;
            return false;
        }

        private bool TryFindDestinationTileActual(int rootTile, int minDist, out int tile)
        {
            tile = 0;
            return false;
        }
    }
}
