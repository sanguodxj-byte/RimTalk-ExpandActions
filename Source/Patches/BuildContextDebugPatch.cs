using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RimTalkExpandActions.Patches
{
    /// <summary>
    /// 调试补丁：监控 RimTalk-ExpandMemory 的 BuildContext 方法
    /// 用于验证常识库规则是否正确注入到 AI Prompt 中
    /// </summary>
    [HarmonyPatch]
    public static class BuildContextDebugPatch
    {
        private static bool isPatched = false;

        /// <summary>
        /// 准备阶段：查找 RimTalk-ExpandMemory 的 BuildContext 方法
        /// </summary>
        static bool Prepare()
        {
            try
            {
                // 查找 RimTalk.Memory.MemoryManager 类型
                Type memoryManagerType = AccessTools.TypeByName("RimTalk.Memory.MemoryManager");
                if (memoryManagerType == null)
                {
                    Log.Warning("[RimTalk-ExpandActions] 调试补丁: 未找到 RimTalk.Memory.MemoryManager，跳过 BuildContext 监控");
                    return false;
                }

                // 查找 BuildContext 方法
                MethodInfo buildContextMethod = AccessTools.Method(memoryManagerType, "BuildContext");
                if (buildContextMethod == null)
                {
                    Log.Warning("[RimTalk-ExpandActions] 调试补丁: 未找到 BuildContext 方法");
                    return false;
                }

                Log.Message("[RimTalk-ExpandActions] 调试补丁: 成功找到 BuildContext 方法，将监控 Prompt 生成");
                isPatched = true;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] BuildContextDebugPatch.Prepare 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 指定目标方法
        /// </summary>
        static MethodBase TargetMethod()
        {
            Type memoryManagerType = AccessTools.TypeByName("RimTalk.Memory.MemoryManager");
            return AccessTools.Method(memoryManagerType, "BuildContext");
        }

        /// <summary>
        /// 后置补丁：检查生成的 Prompt
        /// </summary>
        static void Postfix(ref string __result, List<Pawn> pawns)
        {
            try
            {
                if (!isPatched || __result == null)
                {
                    return;
                }

                // 只对非殖民者进行检查（避免刷屏）
                if (pawns != null && pawns.Count > 0)
                {
                    Pawn pawn = pawns[0];
                    
                    // 只检查访客、囚犯等非殖民者
                    if (!pawn.IsColonist)
                    {
                        string debugMsg = $"[RimTalk-ExpandActions] ━━━━━ Prompt 注入检查 ━━━━━\n";
                        debugMsg += $"目标: {pawn.LabelShort}\n";
                        debugMsg += $"Prompt 长度: {__result.Length} 字符\n";
                        debugMsg += $"\n检查关键词:\n";
                        
                        // 检查是否包含常识库标记
                        bool hasWorldKnowledge = __result.Contains("World Knowledge") || __result.Contains("常识库") || __result.Contains("Common Knowledge");
                        debugMsg += $"  - 常识库标记: {(hasWorldKnowledge ? "? 存在" : "? 不存在")}\n";
                        
                        // 检查是否包含招募规则
                        bool hasRecruitRule = __result.Contains("expand-action-recruit") || __result.Contains("action\": \"recruit");
                        debugMsg += $"  - 招募规则: {(hasRecruitRule ? "? 存在" : "? 不存在")}\n";
                        
                        // 检查是否包含社交用餐规则
                        bool hasSocialDiningRule = __result.Contains("expand-action-social-dining") || __result.Contains("social_dining");
                        debugMsg += $"  - 社交用餐规则: {(hasSocialDiningRule ? "? 存在" : "? 不存在")}\n";
                        
                        // 检查是否包含其他行为规则
                        bool hasDropWeaponRule = __result.Contains("expand-action-drop-weapon") || __result.Contains("drop_weapon");
                        debugMsg += $"  - 投降规则: {(hasDropWeaponRule ? "? 存在" : "? 不存在")}\n";
                        
                        bool hasRomanceRule = __result.Contains("expand-action-romance") || __result.Contains("romance");
                        debugMsg += $"  - 恋爱规则: {(hasRomanceRule ? "? 存在" : "? 不存在")}\n";
                        
                        // 统计存在的规则数量
                        int ruleCount = 0;
                        if (hasRecruitRule) ruleCount++;
                        if (hasSocialDiningRule) ruleCount++;
                        if (hasDropWeaponRule) ruleCount++;
                        if (hasRomanceRule) ruleCount++;
                        
                        debugMsg += $"\n总结: 检测到 {ruleCount}/7 条规则\n";
                        
                        // 如果没有检测到任何规则，显示错误
                        if (ruleCount == 0)
                        {
                            debugMsg += "?? 警告: Prompt 中没有检测到任何 expand-action 规则！\n";
                            debugMsg += "可能原因:\n";
                            debugMsg += "  1. 常识库注入失败\n";
                            debugMsg += "  2. RimTalk-ExpandMemory 未正确检索规则\n";
                            debugMsg += "  3. BuildContext 方法未包含常识库内容\n";
                            
                            Log.Error(debugMsg);
                        }
                        else if (ruleCount < 7)
                        {
                            debugMsg += $"?? 注意: 只检测到 {ruleCount} 条规则，期望 7 条\n";
                            Log.Warning(debugMsg);
                        }
                        else
                        {
                            debugMsg += "? 所有规则都已正确注入到 Prompt 中！\n";
                            Log.Message(debugMsg);
                        }
                        
                        debugMsg += "━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
                        
                        // 如果启用了详细日志，显示 Prompt 片段
                        if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                        {
                            // 查找并显示常识库部分（如果存在）
                            int knowledgeIndex = __result.IndexOf("World Knowledge");
                            if (knowledgeIndex < 0)
                            {
                                knowledgeIndex = __result.IndexOf("常识库");
                            }
                            if (knowledgeIndex < 0)
                            {
                                knowledgeIndex = __result.IndexOf("Common Knowledge");
                            }
                            
                            if (knowledgeIndex >= 0)
                            {
                                // 显示常识库部分的前 500 个字符
                                int endIndex = Math.Min(knowledgeIndex + 500, __result.Length);
                                string knowledgeSnippet = __result.Substring(knowledgeIndex, endIndex - knowledgeIndex);
                                Log.Message($"[RimTalk-ExpandActions] 常识库片段:\n{knowledgeSnippet}...");
                            }
                            else
                            {
                                Log.Warning("[RimTalk-ExpandActions] Prompt 中没有找到常识库部分");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] BuildContextDebugPatch.Postfix 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 检查 BuildContext 是否被正确补丁
        /// </summary>
        public static bool IsPatched => isPatched;
    }
}
