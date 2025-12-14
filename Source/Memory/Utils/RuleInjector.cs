using System;
using System.Linq;
using RimWorld;
using Verse;

namespace RimTalkExpandActions.Memory.Utils
{
    /// <summary>
    /// 启动时自动注入系统规则到常识库
    /// 通过 CrossModRecruitRuleInjector 实现跨 Mod 注入
    /// </summary>
    [StaticConstructorOnStartup]
    public static class RuleInjector
    {
        static RuleInjector()
        {
            // 延迟到游戏完全初始化后执行
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                try
                {
                    // 检查是否启用自动注入
                    if (RimTalkExpandActionsMod.Settings?.autoInjectRules == true)
                    {
                        Log.Message("[RimTalk-ExpandActions] 自动注入模式已启用，尝试注入所有行为规则...");
                        InjectAllRules();
                    }
                    else
                    {
                        Log.Message("[RimTalk-ExpandActions] 自动注入已禁用，需要手动在设置中注入规则");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[RimTalk-ExpandActions] RuleInjector 初始化失败: {ex.Message}\n{ex.StackTrace}");
                }
            });
        }

        /// <summary>
        /// 注入所有行为规则到常识库
        /// </summary>
        private static void InjectAllRules()
        {
            try
            {
                var settings = RimTalkExpandActionsMod.Settings;
                float importance = settings?.ruleImportance ?? 1.0f;

                // 获取所有规则定义
                var allRules = BehaviorRuleContents.GetAllRules();
                
                int successCount = 0;
                int failCount = 0;

                foreach (var ruleKvp in allRules)
                {
                    string ruleId = ruleKvp.Key;
                    RuleDefinition ruleDef = ruleKvp.Value;

                    // 使用自定义内容（如果有）仅用于招募规则
                    string content = null;
                    if (ruleId == "sys-rule-recruit" && !string.IsNullOrWhiteSpace(settings?.customRecruitRuleContent))
                    {
                        content = settings.customRecruitRuleContent;
                    }
                    else
                    {
                        content = ruleDef.Content;
                    }

                    // 使用跨 Mod 注入器
                    bool success = CrossModRecruitRuleInjector.TryInjectRule(
                        ruleDef.Id,
                        ruleDef.Tag,
                        content,
                        ruleDef.Keywords,
                        ruleDef.Importance
                    );

                    if (success)
                    {
                        successCount++;
                        if (settings?.enableDetailedLogging == true)
                        {
                            Log.Message($"[RimTalk-ExpandActions] ? 成功注入规则: {ruleId}");
                        }
                    }
                    else
                    {
                        failCount++;
                        Log.Warning($"[RimTalk-ExpandActions] ? 注入规则失败: {ruleId}");
                    }
                }

                Log.Message($"[RimTalk-ExpandActions] 规则注入完成: 成功 {successCount} 条，失败 {failCount} 条");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] InjectAllRules 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 注入单条招募规则（兼容旧版本）
        /// </summary>
        private static void InjectRecruitmentRule()
        {
            try
            {
                var settings = RimTalkExpandActionsMod.Settings;
                
                string customContent = string.IsNullOrWhiteSpace(settings?.customRecruitRuleContent) 
                    ? null 
                    : settings.customRecruitRuleContent;

                float importance = settings?.ruleImportance ?? 1.0f;

                // 使用跨 Mod 注入器
                bool success = CrossModRecruitRuleInjector.TryInjectRecruitRule(importance, customContent);

                if (success)
                {
                    Log.Message("[RimTalk-ExpandActions] ? 自动注入招募规则成功");
                }
                else
                {
                    Log.Warning("[RimTalk-ExpandActions] 自动注入招募规则失败，请手动在设置中注入");
                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("[RimTalk-ExpandActions] InjectRecruitmentRule 失败: {0}\n{1}", ex.Message, ex.StackTrace));
            }
        }
    }
}
