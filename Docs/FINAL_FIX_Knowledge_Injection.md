# ?? 常识库注入问题完全解决方案

## ?? 问题总结

### 原始问题
```
[RimTalk-ExpandActions] ? 成功注入规则: expand-action-recruit
[RimTalk-ExpandActions] 找到类型 RimTalk.Memory.MemoryManager 在程序集 RimTalkMemoryPatch
```

看起来成功了，但实际上**注入到了错误的程序集 (`RimTalkMemoryPatch`)**，这些规则根本不会被使用。

### 根本原因

1. **PackageId 错误** - 使用了错误的 PackageId 检测 ExpandMemory
2. **旧注入器** - `CrossModRecruitRuleInjector` 找到了错误的程序集
3. **缺少正确的 API 调用** - 没有使用 ExpandMemory 的公共 API

## ? 完整修复

### 1. 修复 PackageId 检测

**Before:**
```csharp
if (ModsConfig.IsActive("RimTalk.ExpandMemory"))  // ← 错误的 ID
```

**After:**
```csharp
string[] possiblePackageIds = new string[]
{
    "sanguo.rimtalk.expandmemory",      // ? 正确的 PackageId
    "RimTalk.ExpandMemory",             // 备选
    // ...
};
```

### 2. 排除错误的程序集

**Before:**
```csharp
foreach (var assembly in assemblies)
{
    if (assembly.FullName.Contains("ExpandMemory"))  // ← 会找到 RimTalkMemoryPatch
    {
        // ...
    }
}
```

**After:**
```csharp
foreach (var assembly in assemblies)
{
    if ((assembly.FullName.Contains("ExpandMemory") ||  assembly.FullName.Contains("RimTalk-ExpandMemory"))
        && !assemblyName.Equals("RimTalkMemoryPatch", StringComparison.OrdinalIgnoreCase))  // ← 排除错误的程序集
    {
        // ...
    }
}
```

### 3. 禁用旧注入器

**Source/Memory/Utils/CrossModRecruitRuleInjector.cs:**
```csharp
[Obsolete("已弃用，请使用 ExpandMemoryKnowledgeInjector")]
public static class CrossModRecruitRuleInjector
{
    [Obsolete("已弃用，不再使用")]
    public static bool TryInjectRule(...)
    {
        Log.Warning("[RimTalk-ExpandActions] CrossModRecruitRuleInjector 已弃用，请使用 ExpandMemoryKnowledgeInjector");
        return false;
    }
}
```

### 4. 更新主 Mod 类

**Source/RimTalkExpandActionsMod.cs:**
```csharp
// Before (使用旧注入器)
bool success = Memory.Utils.CrossModRecruitRuleInjector.TryInjectRule(...);

// After (使用新注入器)
Memory.Utils.ExpandMemoryKnowledgeInjector.ManualReInject();
```

### 5. 更新 About.xml

```xml
<modDependencies>
    <li>
        <packageId>sanguo.rimtalk.expandmemory</packageId>  ← 正确的 PackageId
        <displayName>RimTalk-ExpandMemory</displayName>
    </li>
</modDependencies>
```

## ?? 新的注入流程

### 自动注入（启动时）

```
游戏启动
    ↓
ExpandMemoryKnowledgeInjector 静态构造
    ↓
[1/5] 检查 RimTalk-ExpandMemory
    → ModsConfig.IsActive("sanguo.rimtalk.expandmemory")
    → 查找 CommonKnowledgeLibrary 类型
    → 检查程序集（排除 RimTalkMemoryPatch）
    ↓
[2/5] 查找 CommonKnowledgeLibrary 类型
    ↓
[3/5] 查找 ImportFromExternalMod 方法
    ↓
[4/5] 调用 API 注入规则
    ↓
[5/5] 检查返回值
    ↓
? 成功导入 7 条规则到常识库
```

### 手动注入（设置界面）

```
用户点击"手动注入规则到常识库"按钮
    ↓
调用 ExpandMemoryKnowledgeInjector.ManualReInject()
    ↓
执行完整的注入流程
    ↓
显示结果消息
```

## ?? 对比

| 特性 | 旧方案 (CrossModRecruitRuleInjector) | 新方案 (ExpandMemoryKnowledgeInjector) |
|------|-------------------------------------|---------------------------------------|
| **程序集检测** | ? 找到错误的 `RimTalkMemoryPatch` | ? 正确的 `sanguo.rimtalk.expandmemory` |
| **PackageId** | ? `RimTalk.ExpandMemory` | ? `sanguo.rimtalk.expandmemory` |
| **注入方式** | ? 直接反射操作 MemoryManager | ? 使用公共 API `ImportFromExternalMod` |
| **错误处理** | ? 基本 | ? 完善的空值检查和异常处理 |
| **日志详细度** | ? 少 | ? 5 步详细日志 |
| **规则格式** | ? 手动构建 Entry 对象 | ? 标准文本格式 `[标签|重要性]内容` |

## ?? 验证步骤

### 1. 重启游戏并检查日志

**期望看到：**
```
[RimTalk-ExpandActions] ━━━━━ 开始常识库注入流程 ━━━━━
[RimTalk-ExpandActions] [1/5] 检查 RimTalk-ExpandMemory 是否启用...
[RimTalk-ExpandActions]   ? 通过类型检测找到 ExpandMemory
[RimTalk-ExpandActions]   ? 找到程序集: RimTalk-ExpandMemory  ← 不是 RimTalkMemoryPatch!
[RimTalk-ExpandActions]   ? ModsConfig.IsActive('sanguo.rimtalk.expandmemory') = true
[RimTalk-ExpandActions]   检测结果:
[RimTalk-ExpandActions]     - 类型检测: true
[RimTalk-ExpandActions]     - 程序集检测: true
[RimTalk-ExpandActions]     - ModsConfig 检测: true
[RimTalk-ExpandActions]     - 最终结果: true
[RimTalk-ExpandActions]   ? ExpandMemory 可用
[RimTalk-ExpandActions] [2/5] 查找 CommonKnowledgeLibrary 类型...
[RimTalk-ExpandActions]   ? 找到类型: RimTalk.Memory.CommonKnowledgeLibrary
[RimTalk-ExpandActions]   程序集: RimTalk-ExpandMemory  ← 正确！
[RimTalk-ExpandActions] [3/5] 查找 ImportFromExternalMod 方法...
[RimTalk-ExpandActions]   ? 找到方法: ImportFromExternalMod
[RimTalk-ExpandActions] [4/5] 调用 ImportFromExternalMod...
[RimTalk-ExpandActions]   ? API 调用成功
[RimTalk-ExpandActions] [5/5] 检查导入结果...
[RimTalk-ExpandActions] ━━━━━━━━━━━━━━━━━━━━━━━━━━
[RimTalk-ExpandActions] ??? 成功导入 7 条行为规则到常识库 ???
[RimTalk-ExpandActions] ━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**不应该看到：**
```
[RimTalk-ExpandActions] 找到类型 RimTalk.Memory.MemoryManager 在程序集 RimTalkMemoryPatch  ← 错误！
[RimTalk-ExpandActions] 自动注入模式已启用，尝试注入所有行为规则...  ← 旧注入器！
```

### 2. 测试手动注入

1. 打开 Mod 设置
2. 点击"手动注入规则到常识库"按钮
3. 检查日志是否显示完整的注入流程

### 3. 验证规则是否生效

在游戏中与 NPC 对话，说："加入我们吧"

**期望：**
- AI 回复包含 JSON 指令
- 招募动作被执行

## ?? 修改的文件

| 文件 | 修改内容 | 状态 |
|------|---------|------|
| `Source/Memory/Utils/ExpandMemoryKnowledgeInjector.cs` | 修复 PackageId + 排除错误程序集 | ? 完成 |
| `Source/Memory/Utils/CrossModRecruitRuleInjector.cs` | 标记为 Obsolete | ? 完成 |
| `Source/RimTalkExpandActionsMod.cs` | 使用新注入器 | ? 完成 |
| `About/About.xml` | 正确的 PackageId | ? 已正确 |

## ?? 最终状态

| 项目 | 状态 |
|------|------|
| **编译** | ? 成功 |
| **部署** | ? 完成 |
| **DLL 大小** | 90,112 bytes |
| **PackageId** | ? `sanguo.rimtalk.expandmemory` |
| **注入方式** | ? 使用公共 API |
| **错误处理** | ? 完善 |
| **旧注入器** | ? 已禁用 |
| **准备测试** | ? 是 |

---

**修复时间:** 2025/12/15  
**问题类型:** 错误的程序集检测 + 错误的 PackageId  
**解决方案:** 使用正确的 PackageId + 排除错误程序集 + 禁用旧注入器  
**状态:** ? 完全修复，准备测试

## ?? 下一步

1. **重启 RimWorld**
2. **检查启动日志** - 应该看到正确的注入流程
3. **测试对话** - 验证规则是否生效
4. **如果仍有问题** - 提供完整的启动日志（从 "Loading defs..." 到主菜单）
