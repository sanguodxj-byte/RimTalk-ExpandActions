using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Verse;

namespace RimTalkExpandActions.Patches
{
    /// <summary>
    /// 通过 Postfix 直接在 BuildContext 返回值中注入常识库内容
    /// 不依赖 RimTalk-ExpandMemory，自主管理规则注入
    /// </summary>
    [HarmonyPatch]
    public static class BuildContextInjectorPatch
    {
        private static bool isPatched = false;

        /// <summary>
        /// 准备阶段：查找 BuildContext 方法
        /// </summary>
        static bool Prepare()
        {
            try
            {
                Type promptServiceType = AccessTools.TypeByName("RimTalk.Service.PromptService");
                if (promptServiceType == null)
                {
                    Log.Warning("[RimTalk-ExpandActions] 注入补丁: 未找到 RimTalk.Service.PromptService");
                    return false;
                }

                MethodInfo buildContextMethod = AccessTools.Method(promptServiceType, "BuildContext", new Type[] { typeof(List<Pawn>) });
                if (buildContextMethod == null)
                {
                    Log.Warning("[RimTalk-ExpandActions] 注入补丁: 未找到 BuildContext 方法");
                    return false;
                }

                isPatched = true;
                Log.Message("[RimTalk-ExpandActions] 注入补丁: 将直接注入常识库到 Prompt（不依赖 ExpandMemory）");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] BuildContextInjectorPatch.Prepare 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 指定目标方法
        /// </summary>
        static MethodBase TargetMethod()
        {
            Type promptServiceType = AccessTools.TypeByName("RimTalk.Service.PromptService");
            return AccessTools.Method(promptServiceType, "BuildContext", new Type[] { typeof(List<Pawn>) });
        }

        /// <summary>
        /// Postfix: 在 BuildContext 返回值末尾添加常识库内容
        /// </summary>
        static void Postfix(ref string __result, List<Pawn> pawns)
        {
            try
            {
                if (!isPatched || string.IsNullOrEmpty(__result))
                {
                    return;
                }

                // 检查设置
                if (RimTalkExpandActionsMod.Settings == null || !RimTalkExpandActionsMod.Settings.autoInjectRules)
                {
                    if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                    {
                        Log.Message("[RimTalk-ExpandActions] 常识库注入已禁用（设置）");
                    }
                    return;
                }

                // 获取所有规则
                var allRules = Memory.Utils.BehaviorRuleContents.GetAllRules();
                if (allRules == null || allRules.Count == 0)
                {
                    Log.Warning("[RimTalk-ExpandActions] 没有可注入的规则");
                    return;
                }

                // 构建常识库部分
                var knowledgeBuilder = new StringBuilder();
                knowledgeBuilder.AppendLine();
                knowledgeBuilder.AppendLine("━━━━━ World Knowledge ━━━━━");
                knowledgeBuilder.AppendLine("以下是重要的行为规则，请严格遵守：");
                knowledgeBuilder.AppendLine();
                
                int activeRuleCount = 0;
                foreach (var kvp in allRules)
                {
                    var ruleDef = kvp.Value;
                    
                    // 检查规则是否启用
                    if (!IsRuleEnabled(ruleDef.Id))
                    {
                        continue;
                    }
                    
                    // 格式化规则内容
                    string ruleText = $"[{ruleDef.Tag}|{ruleDef.Importance:F1}] {ruleDef.Content}";
                    knowledgeBuilder.AppendLine(ruleText);
                    knowledgeBuilder.AppendLine();
                    activeRuleCount++;
                }
                
                knowledgeBuilder.AppendLine("━━━━━━━━━━━━━━━━━━━━━━");
                knowledgeBuilder.AppendLine();

                // 只有在有启用的规则时才附加
                if (activeRuleCount > 0)
                {
                    // 附加到原始 Prompt
                    __result += knowledgeBuilder.ToString();

                    if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                    {
                        Log.Message($"[RimTalk-ExpandActions] 已注入 {activeRuleCount}/{allRules.Count} 条规则到 Prompt");
                    }
                }
                else
                {
                    if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                    {
                        Log.Message("[RimTalk-ExpandActions] 没有启用的规则需要注入");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] BuildContextInjectorPatch.Postfix 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 检查指定规则是否启用
        /// </summary>
        private static bool IsRuleEnabled(string ruleId)
        {
            var settings = RimTalkExpandActionsMod.Settings;
            if (settings == null)
            {
                return true; // 默认启用
            }

            // 根据规则 ID 检查对应的设置开关
            switch (ruleId)
            {
                case "expand-action-recruit":
                    return settings.enableRecruit;
                case "expand-action-drop-weapon":
                    return settings.enableDropWeapon;
                case "expand-action-romance":
                    return settings.enableRomance;
                case "expand-action-inspiration":
                    return settings.enableInspiration;
                case "expand-action-rest":
                    return settings.enableRest;
                case "expand-action-gift":
                    return settings.enableGift;
                case "expand-action-social-dining":
                    return settings.enableSocialDining;
                default:
                    return true; // 未知规则默认启用
            }
        }

        /// <summary>
        /// 获取补丁是否已应用
        /// </summary>
        public static bool IsPatched => isPatched;
    }
}
