using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalkExpandActions.SocialDining
{
    public class JobDriver_SocialDine : JobDriver
    {
        private const TargetIndex FoodInd = TargetIndex.A;
        private const TargetIndex TableInd = TargetIndex.B;
        private const TargetIndex PartnerInd = TargetIndex.C;

        private Thing Food => job.GetTarget(FoodInd).Thing;
        private Pawn DiningPartner => (Pawn)job.GetTarget(PartnerInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(Food, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            if (job.targetB.IsValid && job.targetB.HasThing)
            {
                if (!pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed))
                {
                    return false;
                }
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(FoodInd);
            this.FailOn(() => DiningPartner == null || DiningPartner.Dead || !DiningPartner.Spawned);

            // 标记食物为共享
            yield return Toils_General.Do(delegate
            {
                FoodSharingUtility.MarkFoodAsShared(Food, pawn, DiningPartner);
            });

            // 前往食物位置
            yield return Toils_Goto.GotoThing(FoodInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(FoodInd);

            // 拾取食物
            yield return Toils_Haul.StartCarryThing(FoodInd, false, true);

            // 如果有桌椅，前往就座
            Toil gotoTable = Toils_Goto.GotoThing(TableInd, PathEndMode.OnCell);
            gotoTable.FailOnDestroyedOrNull(TableInd);
            gotoTable.initAction = delegate
            {
                if (!job.targetB.IsValid || !job.targetB.HasThing)
                {
                    JumpToToil(GetEatToil());
                }
            };
            yield return gotoTable;

            // 坐下
            Toil sitDown = new Toil();
            sitDown.tickAction = delegate
            {
                if (job.targetB.IsValid && job.targetB.HasThing)
                {
                    Building chair = job.targetB.Thing as Building;
                    if (chair != null && pawn.Position == chair.Position)
                    {
                        JumpToToil(GetEatToil());
                    }
                }
            };
            sitDown.defaultCompleteMode = ToilCompleteMode.Delay;
            sitDown.defaultDuration = 60;
            yield return sitDown;

            // 吃饭并互动
            Toil eatToil = GetEatToil();
            yield return eatToil;

            // 添加心情buff
            yield return Toils_General.Do(delegate
            {
                // 添加互动
                if (pawn != null && DiningPartner != null && pawn.Spawned && DiningPartner.Spawned)
                {
                    pawn.interactions.TryInteractWith(DiningPartner, SocialDiningDefOf.OfferFood);
                }

                if (pawn.needs?.mood != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(SocialDiningDefOf.AteWithColonist);
                }

                if (DiningPartner?.needs?.mood != null)
                {
                    DiningPartner.needs.mood.thoughts.memories.TryGainMemory(SocialDiningDefOf.AteWithColonist);
                }
            });
        }

        private Toil GetEatToil()
        {
            Toil eatToil = new Toil();
            eatToil.initAction = delegate
            {
                if (Food == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                pawn.rotationTracker.FaceTarget(DiningPartner);
            };

            eatToil.tickAction = delegate
            {
                if (Food == null || Food.Destroyed)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                if (DiningPartner != null && DiningPartner.Spawned)
                {
                    pawn.rotationTracker.FaceTarget(DiningPartner);

                    if (pawn.IsHashIntervalTick(250))
                    {
                        pawn.interactions.TryInteractWith(DiningPartner, SocialDiningDefOf.OfferFood);
                    }
                }

                float nutrition = Food.GetStatValue(StatDefOf.Nutrition, true);
                float eatPerTick = nutrition / GenDate.TicksPerHour * 2f;
                
                if (pawn.needs.food != null)
                {
                    float currentLevel = pawn.needs.food.CurLevel;
                    pawn.needs.food.CurLevel = currentLevel + eatPerTick;
                }

                if (pawn.IsHashIntervalTick(50) && Food.stackCount > 0)
                {
                    Food.stackCount--;
                    if (Food.stackCount <= 0)
                    {
                        Food.Destroy();
                        EndJobWith(JobCondition.Succeeded);
                    }
                }
            };

            eatToil.defaultCompleteMode = ToilCompleteMode.Delay;
            eatToil.defaultDuration = GenDate.TicksPerHour;
            eatToil.WithProgressBar(FoodInd, delegate
            {
                if (Food == null)
                {
                    return 1f;
                }
                return 1f - (float)Food.stackCount / (float)(Food.stackCount + 1);
            });

            eatToil.FailOnCannotTouch(FoodInd, PathEndMode.Touch);
            eatToil.AddFinishAction(delegate
            {
                if (Food != null && !Food.Destroyed)
                {
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
                }
            });

            return eatToil;
        }

        public override string GetReport()
        {
            if (DiningPartner != null)
            {
                return "与" + DiningPartner.NameShortColored + "共同进餐";
            }
            return base.GetReport();
        }
    }
}
