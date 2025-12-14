# RimTalk-ExpandActions - 扩展行为系统完整文档

## ?? 功能概述

本 Mod 为 RimWorld 的 RimTalk-ExpandMemory 扩展了 **6 种对话触发的动态行为**，通过 AI 对话自然触发游戏内动作。

### 支持的动作类型

1. **招募 (Recruit)** - 通过对话招募 NPC 加入派系
2. **投降 (Drop Weapon)** - NPC 丢下武器表示投降
3. **恋爱 (Romance)** - 确立或结束恋爱关系
4. **灵感 (Inspiration)** - 触发工作/战斗/交易灵感
5. **休息 (Rest)** - 强制 NPC 休息或昏迷
6. **赠送 (Gift)** - NPC 从背包中赠送物品

---

## ?? 架构设计

### 核心组件

```
RimTalkActions.cs
├── ExecuteRecruit()        → 招募 NPC
├── ExecuteDropWeapon()     → 丢弃武器
├── ExecuteRomanceChange()  → 恋爱关系变更
├── ExecuteInspiration()    → 触发灵感
├── ExecuteRest()           → 休息/昏迷
└── ExecuteGift()           → 赠送物品

AIResponsePostProcessor.cs
├── ProcessActionResponse() → 解析 AI 回复中的 JSON
├── DispatchAction()        → 分发到对应处理器
├── HandleXxxAction()       → 各动作类型的处理器
└── ExtractJsonField()      → 提取 JSON 字段值

RuleInjector.cs
└── InjectAllRules()        → 批量注入 6 条系统规则

BehaviorRuleContents.cs
└── GetAllRules()           → 返回所有规则定义
```

---

## ?? JSON 指令格式

### 1. 招募 (Recruit)
```json
{"action": "recruit", "target": "NPC名字"}
```
**示例对话**：
```
玩家: "加入我们吧，一起建设殖民地！"
NPC: "好的，我愿意加入你们！{"action": "recruit", "target": "艾莉丝"}"
```

### 2. 投降 (Drop Weapon)
```json
{"action": "drop_weapon", "target": "NPC名字"}
```
**示例对话**：
```
玩家: "放下武器，否则开枪！"
NPC: "好好好，我投降！{"action": "drop_weapon", "target": "张三"}"
```

### 3. 恋爱 (Romance)
```json
// 确立关系
{"action": "romance", "target": "NPC名字", "partner": "另一方名字", "type": "new_lover"}

// 分手
{"action": "romance", "target": "NPC名字", "partner": "另一方名字", "type": "breakup"}
```
**示例对话**：
```
玩家: "我喜欢你，做我女朋友吧！"
NPC: "我也喜欢你，我们在一起吧！{"action": "romance", "target": "艾莉丝", "partner": "玩家", "type": "new_lover"}"
```

### 4. 灵感 (Inspiration)
```json
{"action": "give_inspiration", "target": "NPC名字", "type": "类型"}
```
**类型选项**：
- `frenzy_shoot` - 射击狂热
- `frenzy_work` - 工作狂热
- `inspired_trade` - 交易灵感

**示例对话**：
```
玩家: "加油干！我相信你！"
NPC: "好！我今天一定多干活！{"action": "give_inspiration", "target": "李四", "type": "frenzy_work"}"
```

### 5. 休息 (Rest)
```json
// 正常休息
{"action": "force_rest", "target": "NPC名字", "immediate": false}

// 强制昏迷
{"action": "force_rest", "target": "NPC名字", "immediate": true}
```
**示例对话**：
```
NPC: "好累啊，我去睡一会儿。{"action": "force_rest", "target": "王五", "immediate": false}"
NPC: "我...撑不住了...{"action": "force_rest", "target": "王五", "immediate": true}"
```

### 6. 赠送 (Gift)
```json
{"action": "give_item", "target": "NPC名字", "item_keyword": "物品关键词"}
```
**示例对话**：
```
玩家: "你有药吗？我受伤了。"
NPC: "拿点药吧。{"action": "give_item", "target": "赵六", "item_keyword": "药"}"
```

---

## ?? 系统规则详细内容

### 规则 1: sys-rule-recruit
- **标签**: 系统指令
- **重要性**: 1.0
- **关键词**: 招募, 加入, 投靠, 派系, 跟我走, 收留, 收编, 归顺
- **触发场景**: 招募对话

### 规则 2: sys-rule-drop-weapon
- **标签**: 系统指令
- **重要性**: 1.0
- **关键词**: 投降, 放下武器, 认输, 别杀我, 饶命, 缴械
- **触发场景**: 威胁/投降对话

### 规则 3: sys-rule-romance
- **标签**: 系统指令
- **重要性**: 1.0
- **关键词**: 爱, 喜欢, 做我女朋友, 做我男朋友, 分手, 在一起, 表白, 恋爱
- **触发场景**: 恋爱相关对话

### 规则 4: sys-rule-inspiration
- **标签**: 系统指令
- **重要性**: 1.0
- **关键词**: 灵感, 启发, 顿悟, 加油, 鼓励, 激励, 状态
- **触发场景**: 激励对话

### 规则 5: sys-rule-rest
- **标签**: 系统指令
- **重要性**: 1.0
- **关键词**: 休息, 睡觉, 昏迷, 好困, 累了, 疲劳, 躺下
- **触发场景**: 疲劳相关对话

### 规则 6: sys-rule-gift
- **标签**: 系统指令
- **重要性**: 1.0
- **关键词**: 给你, 赠送, 礼物, 拿去, 送你, 给, 送
- **触发场景**: 赠送物品对话

---

## ?? 使用方法

### 步骤 1: 安装 Mod
1. 将本 Mod 放在 Mods 文件夹
2. 确保 RimTalk-ExpandMemory 已安装
3. 在加载顺序中将本 Mod 放在 RimTalk-ExpandMemory **之后**

### 步骤 2: 注入规则
- **自动注入**: 在 Mod 设置中启用"游戏启动时自动注入规则"
- **手动注入**: 进入 Mod 设置 → 点击"立即注入所有规则"按钮

### 步骤 3: 开始对话
与 NPC 对话时，AI 会根据对话内容自动检测并生成 JSON 指令。

---

## ?? 调试技巧

### 查看日志
所有操作都会输出日志，前缀为 `[RimTalk-ExpandActions]`：
```
[RimTalk-ExpandActions] ? 成功注入规则: sys-rule-recruit
[RimTalk-ExpandActions] 检测到招募指令: 艾莉丝
[RimTalk-ExpandActions] 成功通过对话招募: 艾莉丝
```

### 测试 JSON 解析
```csharp
var testJson = "好的！{\"action\": \"recruit\", \"target\": \"张三\"}";
if (AIResponsePostProcessor.TryParseActionJson(testJson, out string action, out string target))
{
    Log.Message($"解析成功: Action={action}, Target={target}");
}
```

### 检查规则状态
在 Mod 设置界面可以实时查看：
- ? 规则已存在于常识库
- ? 规则尚未注入

---

## ?? 高级配置

### 自定义规则内容
在 Mod 设置中可以自定义招募规则的内容（其他规则内容固定）。

### 调整重要性
使用滑块调节规则重要性（0.0 - 1.0），影响 AI 检索优先级。

### 移除规则
点击"移除招募规则"按钮可以从常识库中移除规则。

---

## ?? 实际应用场景

### 场景 1: 招募敌对袭击者
```
玩家: "你们输了，加入我们吧！"
袭击者: "好吧...我投降，跟你们走。"
→ AI 生成 JSON → 系统自动招募
```

### 场景 2: 恋爱剧情
```
殖民者A: "我喜欢你很久了..."
殖民者B: "我也是！我们在一起吧！"
→ AI 生成 JSON → 系统建立恋人关系
```

### 场景 3: 战斗激励
```
指挥官: "坚持住！瞄准射击！"
士兵: "是！我感觉状态来了！"
→ AI 生成 JSON → 触发射击狂热灵感
```

### 场景 4: 物资交换
```
玩家: "我需要药品，你有吗？"
商人: "有的，送你一些。"
→ AI 生成 JSON → NPC 从背包丢出药品
```

---

## ?? 技术细节

### JSON 解析健壮性
- 支持有无空格的 JSON 格式
- 字段缺失时不会崩溃
- 使用正则表达式灵活匹配

### 名字匹配算法
- 支持短名、全名、昵称匹配
- 忽略大小写和空格
- 双向模糊匹配

### 错误处理
- 所有方法都有 try-catch 保护
- 参数有效性全面检查
- 详细的日志输出

---

## ?? 已知问题

1. **Partner 查找**: 恋爱动作中，partner 名字必须精确匹配地图上的 Pawn
2. **物品关键词**: 赠送动作的关键词匹配基于 Label 和 DefName，可能不完全精确
3. **灵感冷却**: 如果 Pawn 已有灵感状态，无法触发新灵感

---

## ?? 更新日志

### v2.0.0 (当前版本)
- ? 新增 5 种动作类型
- ? 扩展 JSON 解析器支持复杂字段
- ? 批量规则注入系统
- ? BehaviorRuleContents 规则定义类
- ? 完整的错误处理和日志

### v1.0.0
- ? 基础招募功能
- ? 跨 Mod 反射注入

---

## ?? 贡献指南

如需添加新的动作类型：

1. 在 `RimTalkActions` 中添加新方法
2. 在 `BehaviorRuleContents` 中定义规则
3. 在 `AIResponsePostProcessor` 中添加处理器
4. 更新本文档

---

## ?? 许可证

本项目基于 MIT 许可证开源。

---

**祝您游戏愉快！**
