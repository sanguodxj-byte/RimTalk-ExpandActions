# ?? RimTalk-ExpandActions 使用指南

## ?? 重要说明

**常识库是存档级别的数据！**

- ? **不会**在游戏启动时自动注入
- ? **需要**在加载存档后手动注入
- ? 注入后规则**永久保存**在该存档中
- ?? 每次加载新存档都需要重新注入

---

## ?? 使用步骤

### 1?? 安装前置 Mod

确保已安装并启用：
- ? **Harmony** (必需)
- ? **RimTalk** (必需)
- ? **RimTalk-ExpandMemory** (必需，PackageId: `sanguo.rimtalk.expandmemory`)

### 2?? 加载顺序

在 Mod 列表中确保正确的加载顺序：
```
1. Harmony
2. Core (游戏核心)
3. RimTalk
4. RimTalk-ExpandMemory  ← 必须在这里
5. RimTalk-ExpandActions  ← 最后加载
```

### 3?? 加载或创建存档

1. 启动 RimWorld
2. 加载已有存档 **或** 创建新游戏
3. 等待游戏完全加载（进入游戏界面）

### 4?? 打开 Mod 设置

游戏内按 **ESC** → 选项 → Mod 设置 → 找到 **RimTalk-ExpandActions**

### 5?? 检查状态

在设置界面中，找到 **"操作"** 部分，查看：
```
常识库状态: ? RimTalk-ExpandMemory 已就绪
```

**可能的状态：**

| 状态 | 含义 | 解决方案 |
|------|------|----------|
| ? RimTalk-ExpandMemory 已就绪 | 一切正常 | 可以注入 |
| ? 未加载游戏存档 | 没有活动存档 | 加载或创建游戏 |
| ? 未找到 RimTalk-ExpandMemory | Mod 未安装或未启用 | 安装并启用 ExpandMemory |
| ? MemoryManager 未初始化 | ExpandMemory 初始化失败 | 检查 Mod 加载顺序 |

### 6?? 查看将要注入的规则

点击 **"查看将要注入的规则"** 按钮，会显示：

```
将注入以下 7 种行为规则：

1. 招募系统
   通过对话招募 NPC 到殖民地

2. 社交用餐
   邀请他人共进晚餐，增进关系

3. 投降系统
   让敌人放下武器投降

4. 恋爱关系
   建立或结束恋人关系

5. 灵感触发
   给予角色工作/战斗/交易灵感

6. 强制休息
   让角色去休息或陷入昏迷

7. 赠送物品
   从背包中赠送物品给他人
```

### 7?? 注入规则到当前存档

点击 **"注入规则到当前存档的常识库"** 按钮

**成功提示：**
```
RimTalk-ExpandActions: 已成功导入 7 条行为规则到当前存档！
```

**日志中会显示：**
```
[RimTalk-ExpandActions] ━━━━━ 手动注入常识库 ━━━━━
[RimTalk-ExpandActions] ? 当前有活动的游戏存档
[RimTalk-ExpandActions] ? 找到 MemoryManager: RimTalk.Memory.MemoryManager
[RimTalk-ExpandActions] ? 找到当前存档的 MemoryManager 实例
[RimTalk-ExpandActions] ? 找到 CommonKnowledge 实例
[RimTalk-ExpandActions] ? 找到 ImportFromExternalMod 方法
[RimTalk-ExpandActions] ??? 成功导入 7 条规则到当前存档 ???
```

---

## ?? 使用行为功能

### 招募系统

**玩家说：** "加入我们吧，一起建设殖民地"

**AI 可能回复：**
```
好啊，我也厌倦了到处流浪了。
{"action": "recruit", "target": "流浪者艾莉丝"}
```

→ 艾莉丝会加入你的殖民地！

### 社交用餐

**玩家说：** "一起吃个饭吧"

**AI 可能回复：**
```
好主意！我也饿了。
{"action": "social_dining", "target": "玩家角色"}
```

→ 双方会一起吃饭，获得心情加成！

### 投降系统

**玩家说：** "放下武器，否则开枪！"

**AI 可能回复：**
```
好好好，我投降！别开枪！
{"action": "drop_weapon", "target": "海盗约翰"}
```

→ 约翰会放下武器投降！

### 其他行为

- **恋爱关系** - "做我女朋友吧"
- **灵感触发** - "加油，你可以的！"
- **强制休息** - "去休息吧，你太累了"
- **赠送物品** - "这把枪送给你"

---

## ? 常见问题

### Q: 注入后关闭游戏，规则还在吗？

**A:** ? **是的！** 规则已永久保存在该存档中。下次加载这个存档时，规则仍然有效。

### Q: 加载其他存档需要重新注入吗？

**A:** ? **是的！** 每个存档有独立的常识库。切换存档后需要重新注入。

### Q: 可以重复注入吗？

**A:** ? **可以！** 重复注入会覆盖已有规则，适用于更新规则内容的情况。

### Q: 注入失败怎么办？

**A:** 检查以下内容：
1. 确保 RimTalk-ExpandMemory 已安装并启用
2. 确保 Mod 加载顺序正确
3. 确保当前有活动的游戏存档
4. 查看日志文件了解详细错误信息

### Q: 如何查看日志？

**A:** 日志文件位置：
```
%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
```

搜索 `[RimTalk-ExpandActions]` 查看相关日志。

### Q: 注入后 AI 还是不输出 JSON 怎么办？

**A:** 可能的原因：
1. 规则未被正确检索 - 检查对话内容是否包含关键词
2. AI 模型未理解规则 - 尝试更明确的对话
3. ExpandMemory 版本不兼容 - 更新到最新版本

---

## ?? 故障排除

### 问题 1: "未加载游戏存档"

**原因：** 在主菜单或没有活动游戏时尝试注入

**解决：** 先加载或创建游戏，再打开 Mod 设置

### 问题 2: "未找到 RimTalk-ExpandMemory"

**原因：** ExpandMemory 未安装或未启用

**解决：**
1. 在 Mod 列表中启用 RimTalk-ExpandMemory
2. 确保 PackageId 为 `sanguo.rimtalk.expandmemory`
3. 重启游戏

### 问题 3: "未找到 ImportFromExternalMod 方法"

**原因：** ExpandMemory 版本太旧

**解决：** 更新 RimTalk-ExpandMemory 到最新版本

### 问题 4: 注入成功但没有效果

**原因：** 可能的情况：
- AI 没有检索到规则
- 对话内容不匹配关键词
- ExpandMemory 配置问题

**解决：**
1. 尝试使用更明确的关键词（如 "招募"、"加入"）
2. 检查 ExpandMemory 设置
3. 查看日志确认规则是否被检索

---

## ?? 设置选项说明

### 全局设置

| 选项 | 说明 | 默认值 |
|------|------|--------|
| 显示行为触发消息 | 在游戏中显示行为触发的提示 | ? 启用 |
| 启用详细日志 | 记录详细的执行过程（调试用） | ? 禁用 |

### 行为开关

所有 7 种行为都可以单独启用或禁用：
- ? 招募系统
- ? 社交用餐
- ? 投降/丢武器
- ? 恋爱关系
- ? 灵感触发
- ? 强制休息
- ? 赠送物品

### 成功率设置

每种行为都有独立的成功率（0% - 100%），可以调整难度：
- **100%** = 必定成功
- **50%** = 一半概率成功
- **0%** = 必定失败（相当于禁用）

---

## ?? 快速开始流程

```
1. 启动 RimWorld
     ↓
2. 加载存档/创建新游戏
     ↓
3. ESC → 选项 → Mod 设置 → RimTalk-ExpandActions
     ↓
4. 检查状态: ? RimTalk-ExpandMemory 已就绪
     ↓
5. 点击 "注入规则到当前存档的常识库"
     ↓
6. 看到提示: "已成功导入 7 条行为规则"
     ↓
7. 开始游戏，与 NPC 对话测试！
```

---

## ?? 版本信息

**当前版本:** v1.1.0  
**DLL 大小:** 80,384 bytes  
**编译日期:** 2025/12/15  
**支持版本:** RimWorld 1.4 / 1.5 / 1.6

---

## ?? 提示与技巧

### 1. 批量测试规则

在开发者模式下（F12 控制台），可以手动测试：

```csharp
// 测试招募
RimTalkExpandActions.Memory.Actions.RimTalkActions.ExecuteRecruit(targetPawn, initiatorPawn);

// 测试社交用餐
RimTalkExpandActions.Memory.Actions.RimTalkActions.ExecuteSocialDining(targetPawn, initiatorPawn);
```

### 2. 查看注入的规则

在 F12 控制台运行：
```csharp
var rules = RimTalkExpandActions.Memory.Utils.ExpandMemoryKnowledgeInjector.GetRuleDescriptions();
foreach (var rule in rules)
{
    Log.Message($"{rule.Key}: {rule.Value}");
}
```

### 3. 重新注入规则

如果更新了 Mod，可以重新注入以获取最新规则：
1. 打开 Mod 设置
2. 点击 "注入规则到当前存档的常识库"
3. 选择覆盖已有规则

---

## ?? 相关链接

- **GitHub 仓库:** https://github.com/sanguodxj-byte/RimTalk-ExpandActions
- **问题反馈:** GitHub Issues
- **RimTalk 主页:** [待补充]
- **ExpandMemory 主页:** [待补充]

---

## ?? 技术支持

如果遇到问题：

1. **查看日志文件** - Player.log
2. **搜索关键词** - `[RimTalk-ExpandActions]`
3. **提交 Issue** - 附上日志相关部分
4. **描述问题** - 包括：
   - Mod 列表和加载顺序
   - 游戏版本
   - 注入时的状态提示
   - 完整的错误消息

---

**感谢使用 RimTalk-ExpandActions！**  
**Have fun! ??**
