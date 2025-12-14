using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalkExpandActions.SocialDining
{
    /// <summary>
    /// 社交用餐任务驱动
    /// 核心修复：
    /// 1. 多人预留逻辑 - 允许伙伴预留同一份食物
    /// 2. 修复幽灵锁定 Bug - Override EndJobWith 强制清理追踪器
    /// 3. 修复双倍营养 Bug - Toil 7 不再增加营养
    /// 4. 同步进食动画 - 双方面对面吃饭
    /// </summary>
    public class JobDriver_SocialDine : JobDriver
    {
        private const TargetIndex FoodInd = TargetIndex.A;
        private const TargetIndex TableInd = TargetIndex.B;
        private const TargetIndex PartnerInd = TargetIndex.C;

        private Thing Food => job.GetTarget(FoodInd).Thing;
        private Thing Table => job.targetB.HasThing ? job.targetB.Thing : null;
        private Pawn Partner => (Pawn)job.GetTarget(PartnerInd).Thing;

        // 追踪器注册状态
        private bool isRegisteredWithTracker = false;

        /// <summary>
        /// 多人预留检查 - 核心逻辑
        /// 如果食物已被伙伴预留，允许继续预留
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 检查食物预留
            if (!pawn.CanReserve(Food, 1, -1, null, false))
            {
                // 检查是否被伙伴预留
                Pawn reserver = pawn.Map?.reservationManager?.FirstRespectedReserver(Food, pawn);
                if (reserver != Partner)
                {
                    if (errorOnFailed)
                    {
                        Log.Warning($"[SocialDine] {pawn.LabelShort} 无法预留食物（已被 {reserver?.LabelShort ?? "未知"} 预留）");
                    }
                    return false;
                }
                // 如果是伙伴预留的，继续尝试预留（多人共享）
            }

            // 尝试预留食物
            if (!pawn.Reserve(Food, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            // 尝试预留桌椅（如果有）
            if (job.targetB.IsValid && job.targetB.HasThing)
            {
                // 桌椅允许被伙伴共享，不强制检查
                pawn.Reserve(job.targetB, job, 1, -1, null, false);
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // --- Toil 0: 失败条件 + 清理处理 ---
            this.FailOnDestroyedNullOrForbidden(FoodInd);
            this.FailOn(() => Partner == null || Partner.Dead || !Partner.Spawned);
            this.FailOn(() => Food == null || Food.Destroyed);
            
            // 添加失败时的清理回调（需要接受 JobCondition 参数）
            this.AddFinishAction((JobCondition condition) => CleanupTracker());

            // --- Toil 1: 注册到追踪器 ---
            yield return Toils_General.Do(delegate
            {
                if (Food != null && !Food.Destroyed)
                {
                    ThingWithComps foodWithComps = Food as ThingWithComps;
                    if (foodWithComps != null)
                    {
                        SharedFoodTracker tracker = foodWithComps.TryGetComp<SharedFoodTracker>();
                        if (tracker != null && !isRegisteredWithTracker)
                        {
                            tracker.RegisterEater(pawn);
                            isRegisteredWithTracker = true;
                        }
                    }
                }
            });

            // --- Toil 2: 前往食物位置 ---
            yield return Toils_Goto.GotoThing(FoodInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(FoodInd);

            // --- Toil 3: 拾取食物 ---
            yield return Toils_Haul.StartCarryThing(FoodInd, false, true)
                .FailOnDestroyedNullOrForbidden(FoodInd);

            // --- Toil 4: 前往餐桌（如果有） ---
            Toil gotoTable = Toils_Goto.GotoThing(TableInd, PathEndMode.OnCell);
            gotoTable.FailOn(() => Table != null && (Table.Destroyed || !Table.Spawned));
            gotoTable.initAction = delegate
            {
                // 如果没有桌子，直接跳到进食
                if (Table == null)
                {
                    JumpToToil(MakeEatingToil());
                }
            };
            yield return gotoTable;

            // --- Toil 5: 放下食物 ---
            Toil dropFood = new Toil
            {
                initAction = delegate
                {
                    if (pawn.carryTracker?.CarriedThing == Food)
                    {
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return dropFood;

            // --- Toil 6: 吃饭（同步动画） ---
            Toil eatToil = MakeEatingToil();
            yield return eatToil;

            // --- Toil 7: 完成用餐（不增加营养！） ---
            Toil finishEating = new Toil
            {
                initAction = delegate
                {
                    // 注销追踪器
                    CleanupTracker();
                    
                    // 添加心情加成
                    if (Partner != null && !Partner.Dead && pawn.needs?.mood?.thoughts?.memories != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(SocialDiningDefOf.AteWithColonist, Partner);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finishEating;
        }

        /// <summary>
        /// 创建进食 Toil - 同步动画和营养增加
        /// </summary>
        private Toil MakeEatingToil()
        {
            // 计算进食时间和营养
            float nutritionTotal = FoodUtility.GetNutrition(pawn, Food, Food.def);
            int ticksToEat = (int)(nutritionTotal * 1600f); // 标准进食时间
            float nutritionPerTick = nutritionTotal / ticksToEat;

            Toil eatFood = new Toil
            {
                initAction = delegate
                {
                    pawn.pather.StopDead();
                    pawn.jobs.curDriver.ticksLeftThisToil = ticksToEat;
                },
                tickAction = delegate
                {
                    // 面向伙伴
                    if (Partner != null && Partner.Spawned)
                    {
                        pawn.rotationTracker.FaceTarget(Partner);
                    }
                    
                    // 逐渐增加营养（这里增加，Toil 7 不再增加）
                    if (pawn.needs?.food != null)
                    {
                        pawn.needs.food.CurLevel += nutritionPerTick;
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = ticksToEat,
                handlingFacing = true
            };
            
            // 添加进食效果
            if (Food?.def?.ingestible?.ingestEffect != null)
            {
                eatFood.WithEffect(() => Food.def.ingestible.ingestEffect, FoodInd);
            }
            eatFood.WithProgressBar(FoodInd, () => 1f - (float)pawn.jobs.curDriver.ticksLeftThisToil / ticksToEat, interpolateBetweenActorAndTarget: false);
            if (Food?.def?.ingestible?.ingestSound != null)
            {
                eatFood.PlaySustainerOrSound(() => Food.def.ingestible.ingestSound);
            }
            
            return eatFood;
        }

        /// <summary>
        /// 清理追踪器 - 防止幽灵锁定
        /// </summary>
        private void CleanupTracker()
        {
            if (isRegisteredWithTracker && Food != null && !Food.Destroyed)
            {
                ThingWithComps foodWithComps = Food as ThingWithComps;
                if (foodWithComps != null)
                {
                    SharedFoodTracker tracker = foodWithComps.TryGetComp<SharedFoodTracker>();
                    if (tracker != null)
                    {
                        bool isLastEater = tracker.UnregisterEater(pawn);
                        isRegisteredWithTracker = false;
                        
                        // 幸存者销毁逻辑：只有最后一个吃完的人才销毁食物
                        if (isLastEater && !Food.Destroyed)
                        {
                            Food.Destroy(DestroyMode.Vanish);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 寻路失败时也要清理
        /// </summary>
        public override void Notify_PatherFailed()
        {
            base.Notify_PatherFailed();
            CleanupTracker();
        }

        public override string GetReport()
        {
            if (Partner != null)
            {
                return "与 " + Partner.NameShortColored + " 共同进餐";
            }
            return base.GetReport();
        }
    }
}
