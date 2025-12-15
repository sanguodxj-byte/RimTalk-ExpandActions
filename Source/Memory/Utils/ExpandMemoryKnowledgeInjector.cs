using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using RimWorld;

namespace RimTalkExpandActions.Memory.Utils
{
    /// <summary>
    /// RimTalk-ExpandMemory 常识库手动注入器
    /// 
    /// 用户说明：
    /// - 常识库是存档绑定数据，新建游戏或加载时自动注入
    /// - 用户需要在加载存档后通过 Mod 设置进行手动注入
    /// - 每个存档拥有独立的常识库实例
    /// 
    /// 更新日志 v3.0：
    /// - 使用新的 ImportFromText(string, bool) API
    /// - 移除所有换行符和多余标点符号
    /// - 优化规则内容格式为单行紧凑格式
    /// </summary>
    public static class ExpandMemoryKnowledgeInjector
    {
        /// <summary>
        /// 注入结果
        /// </summary>
        public class InjectionResult
        {
            public bool Success { get; set; }
            public int TotalRules { get; set; }
            public int InjectedRules { get; set; }
            public List<string> InjectedRuleNames { get; set; } = new List<string>();
            public string ErrorMessage { get; set; }
        }

        /// <summary>
        /// 获取所有行为规则的描述
        /// </summary>
        public static Dictionary<string, string> GetRuleDescriptions()
        {
            return new Dictionary<string, string>
            {
                { "招募系统", "通过对话招募 NPC 到殖民地" },
                { "社交用餐", "邀请他人共进晚餐增进关系" },
                { "投降系统", "让敌人放下武器投降" },
                { "恋爱关系", "建立或结束恋人关系" },
                { "灵感触发", "给予角色工作战斗交易灵感" },
                { "强制休息", "让角色去休息或陷入昏迷" },
                { "赠送物品", "从背包中赠送物品给他人" },
                { "社交放松", "组织多人进行社交娱乐活动" }
            };
        }

        /// <summary>
        /// 手动注入常识库到当前存档
        /// </summary>
        public static InjectionResult ManualInject()
        {
            var result = new InjectionResult();
            
            try
            {
                Log.Message("[RimTalk-ExpandActions] ═══════════ 手动注入常识库 ═══════════");
                
                // 1. 检查是否有活跃游戏
                if (Current.Game == null || Find.World == null)
                {
                    result.ErrorMessage = "请先加载或创建游戏存档";
                    Log.Warning($"[RimTalk-ExpandActions] {result.ErrorMessage}");
                    return result;
                }
                
                Log.Message("[RimTalk-ExpandActions] ? 当前有活跃游戏存档");
                
                // 2. 查找 CommonKnowledgeLibrary 类型
                Type commonKnowledgeType = FindType("RimTalk.Memory.CommonKnowledgeLibrary");
                if (commonKnowledgeType == null)
                {
                    result.ErrorMessage = "未找到 RimTalk-ExpandMemory，确保已安装并启用";
                    Log.Warning($"[RimTalk-ExpandActions] {result.ErrorMessage}");
                    return result;
                }
                
                Log.Message($"[RimTalk-ExpandActions] ? 找到 CommonKnowledgeLibrary: {commonKnowledgeType.FullName}");
                
                // 3. 查找 ImportFromText 静态方法
                Log.Message("[RimTalk-ExpandActions] 查找 ImportFromText 静态方法...");
                
                MethodInfo importMethod = commonKnowledgeType.GetMethod(
                    "ImportFromText",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(string), typeof(bool) },
                    null
                );
                
                if (importMethod == null)
                {
                    // 列出所有静态方法用于调试
                    Log.Warning("[RimTalk-ExpandActions] 未找到标准签名，列出所有静态方法:");
                    foreach (var method in commonKnowledgeType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        var pars = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        Log.Message($"[RimTalk-ExpandActions]   - {method.Name}({pars}) -> {method.ReturnType.Name}");
                    }
                    
                    result.ErrorMessage = "未找到 ImportFromText 静态方法，版本不兼容";
                    Log.Error($"[RimTalk-ExpandActions] {result.ErrorMessage}");
                    return result;
                }
                
                Log.Message("[RimTalk-ExpandActions] ? 找到 ImportFromText 静态方法");
                Log.Message($"[RimTalk-ExpandActions] 方法签名: {importMethod}");
                
                // 4. 准备注入内容
                string knowledgeContent = GetKnowledgeContent();
                result.TotalRules = BehaviorRuleContents.GetAllRules().Count;
                
                Log.Message($"[RimTalk-ExpandActions] 准备注入 {result.TotalRules} 条行为规则...");
                
                // 5. 调用静态方法，null 作为第一个参数因为是静态方法
                object invokeResult = importMethod.Invoke(null, new object[]
                {
                    knowledgeContent,
                    true // overwriteExisting
                });
                
                // 6. 处理结果
                if (invokeResult is int count)
                {
                    result.InjectedRules = count;
                    result.Success = count > 0;
                    
                    // 添加规则名称列表
                    var rules = GetRuleDescriptions();
                    result.InjectedRuleNames = rules.Keys.ToList();
                    
                    if (result.Success)
                    {
                        Log.Message("[RimTalk-ExpandActions] ═══════════════════════════════════");
                        Log.Message($"[RimTalk-ExpandActions] ??? 成功注入 {count} 条规则到当前存档 ???");
                        Log.Message("[RimTalk-ExpandActions] ═══════════════════════════════════");
                        
                        foreach (var ruleName in result.InjectedRuleNames)
                        {
                            Log.Message($"[RimTalk-ExpandActions]   ? {ruleName}: {rules[ruleName]}");
                        }
                    }
                    else
                    {
                        result.ErrorMessage = "注入完成但没有规则添加，可能已存在";
                        Log.Warning($"[RimTalk-ExpandActions] {result.ErrorMessage}");
                    }
                }
                else
                {
                    result.ErrorMessage = $"注入方法返回类型不正确: {invokeResult?.GetType().Name ?? "null"}";
                    Log.Warning($"[RimTalk-ExpandActions] {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"注入失败: {ex.Message}";
                Log.Error($"[RimTalk-ExpandActions] {result.ErrorMessage}");
                Log.Error($"[RimTalk-ExpandActions] 堆栈跟踪:\n{ex.StackTrace}");
            }
            
            return result;
        }

        /// <summary>
        /// 检查常识库状态
        /// </summary>
        public static string CheckStatus()
        {
            try
            {
                if (Current.Game == null || Find.World == null)
                {
                    return "? 未加载游戏存档";
                }
                
                Type commonKnowledgeType = FindType("RimTalk.Memory.CommonKnowledgeLibrary");
                if (commonKnowledgeType == null)
                {
                    return "? 未找到 RimTalk-ExpandMemory";
                }
                
                // 检查静态方法是否存在
                MethodInfo importMethod = commonKnowledgeType.GetMethod(
                    "ImportFromText",
                    BindingFlags.Public | BindingFlags.Static
                );
                
                if (importMethod == null)
                {
                    return "? ImportFromText 方法不存在";
                }
                
                return "? RimTalk-ExpandMemory 已就绪";
            }
            catch (Exception ex)
            {
                return $"? 检查失败: {ex.Message}";
            }
        }

        #region 辅助方法

        private static Type FindType(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullTypeName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        private static string GetKnowledgeContent()
        {
            var allRules = BehaviorRuleContents.GetAllRules();
            var sb = new StringBuilder();
            
            foreach (var ruleKvp in allRules)
            {
                var rule = ruleKvp.Value;
                
                // 清理规则内容：移除所有换行符和多余标点
                string cleanContent = CleanRuleContent(rule.Content);
                
                // 格式: [标签|重要度]内容
                string formattedLine = $"[{rule.Tag}|{rule.Importance:F1}]{cleanContent}";
                
                sb.AppendLine(formattedLine);
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 清理规则内容：移除换行符和多余标点符号
        /// </summary>
        private static string CleanRuleContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
            
            // 1. 移除所有类型的换行符
            content = content.Replace("\r\n", " ");
            content = content.Replace("\r", " ");
            content = content.Replace("\n", " ");
            
            // 2. 移除多余的空格（连续空格替换为单个空格）
            while (content.Contains("  "))
            {
                content = content.Replace("  ", " ");
            }
            
            // 3. 移除字符串开头和结尾的空格
            content = content.Trim();
            
            return content;
        }

        #endregion
    }
}
