using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalkExpandActions.SocialDining
{
    public static class FoodSharingUtility
    {
        private const float FoodSearchRadius = 45f;
        private const float ChairSearchRadius = 30f;

        public static bool TryFindSharedFood(Pawn sharer, Pawn recipient, out Thing food)
        {
            food = null;

            if (sharer == null || recipient == null || sharer.Map == null)
            {
                return false;
            }

            if (TryFindFoodInInventory(sharer, out food))
            {
                return true;
            }

            if (TryFindFoodOnMap(sharer, out food))
            {
                return true;
            }

            return false;
        }

        public static bool TryFindChair(Pawn pawn, out Building chair, float maxDistance = ChairSearchRadius)
        {
            chair = null;
            if (pawn?.Map == null)
            {
                return false;
            }

            Building bestChair = null;
            float bestScore = float.MinValue;
            List<Thing> allThings = pawn.Map.listerThings.AllThings;

            for (int i = 0; i < allThings.Count; i++)
            {
                Building building = allThings[i] as Building;
                if (building == null)
                {
                    continue;
                }

                if (!IsValidChairFor(pawn, building, maxDistance))
                {
                    continue;
                }

                float score = -pawn.Position.DistanceToSquared(building.Position);
                if (building.def.building != null && building.def.building.isSittable)
                {
                    score += 5f;
                }

                if (IsNearDiningSurface(building))
                {
                    score += 3f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestChair = building;
                }
            }

            if (bestChair != null)
            {
                chair = bestChair;
                return true;
            }

            return false;
        }

        public static SharedFoodTracker TryGetFoodTracker(Thing food)
        {
            return food?.TryGetComp<SharedFoodTracker>();
        }

        public static void MarkFoodAsShared(Thing food, Pawn sharer, Pawn recipient)
        {
            TryGetFoodTracker(food)?.MarkShared(sharer, recipient);
        }

        public static bool IsFoodReservedFor(Thing food, Pawn pawn)
        {
            SharedFoodTracker tracker = TryGetFoodTracker(food);
            if (tracker == null)
            {
                return false;
            }

            return tracker.IsSharedWith(pawn) || tracker.IsSharedBy(pawn);
        }

        private static bool TryFindFoodInInventory(Pawn pawn, out Thing food)
        {
            food = null;

            var container = pawn.inventory?.innerContainer;
            if (container == null || container.Count == 0)
            {
                return false;
            }

            Thing bestThing = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < container.Count; i++)
            {
                Thing thing = container[i];
                if (!IsValidFoodToShare(pawn, thing))
                {
                    continue;
                }

                float score = GetFoodScore(thing);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestThing = thing;
                }
            }

            if (bestThing != null)
            {
                food = bestThing;
                return true;
            }

            return false;
        }

        private static bool TryFindFoodOnMap(Pawn pawn, out Thing food)
        {
            food = null;
            if (pawn.Map == null)
            {
                return false;
            }

            ThingRequest request = ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);
            
            // RimWorld 1.6 兼容：TraverseParms.For 只需要 pawn 参数
            TraverseParms traverseParms = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
            
            Thing found = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                request,
                PathEndMode.Touch,
                traverseParms,
                FoodSearchRadius,
                t => IsValidFoodToShare(pawn, t));

            if (found != null)
            {
                food = found;
                return true;
            }

            return false;
        }

        private static bool IsValidFoodToShare(Pawn pawn, Thing food)
        {
            if (pawn == null || food == null)
            {
                return false;
            }

            if (food.def.ingestible == null || !food.def.IsIngestible)
            {
                return false;
            }

            if (!food.IngestibleNow || food.IsForbidden(pawn))
            {
                return false;
            }

            if (!pawn.CanReserve(food))
            {
                return false;
            }

            SharedFoodTracker tracker = TryGetFoodTracker(food);
            if (tracker != null && tracker.IsActive && tracker.Sharer != pawn)
            {
                return false;
            }

            return true;
        }

        private static float GetFoodScore(Thing food)
        {
            float nutrition = food.GetStatValue(StatDefOf.Nutrition, true);
            float preferability = (float)(food.def.ingestible?.preferability ?? FoodPreferability.RawBad);
            return nutrition + preferability;
        }

        private static bool IsValidChairFor(Pawn pawn, Building building, float maxDistance)
        {
            if (building.Map != pawn.Map)
            {
                return false;
            }

            if (building.def.building == null || !building.def.building.isSittable)
            {
                return false;
            }

            if (maxDistance > 0f && building.Position.DistanceTo(pawn.Position) > maxDistance)
            {
                return false;
            }

            if (!pawn.CanReserveAndReach(building, PathEndMode.OnCell, Danger.Some))
            {
                return false;
            }

            if (building.IsForbidden(pawn))
            {
                return false;
            }

            return true;
        }

        private static bool IsNearDiningSurface(Thing seat)
        {
            Map map = seat.Map;
            if (map == null)
            {
                return false;
            }

            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(seat))
            {
                Building edifice = cell.GetEdifice(map);
                if (edifice?.def?.surfaceType == SurfaceType.Eat)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
