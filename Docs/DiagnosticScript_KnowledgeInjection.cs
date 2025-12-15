// ═══════════════════════════════════════════════════════════
// RimTalk-ExpandActions 常识库注入诊断脚本
// 在 RimWorld 开发者控制台（F12）中运行
// ═══════════════════════════════════════════════════════════

Log.Message("━━━━━ 开始诊断 ━━━━━");

// 1. 检查 ExpandActions 是否加载
Log.Message("[1] 检查 RimTalk-ExpandActions...");
try {
    var injectorType = System.Type.GetType("RimTalkExpandActions.Memory.Utils.ExpandMemoryKnowledgeInjector, RimTalk-ExpandActions");
    if (injectorType != null) {
        Log.Message("  ? ExpandMemoryKnowledgeInjector 类型已加载");
        Log.Message($"    程序集: {injectorType.Assembly.GetName().Name}");
        Log.Message($"    位置: {injectorType.Assembly.Location}");
        
        // 检查是否有 ManualReInject 方法
        var reinjectMethod = injectorType.GetMethod("ManualReInject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (reinjectMethod != null) {
            Log.Message("  ? ManualReInject 方法存在");
        } else {
            Log.Error("  ? ManualReInject 方法不存在");
        }
    } else {
        Log.Error("  ? 未找到 ExpandMemoryKnowledgeInjector 类型");
        Log.Error("    可能 RimTalk-ExpandActions 未正确加载");
    }
} catch (System.Exception ex) {
    Log.Error($"  ? 检查失败: {ex.Message}");
}

// 2. 检查 ExpandMemory 是否加载
Log.Message("[2] 检查 RimTalk-ExpandMemory...");
try {
    bool isActive = Verse.ModsConfig.IsActive("RimTalk.ExpandMemory");
    Log.Message($"  ModsConfig.IsActive: {isActive}");
    
    if (!isActive) {
        Log.Warning("  ? ExpandMemory 未在 ModsConfig 中标记为活动");
        Log.Warning("    请在 Mod 列表中启用 RimTalk-ExpandMemory");
    }
    
    // 检查程序集是否加载
    var expandMemoryAssembly = System.AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.Contains("ExpandMemory"));
    if (expandMemoryAssembly != null) {
        Log.Message($"  ? ExpandMemory 程序集已加载");
        Log.Message($"    名称: {expandMemoryAssembly.GetName().Name}");
        Log.Message($"    版本: {expandMemoryAssembly.GetName().Version}");
        Log.Message($"    位置: {expandMemoryAssembly.Location}");
    } else {
        Log.Warning("  ? ExpandMemory 程序集未加载");
    }
} catch (System.Exception ex) {
    Log.Error($"  ? 检查失败: {ex.Message}");
}

// 3. 检查 CommonKnowledgeLibrary 类型
Log.Message("[3] 检查 CommonKnowledgeLibrary 类型...");
try {
    var commonKnowledgeType = System.Type.GetType("RimTalk.Memory.CommonKnowledgeLibrary, RimTalk-ExpandMemory");
    if (commonKnowledgeType != null) {
        Log.Message($"  ? 找到类型: {commonKnowledgeType.FullName}");
        Log.Message($"    程序集: {commonKnowledgeType.Assembly.GetName().Name}");
        
        // 列出所有公共静态方法
        Log.Message("  可用方法:");
        foreach (var method in commonKnowledgeType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
            Log.Message($"    - {method.Name}");
        }
    } else {
        Log.Warning("  ? 未找到 CommonKnowledgeLibrary 类型");
        
        // 尝试列出所有 RimTalk.Memory 命名空间的类型
        Log.Message("  查找所有 RimTalk.Memory 类型...");
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
            if (assembly.FullName.Contains("RimTalk")) {
                Log.Message($"    程序集: {assembly.GetName().Name}");
                try {
                    foreach (var type in assembly.GetTypes()) {
                        if (type.FullName != null && type.FullName.Contains("Memory") && type.FullName.Contains("Knowledge")) {
                            Log.Message($"      类型: {type.FullName}");
                        }
                    }
                } catch {
                    // 忽略无法访问的类型
                }
            }
        }
    }
} catch (System.Exception ex) {
    Log.Error($"  ? 检查失败: {ex.Message}");
}

// 4. 检查 ImportFromExternalMod 方法
Log.Message("[4] 检查 ImportFromExternalMod 方法...");
try {
    var commonKnowledgeType = System.Type.GetType("RimTalk.Memory.CommonKnowledgeLibrary, RimTalk-ExpandMemory");
    if (commonKnowledgeType != null) {
        var importMethod = commonKnowledgeType.GetMethod(
            "ImportFromExternalMod",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            null,
            new System.Type[] { typeof(string), typeof(string), typeof(bool) },
            null
        );
        
        if (importMethod != null) {
            Log.Message($"  ? 找到方法: {importMethod.Name}");
            Log.Message("    参数:");
            foreach (var param in importMethod.GetParameters()) {
                Log.Message($"      - {param.ParameterType.Name} {param.Name}");
            }
            Log.Message($"    返回类型: {importMethod.ReturnType.Name}");
        } else {
            Log.Warning("  ? 未找到 ImportFromExternalMod(string, string, bool) 方法");
            Log.Message("  尝试查找其他可能的导入方法...");
            
            foreach (var method in commonKnowledgeType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
                if (method.Name.Contains("Import")) {
                    Log.Message($"    - {method.Name}");
                }
            }
        }
    } else {
        Log.Warning("  ? 跳过（CommonKnowledgeLibrary 类型不存在）");
    }
} catch (System.Exception ex) {
    Log.Error($"  ? 检查失败: {ex.Message}");
}

// 5. 尝试手动注入
Log.Message("[5] 尝试手动注入...");
try {
    var injectorType = System.Type.GetType("RimTalkExpandActions.Memory.Utils.ExpandMemoryKnowledgeInjector, RimTalk-ExpandActions");
    if (injectorType != null) {
        var reinjectMethod = injectorType.GetMethod("ManualReInject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (reinjectMethod != null) {
            Log.Message("  调用 ManualReInject...");
            reinjectMethod.Invoke(null, null);
            Log.Message("  ? 调用完成，查看上方日志了解详情");
        } else {
            Log.Error("  ? ManualReInject 方法不存在");
        }
    } else {
        Log.Error("  ? ExpandMemoryKnowledgeInjector 类型不存在");
    }
} catch (System.Exception ex) {
    Log.Error($"  ? 手动注入失败: {ex.Message}");
    if (ex.InnerException != null) {
        Log.Error($"    内部异常: {ex.InnerException.Message}");
    }
}

Log.Message("━━━━━ 诊断完成 ━━━━━");
Log.Message("");
Log.Message("请检查上方日志，特别注意 ? 标记的错误");
Log.Message("如果看到 '? 成功导入 X 条规则'，说明注入成功");
