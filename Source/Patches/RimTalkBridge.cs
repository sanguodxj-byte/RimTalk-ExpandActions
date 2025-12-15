using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimTalkExpandActions.Memory;

namespace RimTalkExpandActions.Patches
{
    /// <summary>
    /// RimTalk 对话桥接补丁
    /// 使用反射动态 Patch RimTalk 的对话方法，避免硬依赖
    /// </summary>
    [HarmonyPatch]
    public static class RimTalkBridge
    {
        private static Type rimTalkServiceType = null;
        private static MethodInfo targetMethod = null;
        private static bool isRimTalkAvailable = false;

        /// <summary>
        /// 准备阶段：检查 RimTalk 是否存在
        /// </summary>
        static bool Prepare()
        {
            try
            {
                // 查找 RimTalk.Service.TalkService 类型
                rimTalkServiceType = AccessTools.TypeByName("RimTalk.Service.TalkService");
                
                if (rimTalkServiceType == null)
                {
                    Log.Message("[RimTalk-ExpandActions] RimTalk Mod 未安装，跳过对话桥接补丁");
                    return false;
                }

                // 查找 GetTalk 方法 (public static string GetTalk(Pawn pawn))
                targetMethod = AccessTools.Method(rimTalkServiceType, "GetTalk", new Type[] { typeof(Pawn) });
                
                if (targetMethod == null)
                {
                    Log.Error("[RimTalk-ExpandActions] 未找到 GetTalk 方法");
                    return false;
                }

                isRimTalkAvailable = true;
                Log.Message($"[RimTalk-ExpandActions] 成功找到 RimTalk 对话方法: {targetMethod.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] RimTalkBridge.Prepare 失败: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 指定目标方法
        /// </summary>
        static MethodBase TargetMethod()
        {
            return targetMethod;
        }

        /// <summary>
        /// 后置补丁：处理返回的对话文本
        /// GetTalk 方法签名: public static string GetTalk(Pawn pawn)
        /// </summary>
        static void Postfix(Pawn pawn, ref string __result)
        {
            try
            {
                if (string.IsNullOrEmpty(__result) || pawn == null)
                {
                    return;
                }

                // 检查是否包含 JSON 指令
                if (!__result.Contains("{\"action\"") && !__result.Contains("{ \"action\""))
                {
                    return;
                }

                // 处理 AI 回复 - pawn 是说话者（对话目标）
                string cleanText = AIResponsePostProcessor.ProcessActionResponse(__result, pawn, null);

                // 更新返回值
                __result = cleanText;

                if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                {
                    Log.Message($"[RimTalk-ExpandActions] 对话已处理: {pawn.Name.ToStringShort}");
                    Log.Message($"[RimTalk-ExpandActions] 原始: {__result}");
                    Log.Message($"[RimTalk-ExpandActions] 清理: {cleanText}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] RimTalkBridge.Postfix 执行失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取 RimTalk 是否可用
        /// </summary>
        public static bool IsRimTalkAvailable => isRimTalkAvailable;
    }
}
