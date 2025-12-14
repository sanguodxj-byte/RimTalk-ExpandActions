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
                // 尝试查找 RimTalk.Service.TalkService 类型
                rimTalkServiceType = AccessTools.TypeByName("RimTalk.Service.TalkService");
                
                if (rimTalkServiceType == null)
                {
                    Log.Message("[RimTalk-ExpandActions] RimTalk Mod 未安装，跳过对话桥接补丁");
                    return false;
                }

                // 尝试查找 ConsumeTalk 方法
                targetMethod = AccessTools.Method(rimTalkServiceType, "ConsumeTalk");
                
                if (targetMethod == null)
                {
                    Log.Warning("[RimTalk-ExpandActions] 未找到 RimTalk.Service.TalkService.ConsumeTalk 方法，尝试查找其他方法...");
                    
                    // 尝试其他可能的方法名
                    targetMethod = AccessTools.Method(rimTalkServiceType, "ProcessTalk");
                    
                    if (targetMethod == null)
                    {
                        targetMethod = AccessTools.Method(rimTalkServiceType, "ReceiveResponse");
                    }
                    
                    if (targetMethod == null)
                    {
                        Log.Error("[RimTalk-ExpandActions] 无法找到 RimTalk 的对话处理方法，补丁失败");
                        return false;
                    }
                }

                isRimTalkAvailable = true;
                Log.Message($"[RimTalk-ExpandActions] 成功找到 RimTalk 对话方法: {targetMethod.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] RimTalkBridge.Prepare 失败: {ex.Message}");
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
        /// 前置补丁：拦截 RimTalk 对话，处理 JSON 指令
        /// </summary>
        /// <param name="__0">第一个参数 - 通常是 speaker (Pawn)</param>
        /// <param name="__1">第二个参数 - 通常是 listener (Pawn)</param>
        /// <param name="__2">第三个参数 - 通常是 text (string)，使用 ref 修改</param>
        static void Prefix(object __0, object __1, ref object __2)
        {
            try
            {
                // 参数验证
                if (__0 == null || __1 == null || __2 == null)
                {
                    return;
                }

                // 尝试转换参数
                Pawn speaker = __0 as Pawn;
                Pawn listener = __1 as Pawn;
                string text = __2 as string;

                if (speaker == null || listener == null || string.IsNullOrEmpty(text))
                {
                    return;
                }

                // 检查是否包含 JSON 指令
                if (!text.Contains("{\"action\"") && !text.Contains("{ \"action\""))
                {
                    return;
                }

                // 处理 AI 回复，提取 JSON 指令并执行
                string cleanText = AIResponsePostProcessor.ProcessActionResponse(text, listener, speaker);

                // 更新文本为去除 JSON 后的内容
                __2 = cleanText;

                if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                {
                    Log.Message($"[RimTalk-ExpandActions] 对话已处理: {speaker.Name.ToStringShort} -> {listener.Name.ToStringShort}");
                    Log.Message($"[RimTalk-ExpandActions] 原始: {text}");
                    Log.Message($"[RimTalk-ExpandActions] 清理: {cleanText}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] RimTalkBridge.Prefix 执行失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 获取 RimTalk 是否可用
        /// </summary>
        public static bool IsRimTalkAvailable => isRimTalkAvailable;
    }
}
