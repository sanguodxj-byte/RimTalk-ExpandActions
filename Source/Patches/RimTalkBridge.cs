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
        private static Type pawnStateType = null;
        private static Type talkResponseType = null;
        private static MethodInfo targetMethod = null;
        private static FieldInfo textField = null;
        private static FieldInfo speakerField = null;
        private static FieldInfo listenerField = null;
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

                // 查找 PawnState 类型
                pawnStateType = AccessTools.TypeByName("RimTalk.Data.PawnState");
                if (pawnStateType == null)
                {
                    Log.Error("[RimTalk-ExpandActions] 未找到 RimTalk.Data.PawnState 类型");
                    return false;
                }

                // 查找 TalkResponse 类型
                talkResponseType = AccessTools.TypeByName("RimTalk.Data.TalkResponse");
                if (talkResponseType == null)
                {
                    Log.Error("[RimTalk-ExpandActions] 未找到 RimTalk.Data.TalkResponse 类型");
                    return false;
                }

                // 查找 ConsumeTalk 方法
                targetMethod = AccessTools.Method(rimTalkServiceType, "ConsumeTalk", new Type[] { pawnStateType });
                
                if (targetMethod == null)
                {
                    Log.Error("[RimTalk-ExpandActions] 未找到 ConsumeTalk 方法");
                    return false;
                }

                // 查找 TalkResponse.Text 字段
                textField = AccessTools.Field(talkResponseType, "Text");
                if (textField == null)
                {
                    // 尝试其他可能的字段名
                    textField = AccessTools.Field(talkResponseType, "text");
                }

                // 查找 PawnState 中的 Pawn 字段
                speakerField = AccessTools.Field(pawnStateType, "Pawn");
                if (speakerField == null)
                {
                    speakerField = AccessTools.Field(pawnStateType, "pawn");
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
        /// 后置补丁：处理返回的 TalkResponse
        /// </summary>
        static void Postfix(object pawnState, ref object __result)
        {
            try
            {
                if (__result == null || pawnState == null)
                {
                    return;
                }

                // 获取说话者
                Pawn speaker = speakerField?.GetValue(pawnState) as Pawn;
                if (speaker == null)
                {
                    return;
                }

                // 获取文本
                string text = textField?.GetValue(__result) as string;
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                // 检查是否包含 JSON 指令
                if (!text.Contains("{\"action\"") && !text.Contains("{ \"action\""))
                {
                    return;
                }

                // 处理 AI 回复 - 注意：在这种情况下，listener 可能是 null
                // 我们使用 speaker 作为 targetPawn（对话目标）
                string cleanText = AIResponsePostProcessor.ProcessActionResponse(text, speaker, null);

                // 更新 TalkResponse 的文本
                if (textField != null)
                {
                    textField.SetValue(__result, cleanText);
                }

                if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                {
                    Log.Message($"[RimTalk-ExpandActions] 对话已处理: {speaker.Name.ToStringShort}");
                    Log.Message($"[RimTalk-ExpandActions] 原始: {text}");
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
