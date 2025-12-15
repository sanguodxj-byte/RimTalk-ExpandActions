using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimTalkExpandActions.SocialDining
{
    /// <summary>
    /// 社交共餐互动工作器
    /// 当 A 邀请 B 吃饭时，双方立即开始社交共餐
    /// </summary>
    public class InteractionWorker_OfferFood : InteractionWorker
    {
        /// <summary>
        /// 随机选择权重 - 决定是否自动触发此交互
        /// </summary>
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            // 基础验证
            if (!IsValidInteractionPair(initiator, recipient))
            {
                return 0f;
            }

            // 检查是否可以共餐
            if (!CanOfferFood(initiator, recipient))
            {
                return 0f;
            }

            // 返回低权重，避免频繁触发
            return 0.02f;
        }

        /// <summary>
        /// 交互成功时调用 - 双方立即开始吃饭
        /// </summary>
        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, 
            out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            // 清空信件参数
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;

            // 添加心情buff
            GiveMemories(initiator, recipient);

            // 核心：立即开始社交共餐
            if (initiator != null && recipient != null)
            {
                bool success = TryStartSocialDining(initiator, recipient);
                
                if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                {
                    Log.Message($"[SocialDining] InteractionWorker: {initiator.LabelShort} 邀请 {recipient.LabelShort} 共餐 - {(success ? "成功" : "失败")}");
                }
            }
        }

        #region 核心逻辑

        /// <summary>
        /// 尝试开始社交共餐 - 为双方查找食物并创建任务
        /// </summary>
        private bool TryStartSocialDining(Pawn initiator, Pawn recipient)
        {
            // Step 1: 查找最佳食物
            Thing food = FindBestFoodForDining(initiator, recipient);
            if (food == null)
            {
                if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                {
                    Log.Warning($"[SocialDining] 找不到适合 {initiator.LabelShort} 和 {recipient.LabelShort} 共餐的食物");
                }
                return false;
            }

            // Step 2: 如果发起者持有食物，先放下
            if (initiator.carryTracker?.CarriedThing == food)
            {
                if (!initiator.carryTracker.TryDropCarriedThing(initiator.Position, ThingPlaceMode.Near, out Thing droppedFood))
                {
                    Log.Warning($"[SocialDining] {initiator.LabelShort} 无法放下食物");
                    return false;
                }
                food = droppedFood;
            }

            // Step 3: 验证食物有效性
            if (food == null || food.Destroyed || !food.Spawned)
            {
                Log.Warning("[SocialDining] 食物无效或已被销毁");
                return false;
            }

            // Step 4: 检查预留冲突（多人共餐核心逻辑）
            if (!CanBothPawnsReserveFood(initiator, recipient, food))
            {
                if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                {
                    Log.Warning($"[SocialDining] 食物预留冲突，无法开始共餐");
                }
                return false;
            }

            // Step 5: 查找餐桌（可选）
            Building table = FoodSharingUtility.TryFindTableForTwo(initiator.Map, initiator, recipient, 40f);

            // Step 6: 创建任务
            Job initiatorJob = CreateDiningJob(initiator, food, table, recipient);
            Job recipientJob = CreateDiningJob(recipient, food, table, initiator);

            // Step 7: 强制指派任务（关键！）
            bool initiatorStarted = StartDiningJob(initiator, initiatorJob);
            bool recipientStarted = StartDiningJob(recipient, recipientJob);

            // Step 8: 处理失败情况
            if (!initiatorStarted || !recipientStarted)
            {
                // 如果一方失败，取消另一方
                if (initiatorStarted)
                {
                    initiator.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
                if (recipientStarted)
                {
                    recipient.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
                
                return false;
            }

            // Step 9: 标记食物为共享
            FoodSharingUtility.MarkFoodAsShared(food, initiator, recipient);

            if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
            {
                Log.Message($"[SocialDining] ? {initiator.LabelShort} 和 {recipient.LabelShort} 开始社交共餐");
            }

            return true;
        }

        /// <summary>
        /// 查找最佳食物 - 优先背包，然后地图
        /// </summary>
        private Thing FindBestFoodForDining(Pawn pawn1, Pawn pawn2)
        {
            // 优先级 1: 发起者背包
            Thing food = FindFoodInInventory(pawn1);
            if (food != null) return food;

            // 优先级 2: 接收者背包
            food = FindFoodInInventory(pawn2);
            if (food != null) return food;

            // 优先级 3: 地图上距离中点最近的食物
            food = FindFoodOnMap(pawn1, pawn2);
            if (food != null) return food;

            return null;
        }

        /// <summary>
        /// 在背包中查找食物
        /// </summary>
        private Thing FindFoodInInventory(Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null)
            {
                return null;
            }

            Thing bestFood = null;
            float bestScore = float.MinValue;

            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (!IsFoodValidForDining(pawn, thing))
                {
                    continue;
                }

                float score = GetFoodQualityScore(thing);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestFood = thing;
                }
            }

            return bestFood;
        }

        /// <summary>
        /// 在地图上查找食物
        /// </summary>
        private Thing FindFoodOnMap(Pawn pawn1, Pawn pawn2)
        {
            if (pawn1?.Map == null)
            {
                return null;
            }

            // 计算中点位置
            IntVec3 midPoint = new IntVec3(
                (pawn1.Position.x + pawn2.Position.x) / 2,
                0,
                (pawn1.Position.z + pawn2.Position.z) / 2
            );

            ThingRequest request = ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);
            TraverseParms traverseParms = TraverseParms.For(pawn1, Danger.Deadly, TraverseMode.ByPawn, false);

            return GenClosest.ClosestThingReachable(
                midPoint,
                pawn1.Map,
                request,
                PathEndMode.Touch,
                traverseParms,
                45f,
                t => IsFoodValidForDining(pawn1, t) && IsFoodValidForDining(pawn2, t)
            );
        }

        /// <summary>
        /// 检查食物是否适合共餐
        /// </summary>
        private bool IsFoodValidForDining(Pawn pawn, Thing food)
        {
            if (food == null || food.def == null)
            {
                return false;
            }

            // 必须是可食用的
            if (food.def.ingestible == null || !food.def.IsIngestible)
            {
                return false;
            }

            // 必须现在可以吃
            if (!food.IngestibleNow)
            {
                return false;
            }

            // 不能被禁止
            if (food.IsForbidden(pawn))
            {
                return false;
            }

            // 检查是否已被他人共享
            SharedFoodTracker tracker = food.TryGetComp<SharedFoodTracker>();
            if (tracker != null && tracker.ActiveEatersCount >= 2)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 计算食物质量分数
        /// </summary>
        private float GetFoodQualityScore(Thing food)
        {
            float nutrition = food.GetStatValue(StatDefOf.Nutrition, true);
            float preferability = (float)(food.def.ingestible?.preferability ?? FoodPreferability.RawBad);
            
            // 营养值 + 偏好度 * 10
            return nutrition + (preferability * 10f);
        }

        /// <summary>
        /// 检查双方是否都能预留食物
        /// </summary>
        private bool CanBothPawnsReserveFood(Pawn pawn1, Pawn pawn2, Thing food)
        {
            if (food == null || pawn1 == null || pawn2 == null)
            {
                return false;
            }

            // 检查 pawn1 能否预留
            if (!pawn1.CanReserve(food, 1, -1, null, false))
            {
                Pawn reserver = pawn1.Map?.reservationManager?.FirstRespectedReserver(food, pawn1);
                if (reserver != pawn2)
                {
                    // 被第三方预留
                    return false;
                }
            }

            // 检查 pawn2 能否预留
            if (!pawn2.CanReserve(food, 1, -1, null, false))
            {
                Pawn reserver = pawn2.Map?.reservationManager?.FirstRespectedReserver(food, pawn2);
                if (reserver != pawn1)
                {
                    // 被第三方预留
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 创建社交用餐任务
        /// </summary>
        private Job CreateDiningJob(Pawn eater, Thing food, Building table, Pawn diningPartner)
        {
            Job job = JobMaker.MakeJob(SocialDiningDefOf.SocialDine, food, table, diningPartner);
            job.count = 1; // 只吃一份
            return job;
        }

        /// <summary>
        /// 启动用餐任务（强制指派）
        /// </summary>
        private bool StartDiningJob(Pawn pawn, Job job)
        {
            if (pawn?.jobs == null || job == null)
            {
                return false;
            }

            // 使用 TryTakeOrderedJob 强制指派
            bool success = pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc, false);

            if (success && RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
            {
                Log.Message($"[SocialDining] {pawn.LabelShort} 接受社交共餐任务");
            }
            else if (!success)
            {
                Log.Warning($"[SocialDining] {pawn.LabelShort} 无法接受社交共餐任务");
            }

            return success;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证交互双方有效性
        /// </summary>
        private bool IsValidInteractionPair(Pawn initiator, Pawn recipient)
        {
            if (initiator == null || recipient == null)
            {
                return false;
            }

            if (initiator.Dead || recipient.Dead)
            {
                return false;
            }

            if (!initiator.Spawned || !recipient.Spawned)
            {
                return false;
            }

            if (initiator.Map != recipient.Map)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查是否可以邀请共餐
        /// </summary>
        private bool CanOfferFood(Pawn initiator, Pawn recipient)
        {
            // 检查饥饿度
            if (initiator?.needs?.food == null || recipient?.needs?.food == null)
            {
                return false;
            }

            // 如果双方都不饿，不触发
            if (initiator.needs.food.CurLevelPercentage > 0.9f && 
                recipient.needs.food.CurLevelPercentage > 0.9f)
            {
                return false;
            }

            // 检查是否能找到食物
            Thing food = FindBestFoodForDining(initiator, recipient);
            return food != null;
        }

        /// <summary>
        /// 添加记忆（心情buff）
        /// </summary>
        private void GiveMemories(Pawn initiator, Pawn recipient)
        {
            if (initiator?.needs?.mood != null)
            {
                initiator.needs.mood.thoughts.memories.TryGainMemory(SocialDiningDefOf.OfferedFood, recipient);
            }

            if (recipient?.needs?.mood != null)
            {
                recipient.needs.mood.thoughts.memories.TryGainMemory(SocialDiningDefOf.ReceivedFoodOffer, initiator);
            }
        }

        #endregion
    }
}
