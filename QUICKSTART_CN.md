# RimTalk-ExpandMemory 对话招募功能 - 快速入门

## ?? 文件结构

```
RimTalk-ExpandActions/
├── Source/
│   ├── Memory/
│   │   ├── Actions/
│   │   │   └── RimTalkActions.cs           # ? 任务1: 招募执行器
│   │   ├── Utils/
│   │   │   └── RuleInjector.cs             # ? 任务2: 规则注入器
│   │   ├── AIResponsePostProcessor.cs       # ? 任务3: 回复处理器
│   │   ├── CommonKnowledgeLibrary.cs        # ?? 辅助类: 常识库
│   │   └── MemoryManager.cs                 # ?? 辅助类: 单例管理器
│   └── Examples/
│       └── DialogSystemIntegrationExample.cs # ?? 集成示例
├── RimTalkExpandMemory.csproj               # 项目文件
└── README_RECRUIT_FEATURE.md                # 详细文档
```

## ?? 三步快速集成

### 步骤 1: 编译项目

1. 确保 RimWorld 引用路径正确（在 `.csproj` 文件中）
2. 使用 Visual Studio 或 MSBuild 编译项目
3. 将生成的 DLL 复制到 Mod 的 `Assemblies/` 目录

### 步骤 2: 在对话系统中调用

在您处理 AI 回复的代码中添加：

```csharp
using RimTalkExpandMemory.Memory;

// 处理 AI 回复
string aiResponse = GetAIResponse(); // 您的 AI 调用
string cleanText = AIResponsePostProcessor.ProcessActionResponse(
    aiResponse,
    targetNPC,      // 对话的 NPC
    playerColonist  // 玩家角色
);

// 显示清理后的文本
ShowDialog(cleanText);
```

### 步骤 3: 在 AI 提示词中包含系统规则

```csharp
var memoryManager = MemoryManager.Instance;
var systemRules = memoryManager.CommonKnowledge
    .GetAllEntries()
    .Where(e => e.Tag == "系统指令");

foreach (var rule in systemRules)
{
    promptBuilder.AppendLine(rule.Content);
}
```

## ? 完成的任务

### ? 任务 1: RimTalkActions.cs
**功能**: 执行游戏内招募逻辑
- ?? 参数有效性检查
- ?? 派系切换 `SetFaction()`
- ?? 清除 Lord AI 逻辑
- ?? 清除客人/囚犯状态
- ?? 发送招募成功信件
- ?? 完整的错误处理

### ? 任务 2: RuleInjector.cs
**功能**: 启动时自动注入招募规则
- ?? 使用 `[StaticConstructorOnStartup]`
- ?? 使用 `LongEventHandler.ExecuteWhenFinished`
- ?? 检查重复规则（ID 和标签）
- ?? 创建高优先级规则（Importance: 1.0）
- ?? 包含关键词：招募, 加入, 投靠等
- ?? 添加到常识库
- ?? 完整的错误处理

### ? 任务 3: AIResponsePostProcessor.cs
**功能**: 解析 AI 回复中的 JSON 指令
- ?? JSON 检测和解析（正则表达式）
- ?? 目标名字验证（模糊匹配）
- ?? 执行招募动作
- ?? 移除 JSON 返回纯净文本
- ?? 支持多种 JSON 格式（有无空格）
- ?? 完整的错误处理

## ?? 额外提供的文件

### CommonKnowledgeLibrary.cs
- 常识库管理类
- 支持添加、查询、搜索条目
- 如果您已有实现，可以删除此文件

### MemoryManager.cs
- 单例管理器
- 线程安全的实例获取
- 如果您已有实现，可以删除此文件

### DialogSystemIntegrationExample.cs
- 6 个集成示例
- 展示各种使用场景
- 包含调试方法

## ?? 自定义配置

### 修改招募规则内容

编辑 `RuleInjector.cs` 中的 `CreateRecruitmentRule()` 方法：

```csharp
Content = @"您的自定义规则内容..."
```

### 修改关键词

```csharp
Keywords = "您的, 自定义, 关键词"
```

### 添加新动作类型

1. 在 `RimTalkActions.cs` 添加新方法（如 `ExecuteTrade`）
2. 在 `RuleInjector.cs` 注入新规则
3. 在 `AIResponsePostProcessor.cs` 添加解析分支

## ?? 调试技巧

### 查看日志

所有日志都带有前缀 `[RimTalk-ExpandMemory]`，在 RimWorld 日志中搜索：

```
[RimTalk-ExpandMemory] 成功注入招募规则到常识库
[RimTalk-ExpandMemory] 检测到招募指令，尝试招募: XXX
[RimTalk-ExpandMemory] 成功通过对话招募: XXX
```

### 测试规则是否加载

在游戏内开发者控制台执行：

```csharp
var rule = RimTalkExpandMemory.Memory.MemoryManager.Instance.CommonKnowledge.GetEntryById("sys-rule-recruit");
Log.Message(rule != null ? "规则已加载" : "规则未加载");
```

### 测试 JSON 解析

使用示例中的 `TestJsonParsing()` 方法。

## ?? 注意事项

1. **命名空间**: 所有类使用 `RimTalkExpandMemory.Memory`，可根据项目修改
2. **依赖库**: 确保引用了正确的 RimWorld DLL
3. **.NET 版本**: 目标框架为 .NET Framework 4.7.2
4. **线程安全**: MemoryManager 使用双重检查锁定模式
5. **错误处理**: 所有方法都有 try-catch，不会导致游戏崩溃

## ?? 更多信息

详细文档请参考 `README_RECRUIT_FEATURE.md`

## ?? 测试流程

1. ? 编译成功
2. ? 游戏启动无报错
3. ? 查看日志确认规则注入
4. ? 与 NPC 对话测试招募
5. ? 检查信件是否发送
6. ? 确认 NPC 加入派系

## ?? 技术支持

如遇问题：
1. 检查 RimWorld 日志文件
2. 搜索 `[RimTalk-ExpandMemory]` 前缀
3. 确认 DLL 引用路径正确
4. 验证常识库初始化成功

---

**状态**: ? 所有任务已完成
**代码风格**: 健壮、带注释、完整错误处理
**C# 版本**: 7.0+ (支持 .NET Framework 4.7.2)
