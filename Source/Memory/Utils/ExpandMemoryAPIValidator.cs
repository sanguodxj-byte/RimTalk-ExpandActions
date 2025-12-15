using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace RimTalkExpandActions.Memory.Utils
{
    /// <summary>
    /// RimTalk-ExpandMemory API 验证工具
    /// 用于检查可用的 API 方法
    /// </summary>
    public static class ExpandMemoryAPIValidator
    {
        /// <summary>
        /// 验证 ImportFromText API 是否可用
        /// </summary>
        public static void ValidateImportFromTextAPI()
        {
            try
            {
                Log.Message("[API验证] ═══════════ 开始验证 ImportFromText API ═══════════");
                
                // 1. 查找 CommonKnowledgeLibrary 类型
                Type commonKnowledgeType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name.Contains("RimTalk") && assembly.GetName().Name.Contains("ExpandMemory"))
                    {
                        Log.Message($"[API验证] 找到程序集: {assembly.GetName().Name} (v{assembly.GetName().Version})");
                        
                        commonKnowledgeType = assembly.GetType("RimTalk.Memory.CommonKnowledgeLibrary");
                        if (commonKnowledgeType != null)
                        {
                            Log.Message($"[API验证] ? 找到类型: {commonKnowledgeType.FullName}");
                            break;
                        }
                    }
                }
                
                if (commonKnowledgeType == null)
                {
                    Log.Error("[API验证] ? 未找到 CommonKnowledgeLibrary 类型");
                    return;
                }
                
                // 2. 列出所有公共静态方法
                Log.Message("[API验证] ─── 公共静态方法列表 ───");
                var staticMethods = commonKnowledgeType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var method in staticMethods)
                {
                    var parameters = method.GetParameters();
                    var paramStr = string.Join(", ", Array.ConvertAll(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                    Log.Message($"[API验证]   {method.Name}({paramStr}) -> {method.ReturnType.Name}");
                }
                
                // 3. 检查 ImportFromText 方法
                Log.Message("[API验证] ─── 检查 ImportFromText 方法 ───");
                
                var importFromText = commonKnowledgeType.GetMethod(
                    "ImportFromText",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new Type[] { typeof(string), typeof(bool) },
                    null
                );
                
                if (importFromText != null)
                {
                    Log.Message("[API验证] ??? ImportFromText(string, bool) 可用！");
                    Log.Message($"[API验证]   返回类型: {importFromText.ReturnType.Name}");
                    Log.Message($"[API验证]   是否静态: {importFromText.IsStatic}");
                    Log.Message($"[API验证]   是否公共: {importFromText.IsPublic}");
                }
                else
                {
                    Log.Warning("[API验证] ? ImportFromText(string, bool) 不可用");
                    
                    // 查找任何名为 ImportFromText 的方法
                    var anyImportFromText = commonKnowledgeType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Where(m => m.Name == "ImportFromText")
                        .ToList();
                    
                    if (anyImportFromText.Any())
                    {
                        Log.Warning($"[API验证] 找到 {anyImportFromText.Count} 个 ImportFromText 重载:");
                        foreach (var m in anyImportFromText)
                        {
                            var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                            Log.Warning($"[API验证]   - {m.Name}({pars}) -> {m.ReturnType.Name}");
                        }
                    }
                }
                
                // 4. 检查其他可能的导入方法
                Log.Message("[API验证] ─── 检查其他导入方法 ───");
                
                var allImportMethods = staticMethods.Where(m => m.Name.Contains("Import")).ToList();
                if (allImportMethods.Any())
                {
                    foreach (var m in allImportMethods)
                    {
                        var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        Log.Message($"[API验证]   ? {m.Name}({pars}) -> {m.ReturnType.Name}");
                    }
                }
                else
                {
                    Log.Warning("[API验证] 未找到任何 Import 相关方法");
                }
                
                // 5. 检查 AddEntry 方法（备选方案）
                var addEntry = commonKnowledgeType.GetMethod(
                    "AddEntry",
                    BindingFlags.Public | BindingFlags.Static
                );
                
                if (addEntry != null)
                {
                    var pars = string.Join(", ", addEntry.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Log.Message($"[API验证] ? AddEntry 可用: {addEntry.Name}({pars})");
                }
                
                Log.Message("[API验证] ═══════════ 验证完成 ═══════════");
            }
            catch (Exception ex)
            {
                Log.Error($"[API验证] 验证过程异常: {ex.Message}");
                Log.Error($"[API验证] 堆栈跟踪:\n{ex.StackTrace}");
            }
        }
    }
}
