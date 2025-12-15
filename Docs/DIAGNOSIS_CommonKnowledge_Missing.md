# ?? 常识库注入失败 - 完整诊断报告

## ?? 问题确认

### 调试补丁工作正常 ?
```
[RimTalk-ExpandActions] ━━━━━ Prompt 注入检查 ━━━━━
目标: 黛狐
Prompt 长度: 1009 字符
```

补丁成功 Hook 了 `RimTalk.Service.PromptService.BuildContext` 方法。

### 问题确认 ?
```
检查关键词:
  - 常识库标记: ? 不存在
  - 招募规则: ? 不存在
  - 社交用餐规则: ? 不存在
  - 投降规则: ? 不存在
  - 恋爱规则: ? 不存在

总结: 检测到 0/7 条规则
```

**Prompt 中完全没有常识库内容！**

## ?? 根本原因分析

### 可能原因 1: RimTalk 不包含 RimTalk-ExpandMemory

**最可能的原因！**

你可能使用的是 **原版 RimTalk**，而不是 **RimTalk-ExpandMemory**。

#### 验证方法

在 RimWorld 控制台（F12）运行：

```csharp
// 检查是否有 MemoryManager
var memType = System.Type.GetType("RimTalk.Memory.MemoryManager, RimTalk-ExpandMemory");
if (memType == null)
{
    Log.Error("? 未找到 RimTalk-ExpandMemory！");
    Log.Error("你使用的是原版 RimTalk，它不支持常识库功能！");
}
else
{
    Log.Message("? 找到 RimTalk-ExpandMemory");
}
```

### 可能原因 2: 常识库功能未启用

即使安装了 RimTalk-ExpandMemory，常识库功能可能未启用。

#### 验证方法

检查 RimTalk-ExpandMemory 的设置：
1. 打开 Mod 设置 → RimTalk-ExpandMemory
2. 查找 "启用常识库" 或类似选项
3. 确认已勾选

### 可能原因 3: 注入失败

规则注入到常识库时失败了。

#### 验证方法

查看启动日志，搜索：
```
[RimTalk-ExpandActions] 找到类型 RimTalk.Memory.MemoryManager 在程序集 RimTalk-ExpandMemory
```

如果看到：
```
[RimTalk-ExpandActions] 调试补丁: 未找到 RimTalk.Memory.MemoryManager
```

说明 RimTalk-ExpandMemory 根本没安装。

## ? 解决方案

### 方案 1: 安装 RimTalk-ExpandMemory（推荐）

**如果你想使用常识库功能（推荐）**

1. **下载 RimTalk-ExpandMemory**
   - 从 Steam 创意工坊订阅
   - 或从 GitHub 下载：[RimTalk-ExpandMemory Repository]

2. **确认 Mod 加载顺序**
   ```
   1. Harmony
   2. Core
   3. RimTalk (原版)
   4. RimTalk-ExpandMemory  ← 必须在这里
   5. RimTalk-ExpandActions ← 你的 Mod
   ```

3. **重启游戏**

4. **验证安装**
   ```csharp
   // F12 控制台运行
   var memType = System.Type.GetType("RimTalk.Memory.MemoryManager, RimTalk-ExpandMemory");
   Log.Message(memType != null ? "? ExpandMemory 已安装" : "? ExpandMemory 未安装");
   ```

### 方案 2: 直接在 RimTalk 中集成（替代方案）

**如果你可以修改 RimTalk 源码**

由于你说可以修改 RimTalk-main 代码，可以直接在 `PromptService.BuildContext` 中添加常识库检索逻辑：

#### 修改位置

文件：`RimTalk-main/Source/Service/PromptService.cs`

在 `BuildContext` 方法的末尾添加：

```csharp
public static string BuildContext(List<Pawn> pawns)
{
    var context = new StringBuilder();
    context.AppendLine(Constant.Instruction).AppendLine();

    for (int i = 0; i < pawns.Count; i++)
    {
        // ... 现有代码 ...
    }

    // ===== 新增：常识库注入 =====
    try
    {
        // 尝试获取 RimTalk-ExpandActions 注入的常识库
        var expandActionsType = System.Type.GetType("RimTalkExpandActions.Memory.Utils.BehaviorRuleContents, RimTalkExpandActions");
        if (expandActionsType != null)
        {
            var getAllRulesMethod = expandActionsType.GetMethod("GetAllRules");
            if (getAllRulesMethod != null)
            {
                var rules = getAllRulesMethod.Invoke(null, null) as System.Collections.IDictionary;
                if (rules != null && rules.Count > 0)
                {
                    context.AppendLine().AppendLine("World Knowledge:");
                    
                    foreach (var kvp in rules)
                    {
                        var ruleDef = kvp.GetType().GetProperty("Value")?.GetValue(kvp);
                        if (ruleDef != null)
                        {
                            var contentProp = ruleDef.GetType().GetProperty("Content");
                            var tagProp = ruleDef.GetType().GetProperty("Tag");
                            var importanceProp = ruleDef.GetType().GetProperty("Importance");
                            
                            string content = contentProp?.GetValue(ruleDef) as string;
                            string tag = tagProp?.GetValue(ruleDef) as string;
                            float importance = (float)(importanceProp?.GetValue(ruleDef) ?? 1.0f);
                            
                            if (!string.IsNullOrEmpty(content))
                            {
                                context.AppendLine($"- [{tag}|{importance:F1}] {content}");
                            }
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Log.Warning($"[RimTalk] 加载 ExpandActions 常识库失败: {ex.Message}");
    }
    // ===== 新增结束 =====

    return context.ToString();
}
```

### 方案 3: 使用 Harmony Prefix 注入（高级）

**如果不想修改 RimTalk 源码**

创建一个 Harmony Prefix 补丁来在 `BuildContext` 返回前注入常识库内容。

#### 创建新文件

`Source/Patches/BuildContextInjectorPatch.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Verse;

namespace RimTalkExpandActions.Patches
{
    /// <summary>
    /// 通过 Postfix 在 BuildContext 返回值中注入常识库内容
    /// </summary>
    [HarmonyPatch]
    public static class BuildContextInjectorPatch
    {
        private static bool isPatched = false;

        static bool Prepare()
        {
            try
            {
                Type promptServiceType = AccessTools.TypeByName("RimTalk.Service.PromptService");
                if (promptServiceType == null)
                {
                    Log.Warning("[RimTalk-ExpandActions] 注入补丁: 未找到 PromptService");
                    return false;
                }

                MethodInfo buildContextMethod = AccessTools.Method(promptServiceType, "BuildContext", new Type[] { typeof(List<Pawn>) });
                if (buildContextMethod == null)
                {
                    Log.Warning("[RimTalk-ExpandActions] 注入补丁: 未找到 BuildContext 方法");
                    return false;
                }

                isPatched = true;
                Log.Message("[RimTalk-ExpandActions] 注入补丁: 将直接注入常识库到 Prompt");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] BuildContextInjectorPatch.Prepare 失败: {ex.Message}");
                return false;
            }
        }

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

                // 获取所有规则
                var allRules = Memory.Utils.BehaviorRuleContents.GetAllRules();
                if (allRules == null || allRules.Count == 0)
                {
                    return;
                }

                // 构建常识库部分
                var knowledgeBuilder = new StringBuilder();
                knowledgeBuilder.AppendLine().AppendLine("World Knowledge:");
                
                foreach (var kvp in allRules)
                {
                    var ruleDef = kvp.Value;
                    knowledgeBuilder.AppendLine($"- [{ruleDef.Tag}|{ruleDef.Importance:F1}] {ruleDef.Content}");
                }

                // 附加到原始 Prompt
                __result += knowledgeBuilder.ToString();

                if (RimTalkExpandActionsMod.Settings?.enableDetailedLogging == true)
                {
                    Log.Message($"[RimTalk-ExpandActions] 已注入 {allRules.Count} 条规则到 Prompt");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-ExpandActions] BuildContextInjectorPatch.Postfix 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static bool IsPatched => isPatched;
    }
}
```

这个补丁会**直接在 BuildContext 返回值中添加常识库内容**，不依赖 RimTalk-ExpandMemory。

## ?? 推荐方案

### 优先级排序

1. **方案 3: 使用 BuildContextInjectorPatch** ?????
   - ? 不需要额外的 Mod
   - ? 完全控制注入逻辑
   - ? 立即可用
   - ? 已经有所有必要的代码

2. **方案 1: 安装 RimTalk-ExpandMemory** ????
   - ? 官方支持
   - ? 功能完整
   - ? 需要额外下载
   - ? 增加依赖

3. **方案 2: 修改 RimTalk 源码** ???
   - ? 集成度高
   - ? 需要维护分支
   - ? 更新麻烦

## ?? 立即实施方案 3

我已经为你准备好了 `BuildContextInjectorPatch.cs` 的完整代码（见上方）。

### 实施步骤

1. **我来创建这个文件**

2. **重新编译**
   ```powershell
   msbuild /t:Rebuild /p:Configuration=Debug
   ```

3. **部署**
   ```powershell
   .\Deploy.ps1
   ```

4. **重启游戏并验证**

   再次与 NPC 对话，应该看到：
   ```
   检测到 7/7 条规则
   ? 所有规则都已正确注入到 Prompt 中！
   ```

---

**你希望我立即创建 `BuildContextInjectorPatch.cs` 吗？**

这是最快的解决方案，不需要任何外部依赖！
