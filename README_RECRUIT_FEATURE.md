# RimTalk-ExpandMemory - 对话招募功能

## 功能概述

此模块实现了通过对话招募 NPC 的功能。AI 在生成回复时，如果检测到 NPC 同意加入玩家派系，会在回复文本中嵌入 JSON 指令，系统会自动解析并执行招募动作。

## 核心组件

### 1. RimTalkActions.cs (游戏逻辑执行器)
**位置**: `Source/Memory/Actions/RimTalkActions.cs`

**功能**:
- 执行实际的招募逻辑
- 切换 NPC 派系
- 清除旧的 AI 控制（Lord）
- 清除客人/囚犯状态
- 发送招募成功信件

**核心方法**:
```csharp
RimTalkActions.ExecuteRecruit(Pawn pawnToRecruit, Pawn recruiter = null)
```

### 2. RuleInjector.cs (规则注入器)
**位置**: `Source/Memory/Utils/RuleInjector.cs`

**功能**:
- 游戏启动时自动运行
- 向常识库注入招募指令规则
- 防止重复注入
- 告诉 AI 如何响应招募场景

**自动注入的规则内容**:
- ID: `sys-rule-recruit`
- 标签: `系统指令`
- 关键词: 招募, 加入, 投靠, 派系, 跟我走等
- 优先级: 最高 (1.0)

### 3. AIResponsePostProcessor.cs (回复后处理器)
**位置**: `Source/Memory/AIResponsePostProcessor.cs`

**功能**:
- 解析 AI 回复中的 JSON 指令
- 验证目标名字是否匹配
- 执行相应的游戏动作
- 清理文本中的 JSON 标记

**核心方法**:
```csharp
string cleanText = AIResponsePostProcessor.ProcessActionResponse(
    responseText, 
    targetPawn, 
    recruiter
);
```

## 集成步骤

### 步骤 1: 在对话系统中调用后处理器

在您的对话系统接收到 AI 回复后，调用后处理器：

```csharp
using RimTalkExpandMemory.Memory;

// 在处理 AI 回复的地方
public void HandleAIResponse(string aiResponse, Pawn targetNPC, Pawn playerPawn)
{
    // 处理动作指令并获取清理后的文本
    string cleanText = AIResponsePostProcessor.ProcessActionResponse(
        aiResponse, 
        targetNPC, 
        playerPawn
    );
    
    // 显示清理后的文本给玩家
    ShowDialogBubble(cleanText);
}
```

### 步骤 2: 确保常识库被 AI 使用

确保您的 AI 提示词构建逻辑会检索常识库中的内容，特别是标记为"系统指令"的条目。

示例：
```csharp
var systemRules = MemoryManager.Instance.CommonKnowledge
    .GetAllEntries()
    .Where(e => e.Tag == "系统指令")
    .OrderByDescending(e => e.Importance);

foreach (var rule in systemRules)
{
    promptBuilder.AppendLine(rule.Content);
}
```

### 步骤 3: 测试招募流程

1. 启动游戏，确保没有报错
2. 与敌对或中立派系的 NPC 对话
3. 引导对话走向招募话题（使用关键词：加入、招募、跟我走等）
4. 如果 AI 判断 NPC 同意，回复末尾会包含 JSON
5. 系统自动执行招募并发送信件

## JSON 格式

AI 应该在回复末尾生成如下格式的 JSON：

```json
{"action": "recruit", "target": "NPC名字"}
```

示例完整回复：
```
好的，我愿意加入你们的殖民地！看起来这里很不错。{"action": "recruit", "target": "艾莉丝"}
```

系统会自动：
1. 检测并解析 JSON
2. 验证名字是否匹配
3. 执行招募
4. 移除 JSON，只显示："好的，我愿意加入你们的殖民地！看起来这里很不错。"

## 错误处理

所有组件都包含完整的 try-catch 错误处理，不会因异常导致游戏崩溃。错误日志会输出到 RimWorld 的日志文件中，前缀为 `[RimTalk-ExpandMemory]`。

## 日志输出

### 成功日志
```
[RimTalk-ExpandMemory] 成功注入招募规则到常识库
[RimTalk-ExpandMemory] 检测到招募指令，尝试招募: 艾莉丝
[RimTalk-ExpandMemory] 成功通过对话招募: 艾莉丝
```

### 警告日志
```
[RimTalk-ExpandMemory] 招募规则已存在，跳过注入
[RimTalk-ExpandMemory] 名字不匹配 - JSON目标: 'Alice', 实际: '艾莉丝'
```

### 错误日志
```
[RimTalk-ExpandMemory] ExecuteRecruit: pawnToRecruit 为 null
```

## 扩展功能

此架构可以轻松扩展支持其他动作：

### 添加新动作类型

1. 在 `RimTalkActions.cs` 中添加新方法：
```csharp
public static void ExecuteTrade(Pawn trader, Pawn player)
{
    // 实现交易逻辑
}
```

2. 在 `RuleInjector.cs` 中注入对应规则

3. 在 `AIResponsePostProcessor.cs` 中添加解析分支：
```csharp
if (action == "recruit")
{
    ExecuteRecruitAction(targetPawn, recruiter);
}
else if (action == "trade")
{
    ExecuteTradeAction(targetPawn, recruiter);
}
```

## 依赖关系

### 必需的 RimWorld 引用
- `RimWorld.dll`
- `UnityEngine.CoreModule.dll`
- `Assembly-CSharp.dll` (Verse)

### .NET Framework
- 目标框架: .NET Framework 4.7.2
- C# 语言版本: 7.0 或更高

## 兼容性说明

### 如果您已有 MemoryManager 或 CommonKnowledgeLibrary
本模块提供了基础实现，如果您已有这些类：
1. 删除 `Source/Memory/MemoryManager.cs`
2. 删除 `Source/Memory/CommonKnowledgeLibrary.cs`
3. 确保您的实现包含相同的方法签名

### 命名空间
所有类使用命名空间 `RimTalkExpandMemory.Memory`，可根据您的项目结构修改。

## 许可证

请根据您的项目需求添加适当的许可证信息。

## 支持与反馈

如有问题或建议，请通过以下方式反馈：
- 检查 RimWorld 日志文件
- 查看 `[RimTalk-ExpandMemory]` 前缀的日志输出
