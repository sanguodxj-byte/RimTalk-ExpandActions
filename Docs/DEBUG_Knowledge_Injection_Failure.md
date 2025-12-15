# ?? 常识库注入失败诊断指南

## ?? 诊断检查清单

### 第 1 步：检查启动日志

查看 RimWorld 启动日志：
```
%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
```

搜索关键词：`[RimTalk-ExpandActions]`

### 可能的日志情况

#### 情况 A：未找到 ExpandMemory

**日志：**
```
[RimTalk-ExpandActions] 开始检查 RimTalk-ExpandMemory...
[RimTalk-ExpandActions] RimTalk-ExpandMemory 未启用，跳过常识库注入
```

**原因：** RimTalk-ExpandMemory 未安装或未启用

**解决方案：**
1. 确认已安装 RimTalk-ExpandMemory
2. 在 Mod 列表中启用它
3. 确保加载顺序正确：
   ```
   1. Harmony
   2. Core
   3. RimTalk
   4. RimTalk-ExpandMemory  ← 必须在这里
   5. RimTalk-ExpandActions
   ```

#### 情况 B：未找到 CommonKnowledgeLibrary 类型

**日志：**
```
[RimTalk-ExpandActions] 开始检查 RimTalk-ExpandMemory...
[RimTalk-ExpandActions] 未找到 CommonKnowledgeLibrary 类型
[RimTalk-ExpandActions] 可能是 RimTalk-ExpandMemory 版本不兼容
```

**原因：** ExpandMemory 版本太旧或类型名称变更

**解决方案：**

##### 方案 1: 检查 ExpandMemory 是否真的有这个类

在 F12 控制台运行：
```csharp
// 列出所有程序集
foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
{
    if (assembly.FullName.Contains("ExpandMemory") || assembly.FullName.Contains("RimTalk"))
    {
        Log.Message($"程序集: {assembly.FullName}");
        foreach (var type in assembly.GetTypes())
        {
            if (type.Name.Contains("Knowledge") || type.Name.Contains("Memory"))
            {
                Log.Message($"  类型: {type.FullName}");
            }
        }
    }
}
```

这会显示所有可能的类型名称。

##### 方案 2: 更新 ExpandMemory

下载最新版本的 RimTalk-ExpandMemory。

#### 情况 C：未找到 ImportFromExternalMod 方法

**日志：**
```
[RimTalk-ExpandActions] 开始检查 RimTalk-ExpandMemory...
[RimTalk-ExpandActions] 正在导入行为规则到常识库...
[RimTalk-ExpandActions] 未找到 ImportFromExternalMod 方法
[RimTalk-ExpandActions] 请更新 RimTalk-ExpandMemory 到最新版本
```

**原因：** ExpandMemory 的 API 名称或签名不同

**解决方案：**

检查 ExpandMemory 的实际 API：

```csharp
// F12 控制台运行
var type = System.Type.GetType("RimTalk.Memory.CommonKnowledgeLibrary, RimTalk-ExpandMemory");
if (type != null)
{
    Log.Message($"找到类型: {type.FullName}");
    
    // 列出所有公共静态方法
    foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
    {
        var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Log.Message($"  方法: {method.Name}({parameters})");
    }
}
else
{
    Log.Error("未找到 CommonKnowledgeLibrary 类型");
}
```

#### 情况 D：导入失败（异常）

**日志：**
```
[RimTalk-ExpandActions] 开始检查 RimTalk-ExpandMemory...
[RimTalk-ExpandActions] 正在导入行为规则到常识库...
[RimTalk-ExpandActions] InjectKnowledgeToExpandMemory 失败: ...
```

**原因：** API 调用时抛出异常

**解决方案：**
1. 查看完整的异常堆栈
2. 检查参数格式是否正确
3. 验证 API 签名

#### 情况 E：导入成功但数量为 0

**日志：**
```
[RimTalk-ExpandActions] ? 成功导入 0 条行为规则到常识库
```
或
```
[RimTalk-ExpandActions] 导入完成，但没有有效的规则被添加
```

**原因：** 规则格式不正确

**解决方案：**
检查规则格式是否符合 ExpandMemory 的要求。

#### 情况 F：完全没有日志

**现象：** 搜索 `[RimTalk-ExpandActions]` 找不到任何内容

**原因：** Mod 未被加载或静态构造函数未执行

**解决方案：**
1. 确认 Mod 在 Mod 列表中已启用
2. 检查 DLL 是否正确部署：
   ```powershell
   Test-Path "D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions\1.6\Assemblies\RimTalk-ExpandActions.dll"
   ```
3. 查看游戏日志是否有加载错误

## ??? 调试步骤

### 步骤 1: 手动测试注入

启动游戏后，在 F12 控制台运行：

```csharp
// 手动触发注入
RimTalkExpandActions.Memory.Utils.ExpandMemoryKnowledgeInjector.ManualReInject();
```

查看输出，确认失败在哪一步。

### 步骤 2: 检查 ModsConfig

```csharp
// 检查 Mod 是否被识别为活动
bool isActive = Verse.ModsConfig.IsActive("RimTalk.ExpandMemory");
Log.Message($"ExpandMemory 是否活动: {isActive}");

// 列出所有活动的 Mod
foreach (var mod in Verse.ModsConfig.ActiveModsInLoadOrder)
{
    if (mod.Name.Contains("RimTalk") || mod.Name.Contains("ExpandMemory"))
    {
        Log.Message($"活动 Mod: {mod.Name} ({mod.PackageId})");
    }
}
```

### 步骤 3: 检查程序集加载

```csharp
// 检查 ExpandMemory 的程序集是否已加载
var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
var expandMemoryAssembly = assemblies.FirstOrDefault(a => a.FullName.Contains("ExpandMemory"));

if (expandMemoryAssembly != null)
{
    Log.Message($"? ExpandMemory 程序集已加载: {expandMemoryAssembly.FullName}");
    Log.Message($"  位置: {expandMemoryAssembly.Location}");
    
    // 列出所有导出的类型
    foreach (var type in expandMemoryAssembly.GetExportedTypes())
    {
        Log.Message($"  导出类型: {type.FullName}");
    }
}
else
{
    Log.Error("? ExpandMemory 程序集未加载");
}
```

### 步骤 4: 直接调用 API

如果找到了 API，尝试直接调用：

```csharp
try
{
    var type = System.Type.GetType("RimTalk.Memory.CommonKnowledgeLibrary, RimTalk-ExpandMemory");
    if (type != null)
    {
        var method = type.GetMethod("ImportFromExternalMod", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        
        if (method != null)
        {
            var result = method.Invoke(null, new object[] 
            {
                "[测试|1.0]这是一条测试规则",
                "Test",
                true
            });
            
            Log.Message($"API 调用结果: {result}");
        }
        else
        {
            Log.Error("未找到 ImportFromExternalMod 方法");
        }
    }
    else
    {
        Log.Error("未找到 CommonKnowledgeLibrary 类型");
    }
}
catch (Exception ex)
{
    Log.Error($"API 调用失败: {ex.Message}\n{ex.StackTrace}");
}
```

## ?? 可能的修复方案

### 修复 1: 更新类型名称

如果 ExpandMemory 的类型名称不同，更新 `ExpandMemoryKnowledgeInjector.cs`：

```csharp
// 尝试多个可能的类型名称
private static Type FindCommonKnowledgeType()
{
    string[] possibleNames = new string[]
    {
        "RimTalk.Memory.CommonKnowledgeLibrary",
        "RimTalk.Memory.KnowledgeLibrary",
        "RimTalk.Memory.CommonKnowledge",
        "RimTalkExpandMemory.CommonKnowledgeLibrary"
    };
    
    foreach (var name in possibleNames)
    {
        Type type = FindType(name);
        if (type != null)
        {
            Log.Message($"[RimTalk-ExpandActions] 找到类型: {name}");
            return type;
        }
    }
    
    return null;
}
```

### 修复 2: 添加详细日志

在 `ExpandMemoryKnowledgeInjector.cs` 中添加更多日志：

```csharp
private static void InjectKnowledgeToExpandMemory()
{
    try
    {
        Log.Message("[RimTalk-ExpandActions] === 开始注入流程 ===");
        
        // 1. 检查 Mod
        Log.Message("[RimTalk-ExpandActions] 步骤 1: 检查 ModsConfig...");
        bool isActive = ModsConfig.IsActive("RimTalk.ExpandMemory");
        Log.Message($"[RimTalk-ExpandActions]   ModsConfig.IsActive: {isActive}");
        
        if (!isActive)
        {
            Log.Warning("[RimTalk-ExpandActions] ExpandMemory 未启用");
            return;
        }
        
        // 2. 查找类型
        Log.Message("[RimTalk-ExpandActions] 步骤 2: 查找 CommonKnowledgeLibrary...");
        Type commonKnowledgeType = FindType("RimTalk.Memory.CommonKnowledgeLibrary");
        
        if (commonKnowledgeType == null)
        {
            Log.Warning("[RimTalk-ExpandActions]   未找到类型");
            // 列出所有 RimTalk 相关的类型
            Log.Message("[RimTalk-ExpandActions] 查找所有 RimTalk 类型...");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("RimTalk"))
                {
                    Log.Message($"[RimTalk-ExpandActions]   程序集: {assembly.FullName}");
                }
            }
            return;
        }
        
        Log.Message($"[RimTalk-ExpandActions]   ? 找到类型: {commonKnowledgeType.FullName}");
        
        // 3. 查找方法
        Log.Message("[RimTalk-ExpandActions] 步骤 3: 查找 ImportFromExternalMod...");
        MethodInfo importMethod = commonKnowledgeType.GetMethod(
            "ImportFromExternalMod",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new Type[] { typeof(string), typeof(string), typeof(bool) },
            null
        );
        
        if (importMethod == null)
        {
            Log.Warning("[RimTalk-ExpandActions]   未找到方法");
            // 列出所有方法
            Log.Message("[RimTalk-ExpandActions] 可用方法:");
            foreach (var method in commonKnowledgeType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                var pars = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Log.Message($"[RimTalk-ExpandActions]   - {method.Name}({pars})");
            }
            return;
        }
        
        Log.Message($"[RimTalk-ExpandActions]   ? 找到方法: {importMethod.Name}");
        
        // 4. 调用方法
        Log.Message("[RimTalk-ExpandActions] 步骤 4: 调用 ImportFromExternalMod...");
        Log.Message($"[RimTalk-ExpandActions]   参数 1: {KNOWLEDGE_CONTENT.Length} 字符");
        Log.Message($"[RimTalk-ExpandActions]   参数 2: RimTalk-ExpandActions");
        Log.Message($"[RimTalk-ExpandActions]   参数 3: true");
        
        object result = importMethod.Invoke(null, new object[]
        {
            KNOWLEDGE_CONTENT,
            "RimTalk-ExpandActions",
            true
        });
        
        Log.Message($"[RimTalk-ExpandActions]   ? 调用成功");
        
        // 5. 检查结果
        Log.Message("[RimTalk-ExpandActions] 步骤 5: 检查结果...");
        if (result is int count)
        {
            Log.Message($"[RimTalk-ExpandActions]   返回值: {count}");
            
            if (count > 0)
            {
                Log.Message($"[RimTalk-ExpandActions] ??? 成功导入 {count} 条规则 ???");
            }
            else
            {
                Log.Warning("[RimTalk-ExpandActions] 导入数量为 0");
            }
        }
        else
        {
            Log.Message($"[RimTalk-ExpandActions]   返回值类型: {result?.GetType().Name ?? "null"}");
        }
        
        Log.Message("[RimTalk-ExpandActions] === 注入流程完成 ===");
    }
    catch (Exception ex)
    {
        Log.Error($"[RimTalk-ExpandActions] === 注入失败 ===");
        Log.Error($"[RimTalk-ExpandActions] 异常: {ex.GetType().Name}");
        Log.Error($"[RimTalk-ExpandActions] 消息: {ex.Message}");
        Log.Error($"[RimTalk-ExpandActions] 堆栈:\n{ex.StackTrace}");
    }
}
```

## ?? 报告问题

如果以上所有方法都不行，请提供以下信息：

1. **完整的启动日志** - 从 `Loading defs...` 到游戏主菜单
2. **Mod 列表** - 特别是 RimTalk 相关的 Mod
3. **ExpandMemory 版本** - 如果知道的话
4. **F12 控制台的调试输出** - 运行上面的诊断脚本

---

**创建时间:** 2025/12/15  
**用途:** 诊断常识库注入失败问题  
**状态:** ? 等待用户提供日志
