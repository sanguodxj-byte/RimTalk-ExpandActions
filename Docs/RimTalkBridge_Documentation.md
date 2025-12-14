# RimTalk 对话桥接补丁文档

## ?? 概述

`RimTalkBridge` 是一个 Harmony 补丁，用于拦截 RimTalk Mod 的对话流，自动处理 AI 回复中的 JSON 指令并执行相应的游戏行为。

## ?? 工作原理

### 1. **动态补丁**

使用 `[HarmonyPatch]` 特性和动态目标方法查找，避免硬依赖：

```csharp
[HarmonyPatch]
public static class RimTalkBridge
{
    static bool Prepare() { ... }          // 检查 RimTalk 是否存在
    static MethodBase TargetMethod() { ... } // 查找目标方法
    static void Prefix(...) { ... }        // 前置补丁逻辑
}
```

### 2. **反射查找**

使用 `AccessTools` 动态查找 RimTalk 的类和方法：

```csharp
rimTalkServiceType = AccessTools.TypeByName("RimTalk.Service.TalkService");
targetMethod = AccessTools.Method(rimTalkServiceType, "ConsumeTalk");
```

### 3. **容错机制**

- 如果找不到 RimTalk，`Prepare()` 返回 `false`，补丁不会应用
- 记录详细日志，方便调试
- 多个方法名回退（`ConsumeTalk` → `ProcessTalk` → `ReceiveResponse`）

## ?? 执行流程

```
RimTalk AI 回复
     ↓
ConsumeTalk(speaker, listener, text)
     ↓
[RimTalkBridge.Prefix] 拦截
     ↓
检测 JSON 指令 ({\"action\": ...})
     ↓
AIResponsePostProcessor.ProcessActionResponse()
     ├─ 解析 JSON
     ├─ 执行行为（招募/用餐/等）
     └─ 返回清理后的文本
     ↓
更新 text 参数（去除 JSON）
     ↓
RimTalk 显示对话气泡
```

## ?? 示例场景

### 场景 1：AI 触发招募

**AI 原始回复：**
```
"好吧，我愿意加入你们。{"action": "recruit", "target": "NPC名字"}"
```

**补丁处理：**
1. 拦截文本
2. 提取 JSON：`{"action": "recruit", "target": "NPC名字"}`
3. 执行 `RimTalkActions.ExecuteRecruit()`
4. 清理文本：`"好吧，我愿意加入你们。"`
5. RimTalk 显示清理后的文本

### 场景 2：AI 触发社交用餐

**AI 原始回复：**
```
"好主意，一起吃饭吧！{"action": "social_dining", "target": "玩家"}"
```

**补丁处理：**
1. 拦截文本
2. 提取 JSON：`{"action": "social_dining", "target": "玩家"}`
3. 执行 `RimTalkActions.ExecuteSocialDining()`
4. 清理文本：`"好主意，一起吃饭吧！"`
5. 发起者前往食物位置，邀请目标共餐

## ?? 配置

### Mod 设置

可以在 `RimTalkExpandActionsSettings` 中配置：

```csharp
// 启用详细日志
public bool enableDetailedLogging = false;

// 显示行为触发消息
public bool showActionMessages = true;
```

### 日志级别

启用详细日志后，会输出：

```
[RimTalk-ExpandActions] 对话已处理: 张三 -> 李四
[RimTalk-ExpandActions] 原始: 好吧，我愿意加入你们。{"action": "recruit", "target": "李四"}
[RimTalk-ExpandActions] 清理: 好吧，我愿意加入你们。
```

## ?? 故障排除

### 补丁未应用

检查日志：

```
[RimTalk-ExpandActions] RimTalk Mod 未安装，跳过对话桥接补丁
```

**解决方案：**
1. 确保 RimTalk Mod 已安装
2. 检查 RimTalk 在加载顺序中位于 ExpandActions 之前
3. 验证 `About.xml` 中的依赖声明

### 找不到目标方法

检查日志：

```
[RimTalk-ExpandActions] 无法找到 RimTalk 的对话处理方法，补丁失败
```

**解决方案：**
1. RimTalk 可能更新了 API
2. 在 `RimTalkBridge.cs` 中添加新的方法名回退
3. 联系 Mod 作者更新兼容性

### JSON 未被处理

检查日志：

```
[RimTalk-ExpandActions] RimTalkBridge.Prefix 执行失败: ...
```

**可能原因：**
1. JSON 格式错误
2. 参数类型不匹配
3. 目标 Pawn 无效

## ?? 开发者注意事项

### 添加新的目标方法

如果 RimTalk 更新了方法名，在 `Prepare()` 中添加回退：

```csharp
if (targetMethod == null)
{
    targetMethod = AccessTools.Method(rimTalkServiceType, "NewMethodName");
}
```

### 修改参数处理

如果 RimTalk 改变了方法签名，更新 `Prefix()` 的参数：

```csharp
// 原始：ConsumeTalk(Pawn speaker, Pawn listener, string text)
static void Prefix(object __0, object __1, ref object __2)

// 如果改为：ProcessDialogue(DialogueContext context)
static void Prefix(object __0)
{
    var context = __0 as DialogueContext;
    // 处理 context.Text
}
```

## ? 测试清单

- [ ] RimTalk 已安装且正常工作
- [ ] ExpandActions 在 RimTalk 之后加载
- [ ] Harmony 补丁已成功应用
- [ ] AI 回复中包含 JSON 指令
- [ ] JSON 指令被正确解析和执行
- [ ] 对话气泡中不显示 JSON
- [ ] 日志中没有错误信息

## ?? 相关文件

- `Source/Patches/RimTalkBridge.cs` - 主补丁文件
- `Source/Memory/AIResponsePostProcessor.cs` - JSON 处理器
- `Source/Memory/Actions/RimTalkActions.cs` - 行为执行器
- `About/About.xml` - Mod 依赖声明

## ?? API 参考

### `RimTalkBridge.IsRimTalkAvailable`

```csharp
public static bool IsRimTalkAvailable { get; }
```

检查 RimTalk 是否可用。

**示例：**
```csharp
if (RimTalkBridge.IsRimTalkAvailable)
{
    Log.Message("RimTalk 已加载");
}
```

## ?? 总结

RimTalkBridge 实现了完全透明的 AI 指令处理：

- ? 无硬依赖（反射查找）
- ? 容错机制（找不到 RimTalk 不会报错）
- ? 自动清理 JSON（用户体验友好）
- ? 详细日志（调试方便）
- ? 可扩展（支持添加新方法）

现在玩家可以直接通过 AI 对话触发各种游戏行为，无需手动操作！??
