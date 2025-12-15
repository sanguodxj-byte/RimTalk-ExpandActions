# ?? BuildContext 调试补丁使用指南

## ?? 功能说明

这个调试补丁用于**监控 RimTalk-ExpandMemory 生成的 AI Prompt**，验证常识库规则是否正确注入。

### 什么是 BuildContext？

`BuildContext` 是 RimTalk-ExpandMemory 中负责构建 AI Prompt 的核心方法。它会：
1. 收集角色信息
2. 收集对话历史
3. **从常识库中检索相关规则** ← 重点
4. 组装成完整的 Prompt 发送给 AI

### 补丁功能

- ? 自动 Hook RimTalk-ExpandMemory 的 `BuildContext` 方法
- ? 检查生成的 Prompt 中是否包含行为规则
- ? 统计检测到的规则数量
- ? 显示详细的诊断信息

## ?? 使用方法

### 1. 启动游戏并加载存档

确保以下 Mod 已启用：
- Harmony
- Core
- RimTalk
- RimTalk-ExpandMemory
- **RimTalk-ExpandActions** (你的 Mod)

### 2. 与 NPC 对话

选择一个**非殖民者**（访客、囚犯等），与其对话。

**为什么是非殖民者？**
- 补丁会跳过殖民者，避免刷屏
- 访客/囚犯更容易触发招募等行为

### 3. 查看日志

对话触发后，查看日志文件：
```
%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
```

或使用游戏内日志查看器（Mod：Dev Quick Test）

## ?? 日志示例

### 成功案例（规则已注入）

```
[RimTalk-ExpandActions] ━━━━━ Prompt 注入检查 ━━━━━
目标: 张三
Prompt 长度: 2847 字符

检查关键词:
  - 常识库标记: ? 存在
  - 招募规则: ? 存在
  - 社交用餐规则: ? 存在
  - 投降规则: ? 存在
  - 恋爱规则: ? 存在

总结: 检测到 7/7 条规则
? 所有规则都已正确注入到 Prompt 中！
━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 失败案例（规则未注入）

```
[RimTalk-ExpandActions] ━━━━━ Prompt 注入检查 ━━━━━
目标: 张三
Prompt 长度: 1245 字符

检查关键词:
  - 常识库标记: ? 不存在
  - 招募规则: ? 不存在
  - 社交用餐规则: ? 不存在
  - 投降规则: ? 不存在
  - 恋爱规则: ? 不存在

总结: 检测到 0/7 条规则
?? 警告: Prompt 中没有检测到任何 expand-action 规则！
可能原因:
  1. 常识库注入失败
  2. RimTalk-ExpandMemory 未正确检索规则
  3. BuildContext 方法未包含常识库内容
━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 部分成功（部分规则缺失）

```
[RimTalk-ExpandActions] ━━━━━ Prompt 注入检查 ━━━━━
目标: 张三
Prompt 长度: 1986 字符

检查关键词:
  - 常识库标记: ? 存在
  - 招募规则: ? 存在
  - 社交用餐规则: ? 不存在
  - 投降规则: ? 不存在
  - 恋爱规则: ? 不存在

总结: 检测到 2/7 条规则
?? 注意: 只检测到 2 条规则，期望 7 条
━━━━━━━━━━━━━━━━━━━━━━━━━━
```

## ?? 启用详细日志

如果想查看 Prompt 的实际内容，启用详细日志：

1. 打开 Mod 设置 → RimTalk-ExpandActions
2. 勾选 **"启用详细日志"**
3. 保存设置

再次对话后，日志中会显示：
```
[RimTalk-ExpandActions] 常识库片段:
World Knowledge:
- [招募,加入,投靠,派系,收留,收编,归顺|1.0] 当谈话涉及【招募、加入、投靠、派系】话题，且目标NPC在对话中明确表示同意加入玩家派系时（例如说"我愿意加入"、"好吧，我跟你走"），请务必在回复的最后附加如下JSON代码：{"action": "recruit", "target": "NPC名字"}...
```

## ?? 故障排除

### 问题 1: 补丁未生效

**症状：** 对话后没有看到任何调试日志

**原因：**
- RimTalk-ExpandMemory 未安装或未启用
- 补丁查找方法失败

**解决：**
1. 查看启动日志，搜索：
```
[RimTalk-ExpandActions] 调试补丁: 成功找到 BuildContext 方法
```

2. 如果看到：
```
[RimTalk-ExpandActions] 调试补丁: 未找到 RimTalk.Memory.MemoryManager
```
说明 RimTalk-ExpandMemory 未正确加载。

### 问题 2: Prompt 中没有规则

**症状：** 日志显示 `检测到 0/7 条规则`

**可能原因：**

1. **常识库注入失败**
   - 检查启动日志，搜索：
     ```
     [RimTalk-ExpandActions] 找到类型 RimTalk.Memory.MemoryManager 在程序集 RimTalk-ExpandMemory
     [RimTalk-ExpandActions] ? 成功注入规则: expand-action-recruit
     ```
   - 如果看到注入失败，说明跨 Mod 通信有问题

2. **RimTalk-ExpandMemory 未检索规则**
   - 规则已注入，但 AI 没有检索
   - 可能是关键词不匹配
   - 可能是规则重要性太低

3. **BuildContext 不包含常识库**
   - RimTalk-ExpandMemory 版本问题
   - 需要更新到支持常识库的版本

### 问题 3: 只检测到部分规则

**症状：** 日志显示 `检测到 2/7 条规则`

**原因：**
- 部分规则的关键词与对话内容不匹配
- AI 只检索了最相关的规则（正常行为）

**说明：**
- 这是**正常的**！AI 不会加载所有规则到 Prompt
- 只有与当前对话相关的规则才会被检索
- 例如：讨论招募时，只会检索招募规则

## ?? 检查清单

使用这个清单验证系统是否正常：

- [ ] 启动日志显示：`成功找到 BuildContext 方法`
- [ ] 启动日志显示：`规则注入完成: 成功 7 条, 失败 0 条`
- [ ] 与访客对话后，日志显示：`Prompt 注入检查`
- [ ] 常识库标记存在：`常识库标记: ? 存在`
- [ ] 至少检测到 1 条规则（根据对话内容）
- [ ] AI 回复包含 JSON 指令（例如：`{"action": "recruit", ...}`）

## ?? 测试场景

### 场景 1: 测试招募规则注入

1. 与访客对话
2. 说："加入我们吧"
3. 检查日志是否显示：`招募规则: ? 存在`
4. 检查 AI 回复是否包含：`{"action": "recruit", ...}`

### 场景 2: 测试社交用餐规则注入

1. 与殖民者对话（这次可以用殖民者）
2. 说："一起吃饭吧"
3. 启用详细日志，查看 Prompt 片段
4. 确认包含：`expand-action-social-dining`

### 场景 3: 测试多规则场景

1. 与囚犯对话
2. 说："放下武器，加入我们"
3. 检查日志应显示：
   ```
   - 招募规则: ? 存在
   - 投降规则: ? 存在
   ```

## ?? 进阶：手动检查 Prompt

如果想完全确认，可以手动检查 RimTalk-ExpandMemory 的常识库：

```csharp
// F12 控制台运行
var memType = System.Type.GetType("RimTalk.Memory.MemoryManager, RimTalk-ExpandMemory");
var getMethod = memType.GetMethod("GetCommonKnowledge", 
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
var commonKnowledge = getMethod.Invoke(null, null);
var getAllMethod = commonKnowledge.GetType().GetMethod("GetAllEntries");
var entries = getAllMethod.Invoke(commonKnowledge, null);

int expandActionCount = 0;
foreach (var entry in entries as System.Collections.IEnumerable)
{
    var idProp = entry.GetType().GetProperty("id");
    string id = idProp?.GetValue(entry) as string;
    if (id != null && id.StartsWith("expand-action-"))
    {
        Log.Message($"? 规则: {id}");
        expandActionCount++;
    }
}

Log.Message($"总共: {expandActionCount} 条 expand-action 规则");
```

## ?? 性能影响

- **最小** - 补丁只在非殖民者对话时触发
- 每次对话增加约 5-10ms 处理时间
- 不影响游戏帧率

## ?? 禁用调试补丁

如果不需要调试，可以：

1. **临时禁用：** 在 Mod 设置中关闭"启用详细日志"
2. **永久禁用：** 删除 `Source/Patches/BuildContextDebugPatch.cs` 并重新编译

---

**创建时间:** 2025/12/15  
**DLL 版本:** 81,920 bytes  
**状态:** ? 已部署，准备测试
