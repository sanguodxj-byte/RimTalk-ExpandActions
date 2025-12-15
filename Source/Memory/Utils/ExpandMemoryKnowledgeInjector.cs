using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld;

namespace RimTalkExpandActions.Memory.Utils
{
    /// <summary>
    /// RimTalk-ExpandMemory 常识库手动注入器
    /// 
    /// 设计说明：
    /// - 常识库是存档级别的数据，不在游戏启动时自动注入
    /// - 用户需要在加载存档后，通过 Mod 设置界面手动注入
    /// - 每个存档有独立的常识库实例
    /// 
    /// 更新日志 v2.0：
    /// - 修复：ImportFromExternalMod 是**静态方法**，不是实例方法！
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
                { "社交用餐", "邀请他人共进晚餐，增进关系" },
                { "投降系统", "让敌人放下武器投降" },
                { "恋爱关系", "建立或结束恋人关系" },
                { "灵感触发", "给予角色工作/战斗/交易灵感" },
                { "强制休息", "让角色去休息或陷入昏迷" },
                { "赠送物品", "从背包中赠送物品给他人" },
                { "社交放松", "指令多个小人进行社交娱乐活动" }
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
                Log.Message("[RimTalk-ExpandActions] ━━━━━ 手动注入常识库 ━━━━━");
                
                // 1. 检查是否有活动的游戏
                if (Current.Game == null || Find.World == null)
                {
                    result.ErrorMessage = "请先加载或创建游戏存档";
                    Log.Warning($"[RimTalk-ExpandActions] {result.ErrorMessage}");
                    return result;
                }
                
                Log.Message("[RimTalk-ExpandActions] ? 当前有活动的游戏存档");
                
                // 2. 查找 CommonKnowledgeLibrary 类型
                Type commonKnowledgeType = FindType("RimTalk.Memory.CommonKnowledgeLibrary");
                if (commonKnowledgeType == null)
                {
                    result.ErrorMessage = "未找到 RimTalk-ExpandMemory（请确保已安装并启用）";
                    Log.Warning($"[RimTalk-ExpandActions] {result.ErrorMessage}");
                    return result;
                }
                
                Log.Message($"[RimTalk-ExpandActions] ? 找到 CommonKnowledgeLibrary: {commonKnowledgeType.FullName}");
                Log.Message($"[RimTalk-ExpandActions] 程序集: {commonKnowledgeType.Assembly.GetName().Name}");
                
                // 3. 查找 ImportFromExternalMod 静态方法
                Log.Message("[RimTalk-ExpandActions] 查找 ImportFromExternalMod 静态方法...");
                
                MethodInfo importMethod = commonKnowledgeType.GetMethod(
                    "ImportFromExternalMod",
                    BindingFlags.Public | BindingFlags.Static,  // ← 关键：Static！
                    null,
                    new Type[] { typeof(string), typeof(string), typeof(bool) },
                    null
                );
                
                if (importMethod == null)
                {
                    // 列出所有静态方法帮助诊断
                    Log.Warning("[RimTalk-ExpandActions] 未找到标准签名，列出所有静态方法:");
                    foreach (var method in commonKnowledgeType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    {
                        var pars = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        Log.Message($"[RimTalk-ExpandActions]   - {method.Name}({pars}) -> {method.ReturnType.Name}");
                    }
                    
                    // 尝试查找任何名为 ImportFromExternalMod 的静态方法
                    var allImportMethods = commonKnowledgeType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Where(m => m.Name == "ImportFromExternalMod")
                        .ToList();
                    
                    if (allImportMethods.Any())
                    {
                        importMethod = allImportMethods.First();
                        Log.Message($"[RimTalk-ExpandActions] 使用找到的方法: {importMethod.Name}");
                    }
                    else
                    {
                        result.ErrorMessage = "未找到 ImportFromExternalMod 静态方法（版本不兼容）";
                        Log.Error($"[RimTalk-ExpandActions] {result.ErrorMessage}");
                        Log.Error("[RimTalk-ExpandActions] 请更新 RimTalk-ExpandMemory 到最新版本");
                        return result;
                    }
                }
                
                Log.Message("[RimTalk-ExpandActions] ? 找到 ImportFromExternalMod 静态方法");
                Log.Message($"[RimTalk-ExpandActions] 方法签名: {importMethod}");
                
                // 4. 准备规则内容
                string knowledgeContent = GetKnowledgeContent();
                result.TotalRules = 8; // 8 种行为
                
                Log.Message("[RimTalk-ExpandActions] 准备注入 8 种行为规则...");
                
                // 5. 调用静态方法（null 作为第一个参数，因为是静态方法）
                object invokeResult = importMethod.Invoke(null, new object[]
                {
                    knowledgeContent,
                    "RimTalk-ExpandActions",
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
                        Log.Message("[RimTalk-ExpandActions] ━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        Log.Message($"[RimTalk-ExpandActions] ??? 成功导入 {count} 条规则到当前存档 ???");
                        Log.Message("[RimTalk-ExpandActions] ━━━━━━━━━━━━━━━━━━━━━━━━━━");
                        
                        foreach (var ruleName in result.InjectedRuleNames)
                        {
                            Log.Message($"[RimTalk-ExpandActions]   ? {ruleName}: {rules[ruleName]}");
                        }
                    }
                    else
                    {
                        result.ErrorMessage = "注入完成，但没有规则被添加（可能已存在）";
                        Log.Warning($"[RimTalk-ExpandActions] {result.ErrorMessage}");
                    }
                }
                else
                {
                    result.ErrorMessage = $"注入方法返回了意外的类型: {invokeResult?.GetType().Name ?? "null"}";
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
                    "ImportFromExternalMod",
                    BindingFlags.Public | BindingFlags.Static
                );
                
                if (importMethod == null)
                {
                    return "? ImportFromExternalMod 方法不可用";
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
            // 使用 BehaviorRuleContents 中定义的规则内容
            var allRules = BehaviorRuleContents.GetAllRules();
            
            var lines = new List<string>();
            
            // 只输出规则，不添加注释或空行
            foreach (var ruleKvp in allRules)
            {
                var rule = ruleKvp.Value;
                // 格式：[标签|重要性]内容
                var formattedContent = $"[{rule.Tag}|{rule.Importance:F1}]{rule.Content}";
                lines.Add(formattedContent);
            }
            
            // 用换行符连接，每条规则占一行
            return string.Join("\n", lines);
        }

        #endregion
    }
}
