using HarmonyLib;
using RimWorld;
using Verse;
using RimTalkExpandActions.SocialDining;

namespace RimTalkExpandActions.Patches
{
    /// <summary>
    /// 社交用餐相关的 Harmony 补丁
    /// 防止共享食物被意外销毁
    /// </summary>
    [HarmonyPatch]
    public static class SocialDiningPatches
    {
        /// <summary>
        /// 核心补丁：防止食物在被多人共享时被意外销毁
        /// 
        /// 场景：A 和 B 同时吃一份食物
        /// - A 吃完后想销毁食物
        /// - 但 B 还没吃完
        /// - 此补丁返回 false 阻止销毁
        /// - 直到 B 也吃完（B 是最后一个用餐者）才真正销毁
        /// </summary>
        [HarmonyPatch(typeof(Thing), "Destroy")]
        public static class Patch_Thing_Destroy
        {
            [HarmonyPrefix]
            public static bool Prefix(Thing __instance, DestroyMode mode)
            {
                // 只检查可食用物品
                if (__instance.def.IsIngestible)
                {
                    SharedFoodTracker tracker = __instance.TryGetComp<SharedFoodTracker>();
                    if (tracker != null && tracker.IsBeingShared && tracker.ActiveEatersCount > 0)
                    {
                        // 防止销毁 - 还有人在吃
                        if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                        {
                            Log.Warning($"[SocialDiningPatches] 阻止销毁共享食物 {__instance.Label}，" +
                                       $"还有 {tracker.ActiveEatersCount} 个用餐者");
                        }
                        return false; // 返回 false 阻止销毁
                    }
                }

                return true; // 允许正常销毁
            }
        }

        /// <summary>
        /// 优化补丁：防止共享食物被其他小人选取
        /// 如果食物已经被两人使用，不让第三者选取
        /// </summary>
        [HarmonyPatch(typeof(FoodUtility), "BestFoodSourceOnMap")]
        public static class Patch_FoodUtility_BestFoodSourceOnMap
        {
            [HarmonyPostfix]
            public static void Postfix(Pawn getter, ref Thing __result)
            {
                // 如果选中的食物正在被共享，排除它
                if (__result != null && __result.def.IsIngestible)
                {
                    SharedFoodTracker tracker = __result.TryGetComp<SharedFoodTracker>();
                    if (tracker != null && tracker.ActiveEatersCount >= 2)
                    {
                        // 此食物已经被两人使用，不再提供给第三者
                        if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                        {
                            Log.Message($"[SocialDiningPatches] 排除已共享的食物 {__result.Label}，" +
                                       $"当前有 {tracker.ActiveEatersCount} 个用餐者");
                        }
                        __result = null;
                    }
                }
            }
        }
    }
}
