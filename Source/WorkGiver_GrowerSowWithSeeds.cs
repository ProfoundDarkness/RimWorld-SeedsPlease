using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;
using Verse;
using Verse.AI;

namespace SeedsPlease
{
	public class WorkGiver_GrowerSowWithSeeds : WorkGiver_GrowerSow
	{
		public override Job JobOnCell (Pawn pawn, IntVec3 cell)
		{
			Job job = base.JobOnCell (pawn, cell);
			if (job != null && job.plantDefToSow != null && job.plantDefToSow.blueprintDef != null) {
				Predicate<Thing> predicate = (Thing tempThing) =>
					!ForbidUtility.IsForbidden (tempThing, pawn.Faction)
					&& PawnLocalAwareness.AnimalAwareOf (pawn, tempThing)
					&& ReservationUtility.CanReserve (pawn, tempThing, 1);

				Thing bestSeedThingForSowing = GenClosest.ClosestThingReachable (
					cell, pawn.Map, ThingRequest.ForDef (job.plantDefToSow.blueprintDef), 
					PathEndMode.ClosestTouch, TraverseParms.For (pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999,
	                predicate, null, -1, false);
				
				// TODO: After adding in a new property for growing zones, check if that property is enabled indicating should clear before sowing.
				Plant cutPlant;
				if (siteHasOtherPlants(cell, pawn.Map, job.plantDefToSow, out cutPlant))
				{
					return new Job(LocalJobDefOf.ClearPreSow, cell)
					{
						targetA = new LocalTargetInfo(cutPlant)
					};
				}
				
				Log.Message(string.Concat("pawn ", pawn, " map: ", pawn.Map.ToString()));
				if (bestSeedThingForSowing != null) {
					return new Job (LocalJobDefOf.SowWithSeeds, cell, bestSeedThingForSowing) {
						plantDefToSow = job.plantDefToSow,
						count = 25
					};
				}
				return null;
			}

			return job;
		}
		
		private bool siteHasOtherPlants(IntVec3 cell, Map map, ThingDef def, out Plant plant)
		{
			plant = (Plant)cell.GetZone(map).AllContainedThings.AsQueryable().Where(t => t is Plant && t.def != def && t.def != def.blueprintDef).FirstOrDefault();
			// additional check for trees that could block sowing...
			if (plant == null)
			{
				List<IntVec3> cells = cell.GetZone(map).cells;
				Thing blocker = null;
				foreach (IntVec3 c in cells) {
					blocker = GenPlant.AdjacentSowBlocker(def, c, map);
					if (blocker != null)
						break;
				}
				if (blocker as Plant != null)
					plant = (Plant)blocker;
			}
			return plant != null;
		}
	}
}
