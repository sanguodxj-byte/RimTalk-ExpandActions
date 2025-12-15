# ?? NullReferenceException 修复说明

## ?? 问题

```
[RimTalk-ExpandActions] ??? InjectKnowledgeToExpandMemory 失败 ???
[RimTalk-ExpandActions] 异常类型: NullReferenceException
[RimTalk-ExpandActions] 错误消息: Object reference not set to an instance of an object
```

## ?? 根本原因

在检测 RimTalk-ExpandMemory 时，没有对以下情况进行空值检查：

1. `AppDomain.CurrentDomain.GetAssemblies()` 可能返回包含 null 的数组
2. `assembly.FullName` 可能为 null
3. `method.GetParameters()` 可能为 null
4. `ex.StackTrace` 可能为 null

## ? 修复内容

### 1. 添加了全面的空值检查

**Before (? 会崩溃):**
```csharp
foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
{
    if (assembly.FullName.Contains("ExpandMemory"))  // ← NullReferenceException!
    {
        // ...
    }
}
```

**After (? 安全):**
```csharp
var assemblies = AppDomain.CurrentDomain.GetAssemblies();
if (assemblies != null)  // ← 检查数组
{
    foreach (var assembly in assemblies)
    {
        if (assembly != null && assembly.FullName != null)  // ← 检查对象和属性
        {
            if (assembly.FullName.Contains("ExpandMemory"))
            {
                // ...
            }
        }
    }
}
```

### 2. 添加了 try-catch 保护

每个检测方法都单独用 try-catch 包裹：

```csharp
// 方法 1: 类型检测
try
{
    Type testType = FindType("RimTalk.Memory.CommonKnowledgeLibrary");
    if (testType != null)
    {
        foundByType = true;
    }
}
catch (Exception ex)
{
    Log.Warning($"类型检测失败: {ex.Message}");
}

// 方法 2: 程序集检测
try
{
    // ... 检测代码 ...
}
catch (Exception ex)
{
    Log.Warning($"程序集检测失败: {ex.Message}");
}

// 方法 3: ModsConfig 检测
try
{
    // ... 检测代码 ...
}
catch (Exception ex)
{
    Log.Warning($"ModsConfig 检测失败: {ex.Message}");
}
```

**好处：** 即使某一种检测方法失败，其他方法仍可继续尝试。

### 3. 安全的字符串操作

**Before:**
```csharp
var methodParams = string.Join(", ", importMethod.GetParameters().Select(...));
Log.Message($"签名: {importMethod.Name}({methodParams})");
```

**After:**
```csharp
try
{
    var methodParams = string.Join(", ", importMethod.GetParameters().Select(...));
    Log.Message($"签名: {importMethod.Name}({methodParams})");
}
catch
{
    // 忽略签名显示失败，不影响主要功能
}
```

### 4. 安全的堆栈跟踪输出

**Before:**
```csharp
Log.Error($"堆栈跟踪:\n{ex.StackTrace}");  // ← 可能 NullReferenceException
```

**After:**
```csharp
if (!string.IsNullOrEmpty(ex.StackTrace))
{
    Log.Error($"堆栈跟踪:\n{ex.StackTrace}");
}
```

## ?? 修复后的行为

### 场景 1: ExpandMemory 未安装

**Before (崩溃):**
```
[RimTalk-ExpandActions] ??? InjectKnowledgeToExpandMemory 失败 ???
[RimTalk-ExpandActions] 异常类型: NullReferenceException
```

**After (安全跳过):**
```
[RimTalk-ExpandActions] ━━━━━ 开始常识库注入流程 ━━━━━
[RimTalk-ExpandActions] [1/5] 检查 RimTalk-ExpandMemory 是否启用...
[RimTalk-ExpandActions]   检测结果:
[RimTalk-ExpandActions]     - 类型检测: false
[RimTalk-ExpandActions]     - 程序集检测: false
[RimTalk-ExpandActions]     - ModsConfig 检测: false
[RimTalk-ExpandActions]     - 最终结果: false
[RimTalk-ExpandActions] ? RimTalk-ExpandMemory 未启用或未安装
[RimTalk-ExpandActions] ━━━━━ 注入流程结束（跳过） ━━━━━
```

### 场景 2: ExpandMemory 已安装

```
[RimTalk-ExpandActions] ━━━━━ 开始常识库注入流程 ━━━━━
[RimTalk-ExpandActions] [1/5] 检查 RimTalk-ExpandMemory 是否启用...
[RimTalk-ExpandActions]   ? 通过类型检测找到 ExpandMemory
[RimTalk-ExpandActions]   ? 找到程序集: RimTalk-ExpandMemory
[RimTalk-ExpandActions]   检测结果:
[RimTalk-ExpandActions]     - 类型检测: true
[RimTalk-ExpandActions]     - 程序集检测: true
[RimTalk-ExpandActions]     - ModsConfig 检测: false
[RimTalk-ExpandActions]     - 最终结果: true
[RimTalk-ExpandActions]   ? ExpandMemory 可用
[RimTalk-ExpandActions] [2/5] 查找 CommonKnowledgeLibrary 类型...
[RimTalk-ExpandActions]   ? 找到类型: RimTalk.Memory.CommonKnowledgeLibrary
...
[RimTalk-ExpandActions] ??? 成功导入 7 条行为规则到常识库 ???
```

### 场景 3: 某个检测方法失败

```
[RimTalk-ExpandActions] [1/5] 检查 RimTalk-ExpandMemory 是否启用...
[RimTalk-ExpandActions]   程序集检测失败: Some error message
[RimTalk-ExpandActions]   ? 通过类型检测找到 ExpandMemory  ← 其他方法仍可工作
[RimTalk-ExpandActions]   检测结果:
[RimTalk-ExpandActions]     - 类型检测: true
[RimTalk-ExpandActions]     - 程序集检测: false
[RimTalk-ExpandActions]     - ModsConfig 检测: false
[RimTalk-ExpandActions]     - 最终结果: true  ← 仍然成功！
```

## ?? 测试验证

### 1. 重启游戏

关闭 RimWorld，重新启动。

### 2. 查看启动日志

应该**不再看到** NullReferenceException。

### 3. 预期结果

**如果 ExpandMemory 未安装：**
- ? 不会崩溃
- ? 显示"未启用或未安装"
- ? 其他功能正常工作

**如果 ExpandMemory 已安装：**
- ? 成功检测
- ? 成功导入规则
- ? 显示成功消息

## ?? 修复清单

- [x] ? 添加空值检查（assembly, FullName, StackTrace 等）
- [x] ? 每个检测方法独立 try-catch
- [x] ? 安全的 LINQ 操作
- [x] ? 安全的字符串操作
- [x] ? 详细的错误日志
- [x] ? 重新编译
- [x] ? 重新部署

## ?? 代码变更统计

| 项目 | Before | After | 变化 |
|------|--------|-------|------|
| **空值检查** | 0 处 | 15+ 处 | +15 |
| **try-catch 块** | 1 个 | 6 个 | +5 |
| **安全性** | ? 低 | ? 高 | ?? |
| **鲁棒性** | ? 脆弱 | ? 强健 | ?? |

## ?? 部署状态

| 项目 | 状态 |
|------|------|
| **编译** | ? 成功 |
| **DLL 大小** | ~98 KB |
| **部署** | ? 完成 |
| **测试** | ? 待用户重启游戏 |

---

**修复时间:** 2025/12/15  
**修复类型:** NullReferenceException 防护  
**影响范围:** ExpandMemoryKnowledgeInjector.cs  
**状态:** ? 已完成，等待测试
