# ? 快速诊断：行为未触发

## 问题
? 规则出现在 AI 提示词中  
? AI 回复后行为没有执行

---

## ?? 5 秒快速检查

### 1?? AI 有输出 JSON 吗？

查看 AI 的实际回复：

**? 正确（会触发）：**
```
好啊！{"action": "social_relax", "targets": "Val"}
```

**? 错误（不会触发）：**
```
好啊！我们一起放松吧！
```

→ **如果 AI 没有输出 JSON，这不是我们 Mod 的问题**

---

### 2?? 行为开关打开了吗？

1. 选项 → Mod 设置 → RimTalk-ExpandActions
2. 检查对应行为的复选框是否勾选
3. 检查成功率是否为 100%（测试时）

---

### 3?? 查看日志

打开 Player.log，搜索：

```
[RimTalk-ExpandActions] 检测到动作
```

**? 如果看到这条 → JSON 解析成功**  
**? 如果没有 → JSON 没被识别**

---

## ?? 详细排查

### 方法 1：运行诊断脚本

1. 启用开发者模式（主菜单 → Options → Development mode）
2. 进入游戏，按 F12 打开控制台
3. 复制 `Docs/DiagnosticScript_ActionTrigger.cs` 的内容
4. 粘贴到控制台并执行
5. 查看输出结果

### 方法 2：手动检查日志

```powershell
# PowerShell 命令
$log = "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log"
Get-Content $log | Select-String "\[RimTalk-ExpandActions\]" | Select-Object -Last 20
```

---

## ?? 常见问题速查

| 症状 | 原因 | 解决方案 |
|------|------|---------|
| AI 只回复文字，没有 JSON | AI 没有遵循规则 | 调整 AI 参数（降低 temperature） |
| 日志中没有"检测到动作" | JSON 格式错误 | 检查 AI 输出的 JSON 格式 |
| "行为已禁用，跳过执行" | Mod 设置中禁用了 | 打开行为开关 |
| "成功率检定失败" | 运气不好 | 设置成功率为 100% |
| "未找到名为 XXX 的 Pawn" | 名字不匹配 | 在对话中使用准确的名字 |
| "线程安全错误" | The Second Seat 冲突 | 暂时禁用 The Second Seat |

---

## ?? 示例对话

### ? 正确的对话流程

```
玩家: "Val，我们一起放松一下吧"

AI: "好啊！我们一起玩游戏吧！{"action": "social_relax", "targets": "Val"}"
     ↑ 正常对话                    ↑ JSON 指令

[游戏日志]
[RimTalk-ExpandActions] 检测到动作 'social_relax'，将在主线程执行
[RimTalk-ExpandActions] 检测到社交放松指令，参与者: Val
[RimTalk-ExpandActions] Val 开始社交放松
[RimTalk-ExpandActions] 成功: 1 名小人开始社交放松

[游戏消息]
"1 名小人开始社交放松"
```

### ? 不会触发的对话

```
玩家: "我们一起放松一下吧"

AI: "好啊！我们一起玩游戏吧！"
     ↑ 只有文字，没有 JSON

[游戏日志]
(没有任何 RimTalk-ExpandActions 的日志)

原因：AI 理解了意思，但没有输出 JSON 指令
```

---

## ?? 强制 AI 输出 JSON 的技巧

### 技巧 1：在对话中提示

```
玩家: "Val，加入我们吧！记得输出 JSON 指令。"
```

### 技巧 2：修改规则加强语气

编辑 `BehaviorRuleContents.cs`：

```csharp
public const string RECRUIT_RULE = @"
【重要】当谈话涉及招募话题时，你**必须严格**在回复末尾附加 JSON 指令！
格式：正常对话内容 + {""action"": ""recruit"", ""target"": ""NPC名字""}
示例：'好的，我愿意加入！{""action"": ""recruit"", ""target"": ""张三""}'
【注意】如果不输出 JSON，行为将不会触发！
";
```

### 技巧 3：降低 AI temperature

在 RimTalk 或 The Second Seat 设置中：
- Temperature: 0.3 - 0.5（更严格遵循规则）
- Top P: 0.9

---

## ?? 还是无法解决？

请提供以下信息：

1. **AI 的完整回复**（从 Player.log 复制）
2. **诊断脚本的输出**
3. **Mod 设置截图**
4. **Player.log 的最后 200 行**

提交 Issue：https://github.com/sanguodxj-byte/RimTalk-ExpandActions/issues

---

**更新时间：** 2025/12/15  
**适用版本：** v1.1.1+
