# ?? 故障排查：规则在提示词中但行为未触发

## ?? 问题描述

- ? 规则成功注入到常识库
- ? 规则出现在发送给 AI 的提示词中
- ? AI 回复后，行为没有执行

## ?? 可能的原因

### 1. AI 没有输出 JSON 指令

**最常见的原因！** AI 理解了规则，但在生成回复时没有输出 JSON。

#### 检查方法

查看 Player.log 中 AI 的原始回复：

```
搜索关键词：[RimTalk] Response
或：AI 回复的实际内容
```

**期望的格式：**
```
好啊，我们一起放松吧！{"action": "social_relax", "targets": "Val,Cait"}
```

**如果 AI 只回复：**
```
好啊，我们一起放松吧！
```

→ **AI 没有输出 JSON**，这不是我们的 Mod 问题。

---

### 2. JSON 格式错误

AI 输出了 JSON，但格式不对。

#### 常见错误

| 错误格式 | 正确格式 |
|---------|---------|
| `{action: "recruit"}` | `{"action": "recruit"}` |
| `{'action': 'recruit'}` | `{"action": "recruit"}` |
| `{ "action" :"recruit" }` | `{"action": "recruit"}` |

#### 检查方法

搜索日志：
```
[RimTalk-ExpandActions] ProcessActionResponse
```

如果看到：
```
[RimTalk-ExpandActions] 检测到动作 'recruit'，将在主线程执行
```

→ JSON 解析成功

如果没有这条日志 → JSON 没有被识别

---

### 3. 线程安全问题导致静默失败

延迟执行到主线程时发生错误。

#### 检查方法

搜索日志：
```
[RimTalk-ExpandActions] 主线程执行失败
或
[RimTalk-ExpandActions] FindPawnByName: 线程安全错误
```

如果看到这些错误 → 行为在执行时失败了

---

### 4. 行为被禁用或成功率检定失败

Mod 设置中的开关或成功率导致行为被跳过。

#### 检查方法

1. **打开 Mod 设置**
   - 选项 → Mod 设置 → RimTalk-ExpandActions

2. **检查行为开关**
   ```
   [ √ ] 招募行为
   [ √ ] 社交放松
   [ √ ] 社交用餐
   ... 等等
   ```

3. **检查成功率**
   ```
   招募成功率: 100%
   社交放松成功率: 100%
   ```

4. **查看日志**
   ```
   [RimTalk-ExpandActions] 行为 'recruit' 已禁用，跳过执行
   或
   [RimTalk-ExpandActions] 行为 'recruit' 成功率检定失败 (0.85 > 0.80)
   ```

---

### 5. 目标 Pawn 未找到

行为需要找到目标 Pawn，但名字不匹配。

#### 检查方法

搜索日志：
```
[RimTalk-ExpandActions] 未找到名为 'XXX' 的 Pawn
或
[RimTalk-ExpandActions] 名字不匹配 - JSON目标: 'XXX', 实际: 'YYY'
```

**常见问题：**
- AI 使用了昵称，但实际名字不同
- AI 使用了全名，但游戏中只显示昵称
- 中文/英文名字混淆

**解决方案：**
- 在对话中明确提及 Pawn 的确切名字
- 使用 Mod 设置中的"显示详细日志"查看匹配过程

---

### 6. 行为执行失败

行为开始执行，但因为游戏状态不满足条件而失败。

#### 常见失败原因

**招募 (recruit):**
- ? 需要：NPC 不是玩家派系
- ? 失败：NPC 已经是殖民者

**社交用餐 (social_dining):**
- ? 需要：双方都在地图上
- ? 需要：有可用的食物
- ? 失败：找不到食物

**社交放松 (social_relax):**
- ? 需要：目标在地图上
- ? 需要：目标没有倒地
- ? 失败：目标正在战斗中

#### 检查方法

搜索日志中的警告：
```
[RimTalk-ExpandActions] 招募失败: XXX 已经是玩家派系成员
[RimTalk-ExpandActions] XXX 找不到可以分享的食物
[RimTalk-ExpandActions] XXX 现在不适合进餐（正忙碌或战斗中）
```

---

## ??? 诊断脚本

将以下代码保存为 `DiagnosticScript_ActionTrigger.cs`，在开发者模式下运行：

```csharp
using System;
using System.Linq;
using Verse;
using RimWorld;
using RimTalkExpandActions.Memory;

// 开发者控制台测试脚本
public static class ActionTriggerDiagnostic
{
    public static void Test()
    {
        Log.Message("═══════════════════════════════════════");
        Log.Message("RimTalk-ExpandActions 行为触发诊断");
        Log.Message("═══════════════════════════════════════");
        
        // 1. 测试 JSON 解析
        Log.Message("\n[1/5] 测试 JSON 解析");
        string testJson = "好啊！{\"action\": \"recruit\", \"target\": \"TestPawn\"}";
        string cleaned = AIResponsePostProcessor.ProcessActionResponse(testJson, null, null);
        Log.Message($"输入: {testJson}");
        Log.Message($"输出: {cleaned}");
        Log.Message($"结果: {(cleaned != testJson ? "? JSON 被检测" : "? JSON 未检测")}");
        
        // 2. 测试行为开关
        Log.Message("\n[2/5] 测试行为开关");
        var settings = RimTalkExpandActionsMod.Settings;
        if (settings != null)
        {
            string[] actions = { "recruit", "social_dining", "social_relax", "romance", "give_inspiration", "force_rest", "give_item", "drop_weapon" };
            foreach (var action in actions)
            {
                bool enabled = settings.IsActionEnabled(action);
                float chance = settings.GetSuccessChance(action);
                Log.Message($"  {action}: {(enabled ? "? 启用" : "? 禁用")} | 成功率: {chance:P0}");
            }
        }
        else
        {
            Log.Warning("  ? Mod 设置未加载");
        }
        
        // 3. 测试 Pawn 查找
        Log.Message("\n[3/5] 测试 Pawn 查找");
        if (Find.Maps != null && Find.Maps.Count > 0)
        {
            var map = Find.Maps[0];
            var colonists = map.mapPawns.FreeColonists;
            if (colonists.Any())
            {
                var testPawn = colonists.First();
                Log.Message($"  测试 Pawn: {testPawn.Name.ToStringShort}");
                Log.Message($"  昵称: {(testPawn.Name as NameTriple)?.Nick ?? "无"}");
                Log.Message($"  派系: {testPawn.Faction?.Name ?? "无"}");
                Log.Message($"  在地图上: {testPawn.Spawned}");
            }
            else
            {
                Log.Warning("  ? 地图上没有殖民者");
            }
        }
        else
        {
            Log.Warning("  ? 没有加载的地图");
        }
        
        // 4. 测试延迟执行
        Log.Message("\n[4/5] 测试延迟执行");
        int testValue = 0;
        LongEventHandler.ExecuteWhenFinished(() => {
            testValue = 42;
            Log.Message($"  ? 延迟执行成功，testValue = {testValue}");
        });
        Log.Message($"  当前 testValue = {testValue} (应该是 0，下一帧变成 42)");
        
        // 5. 测试实际行为执行
        Log.Message("\n[5/5] 模拟行为执行");
        if (Find.Maps != null && Find.Maps.Count > 0 && Find.Maps[0].mapPawns.FreeColonists.Any())
        {
            var pawn = Find.Maps[0].mapPawns.FreeColonists.First();
            Log.Message($"  测试对象: {pawn.Name.ToStringShort}");
            
            try
            {
                // 模拟 social_relax
                Log.Message($"  执行 ExecuteSocialRelax...");
                bool result = RimTalkActions.ExecuteSocialRelax(pawn, pawn.Name.ToStringShort);
                Log.Message($"  结果: {(result ? "? 成功" : "? 失败")}");
            }
            catch (Exception ex)
            {
                Log.Error($"  ? 异常: {ex.Message}");
            }
        }
        
        Log.Message("\n═══════════════════════════════════════");
        Log.Message("诊断完成");
        Log.Message("═══════════════════════════════════════");
    }
}

// 运行测试
ActionTriggerDiagnostic.Test();
```

---

## ?? 诊断检查表

按顺序检查以下项目：

| # | 检查项 | 如何检查 | 预期结果 |
|---|-------|---------|---------|
| 1 | **AI 是否输出 JSON？** | 查看 Player.log 中的 AI 回复 | 包含 `{"action": "..."}` |
| 2 | **JSON 是否被检测？** | 搜索 `检测到动作` | 有相关日志 |
| 3 | **行为是否启用？** | Mod 设置 → 行为开关 | 所有需要的行为都勾选 |
| 4 | **成功率是否合理？** | Mod 设置 → 成功率 | 100% 用于测试 |
| 5 | **目标 Pawn 是否找到？** | 搜索 `未找到名为` | 没有此错误 |
| 6 | **是否有执行错误？** | 搜索 `执行失败` | 没有此错误 |
| 7 | **线程安全是否正常？** | 搜索 `线程安全错误` | 没有此错误 |

---

## ?? 常见场景与解决方案

### 场景 1：AI 理解了但不输出 JSON

**症状：**
- AI 说："好的，我愿意加入你们！"
- 但没有 `{"action": "recruit", "target": "XXX"}`

**原因：** AI 没有严格遵循规则格式

**解决方案：**
1. **强化规则**：在常识库中强调"**必须**"输出 JSON
2. **调整 AI 参数**：降低 temperature，提高遵循性
3. **示例对话**：在规则中添加更多示例

**修改规则（BehaviorRuleContents.cs）：**
```csharp
public const string RECRUIT_RULE = @"当谈话涉及【招募、加入、投靠、派系】话题，且目标NPC在对话中明确表示同意加入玩家派系时，**你必须严格按照以下格式输出**：首先是正常的对话内容，然后**紧跟**JSON指令{""action"": ""recruit"", ""target"": ""NPC名字""}。例如：'好吧，我愿意加入你们！{""action"": ""recruit"", ""target"": ""张三""}'。注意：JSON指令是**强制性的**，不输出JSON则行为不会触发。";
```

### 场景 2：名字匹配失败

**症状：**
- AI 输出：`{"action": "recruit", "target": "艾莉丝"}`
- 日志：`未找到名为 '艾莉丝' 的 Pawn`
- 实际名字：`Alice`

**解决方案：**
1. **在对话中确认名字**：
   ```
   玩家：Alice，加入我们吧！
   AI：好的，我愿意！{"action": "recruit", "target": "Alice"}
   ```

2. **改进名字匹配算法**（已实现）：
   - 模糊匹配昵称和全名
   - 忽略空格和大小写

### 场景 3：延迟执行失败

**症状：**
- 日志：`检测到动作 'social_relax'，将在主线程执行`
- 然后：`FindPawnByName: 线程安全错误`

**原因：** 即使使用了 `LongEventHandler.ExecuteWhenFinished`，某些情况下仍可能失败

**解决方案：** 已在 v1.1.1 中修复，使用双重保护

---

## ?? 调试技巧

### 启用详细日志

1. **Mod 设置** → `启用详细日志`
2. 重新加载游戏
3. 触发对话
4. 查看 Player.log

### 手动测试行为

在开发者控制台（Dev Mode, F12）运行：

```csharp
// 测试招募
var pawn = Find.Maps[0].mapPawns.FreeColonists.First();
RimTalkActions.ExecuteRecruit(pawn, null);

// 测试社交放松
RimTalkActions.ExecuteSocialRelax(pawn, pawn.Name.ToStringShort);
```

### 检查 Harmony 补丁

确保 `RimTalkBridge.Postfix` 正在拦截 AI 回复：

```
搜索日志：[RimTalk-ExpandActions] 对话已处理
```

如果没有这条日志 → Harmony 补丁没有生效

---

## ? 验证修复

修复后，应该看到完整的执行流程：

```
1. [RimTalk-ExpandActions] Prompt 注入检查
2. [RimTalk] AI 正在生成回复...
3. [RimTalk] Response: "好啊！{"action": "social_relax", "targets": "Val"}"
4. [RimTalk-ExpandActions] 检测到动作 'social_relax'，将在主线程执行
5. [RimTalk-ExpandActions] 检测到社交放松指令，参与者: Val
6. [RimTalk-ExpandActions] Val 开始社交放松
7. [RimTalk-ExpandActions] 成功: 1 名小人开始社交放松
8. (游戏消息) 1 名小人开始社交放松
```

---

## ?? 仍然无法解决？

如果按照上述步骤仍然无法解决，请提供：

1. **完整的 Player.log**（最后 200 行）
2. **Mod 设置截图**
3. **AI 的原始回复**
4. **游戏版本和 Mod 列表**

提交 Issue 到 GitHub：
https://github.com/sanguodxj-byte/RimTalk-ExpandActions/issues

---

**最后更新：** 2025/12/15  
**文档版本：** v1.0
