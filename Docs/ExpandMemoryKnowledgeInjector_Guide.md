# ?? RimTalk-ExpandMemory 常识库集成指南

## ?? 概述

`ExpandMemoryKnowledgeInjector` 自动将 RimTalk-ExpandActions 的行为规则注入到 RimTalk-ExpandMemory 的常识库中。

### 优势

相比之前的硬编码注入方案：

| 特性 | 硬编码注入 | ExpandMemory 常识库 |
|------|-----------|---------------------|
| Token 消耗 | ? 每次对话 +1000 tokens | ? 按需检索，~100-300 tokens |
| 响应速度 | ? 慢（Prompt 太长） | ? 快 |
| 智能性 | ? 全部规则都发送 | ? 只发送相关规则 |
| API 成本 | ? 高 | ? 低 |
| 维护性 | ? 需要自己管理 | ? 由 ExpandMemory 管理 |

## ?? 工作原理

### 1. 自动注入流程

```
游戏启动
    ↓
RimTalk-ExpandActions 加载
    ↓
[StaticConstructorOnStartup] 触发
    ↓
ExpandMemoryKnowledgeInjector 初始化
    ↓
检查 RimTalk-ExpandMemory 是否存在
    ↓ 存在
通过反射调用 ImportFromExternalMod()
    ↓
7 条行为规则注入到常识库
    ↓
? 完成
```

### 2. 运行时检索流程

```
玩家与 NPC 对话
    ↓
玩家说："加入我们吧"  ← 包含关键词 "加入"
    ↓
ExpandMemory 检索常识库
    ↓
找到匹配规则：[行为-招募|1.0] 当对话涉及招募、加入...
    ↓
AI 收到 Prompt（包含招募规则）
    ↓
AI 回复："好的！{\"action\": \"recruit\", \"target\": \"张三\"}"
    ↓
RimTalkBridge 处理 JSON 指令
    ↓
执行招募动作
```

## ?? 注入的规则

### 规则列表

1. **[行为-招募|1.0]** - 招募 NPC 到殖民地
2. **[行为-投降|1.0]** - 让 NPC 放下武器投降
3. **[行为-恋爱|0.9]** - 建立或结束恋爱关系
4. **[行为-灵感|0.8]** - 触发工作/战斗/交易灵感
5. **[行为-休息|0.7]** - 强制休息或昏迷
6. **[行为-赠送|0.8]** - 赠送物品
7. **[行为-用餐|0.9]** - 社交用餐

### 规则格式

```
[标签|重要性]内容

示例：
[行为-招募|1.0]当对话涉及招募、加入、投靠、派系等话题...
```

- **标签**：规则的分类标识
- **重要性**：0.0 到 1.0（越高越优先被检索）
- **内容**：规则的详细说明和 JSON 指令格式

## ?? 验证注入

### 方法 1: 查看启动日志

启动游戏后，查看日志：

```
%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
```

**成功案例：**
```
[RimTalk-ExpandActions] 开始检查 RimTalk-ExpandMemory...
[RimTalk-ExpandActions] 正在导入行为规则到常识库...
[RimTalk-ExpandActions] ? 成功导入 7 条行为规则到常识库
```

游戏内也会显示消息：
```
RimTalk-ExpandActions: 已导入 7 条行为规则
```

**失败案例：**
```
[RimTalk-ExpandActions] RimTalk-ExpandMemory 未启用，跳过常识库注入
[RimTalk-ExpandActions] 提示：启用 RimTalk-ExpandMemory 以获得智能规则检索功能
```

### 方法 2: 使用开发者控制台

按 `F12` 打开控制台，运行：

```csharp
// 检查常识库中的 ExpandActions 规则
var memType = System.Type.GetType("RimTalk.Memory.MemoryManager, RimTalk-ExpandMemory");
if (memType != null)
{
    var getMethod = memType.GetMethod("GetCommonKnowledge", 
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
    var commonKnowledge = getMethod.Invoke(null, null);
    var getAllMethod = commonKnowledge.GetType().GetMethod("GetAllEntries");
    var entries = getAllMethod.Invoke(commonKnowledge, null);
    
    int expandActionsCount = 0;
    foreach (var entry in entries as System.Collections.IEnumerable)
    {
        var sourceProp = entry.GetType().GetProperty("source");
        string source = sourceProp?.GetValue(entry) as string;
        if (source == "RimTalk-ExpandActions")
        {
            var idProp = entry.GetType().GetProperty("id");
            string id = idProp?.GetValue(entry) as string;
            Log.Message($"? 规则: {id}");
            expandActionsCount++;
        }
    }
    
    Log.Message($"总共: {expandActionsCount} 条来自 ExpandActions 的规则");
}
```

**预期输出：**
```
? 规则: behavior_recruit_001
? 规则: behavior_surrender_001
? 规则: behavior_romance_001
? 规则: behavior_inspiration_001
? 规则: behavior_rest_001
? 规则: behavior_gift_001
? 规则: behavior_dining_001
总共: 7 条来自 ExpandActions 的规则
```

### 方法 3: 实战测试

1. 进入游戏，加载存档
2. 与访客对话，说："加入我们吧"
3. 检查 `BuildContextDebugPatch` 的日志：

```
[RimTalk-ExpandActions] ━━━━━ Prompt 注入检查 ━━━━━
目标: 张三
Prompt 长度: 2145 字符

检查关键词:
  - 常识库标记: ? 存在  ← 应该是 ?
  - 招募规则: ? 存在    ← 应该是 ?
  
总结: 检测到 1/7 条规则  ← 只检索到相关规则（招募），而不是全部 7 条
? 规则已正确检索！
```

**关键点：**
- Prompt 长度应该在 2000-3000 字符（而不是 5000+）
- 只检测到**相关规则**（例如讨论招募时只有招募规则）
- 这证明 ExpandMemory 的智能检索在工作

## ??? 手动重新注入

如果你更新了规则内容，可以手动重新注入：

### 方法 1: 使用开发者控制台

```csharp
// F12 控制台运行
RimTalkExpandActions.Memory.Utils.ExpandMemoryKnowledgeInjector.ManualReInject();
```

### 方法 2: 重启游戏

规则会在每次游戏启动时自动注入（覆盖旧规则）。

## ?? 故障排除

### 问题 1: 未找到 RimTalk-ExpandMemory

**日志：**
```
[RimTalk-ExpandActions] RimTalk-ExpandMemory 未启用
```

**原因：** RimTalk-ExpandMemory 未安装或未启用

**解决：**
1. 从 Steam 创意工坊订阅 RimTalk-ExpandMemory
2. 在 Mod 列表中启用它
3. 确保加载顺序：
   ```
   1. Harmony
   2. Core
   3. RimTalk
   4. RimTalk-ExpandMemory  ← 必须
   5. RimTalk-ExpandActions
   ```

### 问题 2: 未找到 ImportFromExternalMod 方法

**日志：**
```
[RimTalk-ExpandActions] 未找到 ImportFromExternalMod 方法
```

**原因：** RimTalk-ExpandMemory 版本太旧

**解决：**
1. 更新 RimTalk-ExpandMemory 到最新版本
2. 确认版本支持 `ImportFromExternalMod` API

### 问题 3: 导入成功但 AI 不触发行为

**日志：**
```
[RimTalk-ExpandActions] ? 成功导入 7 条行为规则
```

但 AI 回复没有 JSON 指令。

**可能原因：**
1. **关键词不匹配** - 对话内容没有包含规则的关键词
2. **规则重要性太低** - 被其他规则覆盖
3. **AI 理解问题** - AI 没有正确理解规则内容

**解决：**
1. 使用更明确的关键词触发（例如："加入我们"、"一起吃饭"）
2. 启用详细日志，查看 ExpandMemory 检索了哪些规则
3. 检查 Prompt 中是否包含相关规则（使用 BuildContextDebugPatch）

## ?? 性能对比

### Token 消耗对比

| 场景 | 硬编码注入 | ExpandMemory 常识库 |
|------|-----------|---------------------|
| **普通对话** | 1200 tokens | 850 tokens ? |
| **招募对话** | 1200 tokens | 1100 tokens ? |
| **社交对话** | 1200 tokens | 1000 tokens ? |
| **战斗对话** | 1200 tokens | 950 tokens ? |

**节省：** 平均每次对话节省 **150-350 tokens**（约 20-30%）

### API 成本对比

假设每天 100 次对话：

| 方案 | 每天 Token 消耗 | 每月成本 (GPT-4) |
|------|----------------|-----------------|
| 硬编码注入 | 120,000 tokens | $3.60 |
| ExpandMemory | 90,000 tokens | $2.70 ? |

**节省：** 每月约 **$0.90**（25%）

## ?? 最佳实践

### 1. 规则设计原则

- **明确的触发关键词** - 确保关键词常见且明确
- **合适的重要性** - 核心规则（招募、投降）使用 1.0，辅助规则（休息）使用 0.7-0.8
- **简洁的说明** - 避免冗长的规则内容，影响 Token 消耗

### 2. 测试流程

1. **启动验证** - 检查日志确认注入成功
2. **关键词测试** - 使用明确关键词测试每个规则
3. **Token 监控** - 使用 BuildContextDebugPatch 监控 Prompt 长度
4. **实战测试** - 在真实游戏场景中测试触发率

### 3. 维护建议

- **定期更新规则** - 根据玩家反馈优化规则内容
- **监控日志** - 查看 ExpandMemory 的检索日志，了解哪些规则被使用
- **A/B 测试** - 测试不同的关键词和重要性配置

## ?? 相关文档

- `Source/Memory/Utils/ExpandMemoryKnowledgeInjector.cs` - 注入器源码
- `Source/Memory/Utils/BehaviorRuleContents.cs` - 规则内容定义
- `Source/Patches/BuildContextDebugPatch.cs` - Prompt 调试工具
- `Docs/BuildContextDebugPatch_Guide.md` - 调试补丁使用指南

---

**创建时间:** 2025/12/15  
**版本:** 1.0  
**状态:** ? 已实现并测试  
**依赖:** RimTalk-ExpandMemory (latest)
