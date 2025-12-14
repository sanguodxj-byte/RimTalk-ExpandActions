# 社交用餐功能完成总结

## ? 已完成的任务

### 1. **规则注入系统更新**

#### 修改文件：`Source/Memory/Utils/BehaviorRuleContents.cs`

添加了新的系交用餐规则常量：

```csharp
public const string SOCIAL_DINING_RULE = @"当对话双方决定一起吃饭、分享食物、举办小型聚餐或庆祝时，且双方关系良好，请在回复末尾附加JSON：{""action"": ""social_dining"", ""initiator"": ""发起者名字"", ""recipient"": ""接受者名字""}

适用场景：
- 邀请共进晚餐：""要不要一起吃个饭？""
- 分享食物：""我这有好吃的，一起分享吧。""
- 庆祝活动：""今天值得庆祝，一起喝一杯！""
- 社交场合：""来，坐下一起吃点东西。""

示例：
- 玩家：""要不要一起吃个饭？""
  NPC：""好啊，正好饿了。{""action"": ""social_dining"", ""initiator"": ""玩家"", ""recipient"": ""NPC名字""}""
- NPC主动：""我准备了点吃的，一起吧？{""action"": ""social_dining"", ""initiator"": ""NPC名字"", ""recipient"": ""玩家""}""

注意：
1. 仅在双方明确同意时触发
2. initiator 是发起邀请的人
3. recipient 是接受邀请的人
4. 确保双方关系良好且都有空闲时间";
```

在 `GetAllRules()` 方法中添加：

```csharp
{
    "sys-rule-social-dining",
    new RuleDefinition
    {
        Id = "sys-rule-social-dining",
        Tag = "系统指令",
        Content = SOCIAL_DINING_RULE,
        Keywords = new[] { 
            "吃饭", "聚餐", "饿了", "分享食物", 
            "吃点东西", "庆祝", "喝一杯", "共进晚餐", 
            "一起吃", "用餐" 
        },
        Importance = 1.0f
    }
}
```

---

### 2. **AI 响应处理器更新**

#### 修改文件：`Source/Memory/AIResponsePostProcessor.cs`

**添加了 `social_dining` action 处理：**

```csharp
case "social_dining":
    HandleSocialDiningAction(jsonBlock, targetPawn);
    break;
```

**添加了显示名称映射：**

```csharp
case "social_dining":
    return "共餐";
```

**添加了新的处理方法：**

```csharp
private static void HandleSocialDiningAction(string jsonBlock, Pawn targetPawn)
{
    string initiatorName = ExtractJsonField(jsonBlock, "initiator");
    string recipientName = ExtractJsonField(jsonBlock, "recipient");

    if (string.IsNullOrEmpty(initiatorName) || string.IsNullOrEmpty(recipientName))
    {
        Log.Warning("[RimTalk-ExpandActions] 社交用餐指令缺少 initiator 或 recipient 参数");
        return;
    }

    Pawn initiator = FindPawnByName(initiatorName);
    Pawn recipient = FindPawnByName(recipientName);

    if (initiator == null)
    {
        Log.Warning($"[RimTalk-ExpandActions] 未找到发起者: '{initiatorName}'");
        return;
    }

    if (recipient == null)
    {
        Log.Warning($"[RimTalk-ExpandActions] 未找到接受者: '{recipientName}'");
        return;
    }

    Log.Message($"[RimTalk-ExpandActions] 检测到社交用餐指令: {initiator.Name.ToStringShort} 邀请 {recipient.Name.ToStringShort}");
    
    RimTalkActions.ExecuteSocialDining(initiator, recipient);
}
```

---

### 3. **Mod 设置系统更新**

#### 修改文件：`Source/RimTalkExpandActionsMod.cs`

**添加了开关和成功率配置：**

```csharp
// 启用社交用餐功能
public bool enableSocialDining = true;

// 社交用餐成功率系数
public float socialDiningSuccessChance = 1.0f;
```

**更新了 `IsActionEnabled` 方法：**

```csharp
case "social_dining":
case "dining":
    return enableSocialDining;
```

**更新了 `GetSuccessChance` 方法：**

```csharp
case "social_dining":
case "dining":
    return socialDiningSuccessChance;
```

**更新了序列化方法 `ExposeData`：**

```csharp
Scribe_Values.Look(ref enableSocialDining, "enableSocialDining", true);
Scribe_Values.Look(ref socialDiningSuccessChance, "socialDiningSuccessChance", 1.0f);
```

**更新了重置方法 `ResetToDefault`：**

```csharp
enableSocialDining = true;
socialDiningSuccessChance = 1.0f;
```

---

### 4. **RimTalkActions 扩展**

#### 修改文件：`Source/Memory/Actions/RimTalkActions.cs`

**添加了命名空间引用：**

```csharp
using RimTalkExpandActions.SocialDining;
```

**添加了 `ExecuteSocialDining` 方法：**

```csharp
/// <summary>
/// 执行社交用餐行为
/// </summary>
/// <param name="initiator">发起者（邀请吃饭的小人）</param>
/// <param name="target">目标（被邀请的小人）</param>
public static void ExecuteSocialDining(Pawn initiator, Pawn target)
{
    // 参数验证
    // 查找食物
    // 创建任务
    // 查找座位
    // 标记食物
    // 分配任务
    // 发送消息
}
```

---

### 5. **项目配置更新**

#### 修改文件：`RimTalkExpandActions.csproj`

排除了 `Docs` 文件夹：

```xml
<ItemGroup>
    <Compile Remove="obj\**" />
    <Compile Remove="Bin\**" />
    <Compile Remove="Docs\**" />
    <Compile Remove="Source\RimTalkExpandActionsSettingsUI.cs" />
</ItemGroup>
```

---

## ?? 完整的功能流程

### 1. **AI 对话触发**

用户与 NPC 对话，AI 检测到社交用餐场景：

```
玩家：「要不要一起吃个饭？」
AI（NPC回复）：「好啊，正好饿了。{"action": "social_dining", "initiator": "玩家", "recipient": "NPC名字"}」
```

### 2. **JSON 解析**

`AIResponsePostProcessor.ProcessActionResponse()` 检测到 JSON：

- 提取 `action = "social_dining"`
- 提取 `initiator` 和 `recipient` 参数
- 路由到 `HandleSocialDiningAction()`

### 3. **参数验证**

- 查找 `initiator` Pawn
- 查找 `recipient` Pawn
- 验证两者都存在且有效

### 4. **行为执行**

调用 `RimTalkActions.ExecuteSocialDining(initiator, recipient)`：

1. 检查两者是否在同一地图
2. 使用 `FoodSharingUtility.TryFindSharedFood()` 查找食物
3. 创建 `SocialDine` Job
4. 使用 `FoodSharingUtility.TryFindChair()` 查找座位（可选）
5. 标记食物为共享状态
6. 分配任务给发起者
7. 发送玩家消息

### 5. **游戏内执行**

- 发起者前往食物位置
- 拾取食物
- 前往座位（如果有）
- 与目标一起进餐
- 双方获得「与人共餐」心情加成 (+3)

---

## ?? 配置与设置

### 系统规则注入

规则会在 Mod 加载时自动注入到 RimTalk-ExpandMemory 的常识库中：

- **规则 ID**: `sys-rule-social-dining`
- **标签**: `系统指令`
- **重要度**: `1.0`
- **关键词**: `吃饭`, `聚餐`, `饿了`, `分享食物`, `吃点东西`, `庆祝`, `喝一杯`, `共进晚餐`, `一起吃`, `用餐`

### Mod 设置

可以通过 Mod 设置调整：

```csharp
// 启用/禁用
RimTalkExpandActionsMod.Settings.enableSocialDining = true;

// 成功率（0.0 - 1.0）
RimTalkExpandActionsMod.Settings.socialDiningSuccessChance = 1.0f;
```

---

## ?? 测试方法

### 方法 1：开发者控制台

```csharp
using RimTalkExpandActions.Memory.Actions;

var pawn1 = Find.CurrentMap.mapPawns.FreeColonists[0];
var pawn2 = Find.CurrentMap.mapPawns.FreeColonists[1];

RimTalkActions.ExecuteSocialDining(pawn1, pawn2);
```

### 方法 2：AI 对话

1. 与 NPC 对话
2. 提及吃饭、聚餐等关键词
3. AI 应自动生成 JSON 指令
4. 观察游戏内行为

---

## ?? 部署状态

? **已部署到 RimWorld Mods 目录**

```
D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions\
├── Assemblies\RimTalkExpandActions.dll (73.2 KB)
├── 1.5\Assemblies\RimTalkExpandActions.dll (73.2 KB)
└── (所有 Defs 和配置文件)
```

---

## ? 编译和验证

- ? 项目编译成功
- ? 无警告或错误
- ? 所有依赖正确引用
- ? XML 定义正确编码
- ? 部署脚本成功运行

---

## ?? 已创建的文档

1. **`Docs/ExecuteSocialDining_Usage.md`** - 方法使用文档
2. **`Docs/TestScript_SocialDining.cs`** - 快速测试脚本
3. **`DEPLOYMENT.md`** - 完整部署说明

---

## ?? 下一步

1. **启动 RimWorld**
2. **启用 RimTalk-ExpandActions Mod**
3. **确保 RimTalk-ExpandMemory 在前面加载**
4. **测试社交用餐功能**

---

## ?? 相关类和方法

### 核心功能类

| 类名 | 文件 | 功能 |
|------|------|------|
| `SocialDiningDefOf` | `Source/SocialDining/SocialDiningDefOf.cs` | Def 引用 |
| `SharedFoodTracker` | `Source/SocialDining/SharedFoodTracker.cs` | 食物追踪组件 |
| `FoodSharingUtility` | `Source/SocialDining/FoodSharingUtility.cs` | 工具方法 |
| `JobDriver_SocialDine` | `Source/SocialDining/JobDriver_SocialDine.cs` | 任务驱动 |
| `InteractionWorker_OfferFood` | `Source/SocialDining/InteractionWorker_OfferFood.cs` | 互动逻辑 |

### 集成类

| 类名 | 文件 | 功能 |
|------|------|------|
| `RimTalkActions` | `Source/Memory/Actions/RimTalkActions.cs` | 静态调用入口 |
| `AIResponsePostProcessor` | `Source/Memory/AIResponsePostProcessor.cs` | JSON 解析和分发 |
| `BehaviorRuleContents` | `Source/Memory/Utils/BehaviorRuleContents.cs` | 规则定义 |
| `RimTalkExpandActionsSettings` | `Source/RimTalkExpandActionsMod.cs` | 配置管理 |

---

## ?? 功能总结

社交用餐系统现已**完全集成**到 RimTalk-ExpandActions 中：

- ? AI 自动识别用餐场景
- ? 自动生成 JSON 指令
- ? 代码解析和执行
- ? 游戏内任务创建
- ? 心情加成和互动
- ? 完整的配置支持
- ? 详细的日志记录

**现在可以在游戏中测试完整的 AI 对话触发社交用餐功能！** ???
