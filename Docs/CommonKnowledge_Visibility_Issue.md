# 调试脚本：检查常识库中的规则

## 问题分析

根据日志，规则已成功注入：
```
[RimTalk-ExpandActions] ? 成功注入规则: sys-rule-recruit
[RimTalk-ExpandActions] ? 成功注入规则: sys-rule-drop-weapon
[RimTalk-ExpandActions] ? 成功注入规则: sys-rule-romance
[RimTalk-ExpandActions] ? 成功注入规则: sys-rule-inspiration
[RimTalk-ExpandActions] ? 成功注入规则: sys-rule-rest
[RimTalk-ExpandActions] ? 成功注入规则: sys-rule-gift
[RimTalk-ExpandActions] ? 成功注入规则: sys-rule-social-dining
```

## 可能的原因

### 1. 规则标签问题

所有规则的标签都设置为：**"系统指令"**

```csharp
private const string SYSTEM_RULE_TAG = "系统指令";
```

**解决方案：**
- 检查 RimTalk-ExpandMemory 的常识库 UI
- 查找是否有"系统指令"标签的过滤器或分类
- 尝试切换到"显示所有标签"模式

### 2. 规则 ID 前缀

所有规则 ID 都以 `sys-rule-` 开头：
- `sys-rule-recruit`
- `sys-rule-drop-weapon`
- `sys-rule-romance`
- `sys-rule-inspiration`
- `sys-rule-rest`
- `sys-rule-gift`
- `sys-rule-social-dining`

这可能导致 UI 将它们识别为系统规则并隐藏。

### 3. 常识库查看方法

在 RimTalk-ExpandMemory UI 中：

1. 打开**常识库管理界面**
2. 检查是否有**标签过滤器**
3. 确保选择了**"显示全部"**或**"系统指令"**标签
4. 搜索规则 ID：`sys-rule-recruit`

### 4. 手动验证规则存在

可以通过以下方式验证：

#### 方法 A：查看存档文件

1. 打开存档文件（.rws）
2. 搜索 `sys-rule-recruit`
3. 如果找到，说明规则确实已保存

#### 方法 B：使用开发者控制台

```csharp
// 在游戏的开发者控制台中执行
var memoryManager = Find.World.GetComponent<RimTalk.Memory.MemoryManager>();
var commonKnowledge = memoryManager.CommonKnowledge;
var entry = commonKnowledge.GetEntryById("sys-rule-recruit");
Log.Message($"规则存在: {entry != null}");
if (entry != null) {
    Log.Message($"标签: {entry.tag}");
    Log.Message($"内容: {entry.content}");
}
```

## 推荐的修复方案

### 选项 1：更改标签（推荐）

修改 `CrossModRecruitRuleInjector.cs`：

```csharp
// 从
private const string SYSTEM_RULE_TAG = "系统指令";

// 改为
private const string SYSTEM_RULE_TAG = "RimTalk-ExpandActions";
// 或
private const string SYSTEM_RULE_TAG = "对话行为";
```

### 选项 2：移除标签前缀

```csharp
// 从
private const string RECRUIT_RULE_ID = "sys-rule-recruit";

// 改为
private const string RECRUIT_RULE_ID = "recruit";
```

### 选项 3：检查 ExpandMemory UI

1. 确认 RimTalk-ExpandMemory 的版本
2. 查看常识库 UI 是否支持显示不同标签的规则
3. 可能需要更新 ExpandMemory 以支持显示系统规则

## 立即测试步骤

1. **打开常识库界面**
2. **查找标签选择器或过滤器**
3. **选择"系统指令"标签**（如果有）
4. **搜索"recruit"或"sys-rule"**

## 临时解决方案

如果确实看不到规则，但日志显示已注入成功，**规则仍然会生效**！

- AI 在生成回复时会检索这些规则
- JSON 指令仍会被处理
- 只是 UI 中看不到而已

## 验证规则是否生效

**测试方法：**

1. 与 NPC 对话
2. 提及"招募"、"加入"等关键词
3. 查看 AI 回复中是否包含 JSON 指令
4. 检查日志中是否有行为触发记录

```
[RimTalk-ExpandActions] 检测到 JSON 指令: {"action": "recruit", "target": "NPC名字"}
[RimTalk-ExpandActions] 成功通过对话招募: NPC名字
```

## 结论

**规则已成功注入**，但可能由于标签分类导致 UI 中不可见。

**两种可能：**
1. 规则正常工作，只是 UI 不显示
2. 需要更新 ExpandMemory 以支持显示"系统指令"标签的规则

**建议：**
- 先测试功能是否正常
- 如果功能正常但看不到，这是 UI 显示问题，不影响使用
- 如果需要在 UI 中可见，可以修改标签名称
