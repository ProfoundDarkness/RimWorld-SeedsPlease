using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SeedsPlease
{
	public class JobDriver_PlantClearPreSow : JobDriver
	{
		protected float workDone;
		
		protected Plant Plant
		{
			get
			{
				return (Plant)base.CurJob.targetA.Thing;
			}
		}
		
		public override string GetReport() {
			string text = LocalJobDefOf.ClearPreSow.reportString;
			text = text.Replace("TargetAZone", this.TargetA.Cell.GetZone(this.pawn.Map).label);
			text = text.Replace("TargetA", this.Plant.Label);
			return text;
		}
		
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.LookValue<float>(ref this.workDone, "workDone", 0f, false);
		}
		
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
				.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
			
			Toil cut = new Toil();
			cut.tickAction = delegate 
			{
				Pawn actor = cut.actor;
				if (actor.skills != null)
				{
					actor.skills.Learn(SkillDefOf.Growing, 0.11f, false);
				}
				float statValue = actor.GetStatValue(StatDefOf.PlantWorkSpeed, true);
				Plant plant = this.Plant;
				this.workDone += statValue;
				if (this.workDone >= plant.def.plant.harvestWork)
				{
					if (plant.def.plant.harvestedThingDef != null)
					{
						if (actor.RaceProps.Humanlike && plant.def.plant.harvestFailable && Rand.Value < actor.GetStatValue(StatDefOf.HarvestFailChance, true))
						{
							Vector3 loc = (this.pawn.DrawPos + plant.DrawPos) / 2f;
							MoteMaker.ThrowText(loc, this.Map, "HarvestFailed".Translate(), 3.65f);
						}
						else
						{
							int num2 = plant.YieldNow();
							if (num2 > 0)
							{
								Thing thing = ThingMaker.MakeThing(plant.def.plant.harvestedThingDef, null);
								thing.stackCount = num2;
								if (actor.Faction != Faction.OfPlayer)
								{
									thing.SetForbidden(true, true);
								}
								GenPlace.TryPlaceThing(thing, actor.Position, this.Map, ThingPlaceMode.Near, null);
								actor.records.Increment(RecordDefOf.PlantsHarvested);
							}
						}
					}
					plant.def.plant.soundHarvestFinish.PlayOneShot(actor);
					plant.PlantCollected();
					this.workDone = 0f;
					this.ReadyForNextToil();
					return;
				}
			};
			cut.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			cut.defaultCompleteMode = ToilCompleteMode.Never;
			cut.WithEffect(EffecterDefOf.Harvest, TargetIndex.A);
			cut.WithProgressBar(TargetIndex.A, () => this.workDone / this.Plant.def.plant.harvestWork, true, -0.5f);
			cut.PlaySustainerOrSound(() => this.Plant.def.plant.soundHarvesting);
			yield return cut;
			
			
		}
		
		//private Toil TryToSetNextClearcut()
		//{
		//	
		//}
		
		//TODO: Convert to a protected static method.
		/*private Plant findNearbyPlantToCut(IntVec3 originPos, Map map) {
			// goal: Find a cell either with a plant that doesn't belong or has a nearby sowblocker... extra work needed to find the sowblocker and if it's a plant.
			Predicate<IntVec3> validator = (IntVec3 tempCell) => isCellNotAptPlantOrGrowable(tempCell, map, this.Plant.def);
			IntVec3 checkCell;
			if (CellFinder.TryFindRandomCellNear(originPos, map, 5, validator, out checkCell))
			{
				Thing checkThing = checkCell.GetThingList(map).FirstOrDefault(t => t is Plant);
				if (checkThing as Plant != null)
					return (Plant)checkThing;
				checkThing = GenPlant.AdjacentSowBlocker(this.Plant.def, checkCell, map);
				if (ReservationUtility.CanReserveAndReach(GetActor(), checkThing, PathEndMode.Touch, DangerUtility.NormalMaxDanger(GetActor())))
					return (Plant)checkThing;
			}
			return null;
		}
		
		private bool isCellNotAptPlantOrGrowable(IntVec3 cell, Map map, ThingDef plantDef) {
			// concept: If the cell being checked has a plant other than what we are wanting to plant (or what we want to plant's blueprint) we have a hit.
			Plant checkPlant = (Plant)cell.GetThingList(map).FirstOrDefault(t => t is Plant);
			if (checkPlant != null && (checkPlant.def != plantDef || checkPlant.def != plantDef.blueprintDef))
				return ReservationUtility.CanReserveAndReach(GetActor(), checkPlant, PathEndMode.Touch, DangerUtility.NormalMaxDanger(GetActor()));
			// concept: if the cell being checked has a sowblocker nearby, return true.
			Thing checkThing = GenPlant.AdjacentSowBlocker(plantDef, cell, map);
			if (checkThing as Plant != null)
				return ReservationUtility.CanReserveAndReach(GetActor(), checkThing, PathEndMode.Touch, DangerUtility.NormalMaxDanger(GetActor()));
			return false;
		}*/
		
	}
}