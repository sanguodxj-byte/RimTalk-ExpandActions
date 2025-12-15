# ?? 常识库注入验证指南

## ?? 问题已修复

| 问题 | 状态 |
|------|------|
| **Mod 设置错误** | ? 已修复 (RimWorld 1.6 API) |
| **招募静默失败** | ? 已修复 (增强日志) |
| **常识库注入失败** | ? 已修复 (移除冲突类) |

**当前 DLL:** 75,776 bytes (2025/12/15 08:53:09)

## ?? 验证步骤

### 第 1 步：完全清理并重启

```powershell
# 1. 关闭 RimWorld
Get-Process -Name "RimWorldWin64" -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. 清理 Mod 缓存
Remove-Item "D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions" -Recurse -Force

# 3. 重新部署
cd "C:\Users\Administrator\Desktop\rim mod\RimTalk-ExpandActions"
.\Deploy.ps1

# 4. 启动游戏
Start-Process "D:\steam\steamapps\common\RimWorld\RimWorldWin64.exe"
```

### 第 2 步：检查启动日志

启动游戏后，查看日志文件：
```
%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
```

**应该看到（关键日志）：**
```
[RimTalk-ExpandActions] Mod 已加载
[RimTalk-ExpandActions] Harmony 补丁已应用
[RimTalk-ExpandActions] 自动注入模式已启用，尝试注入所有行为规则...

? [RimTalk-ExpandActions] 找到类型 RimTalk.Memory.MemoryManager 在程序集 RimTalk-ExpandMemory
? [RimTalk-ExpandActions] ? 成功注入规则: expand-action-recruit
? [RimTalk-ExpandActions] ? 成功注入规则: expand-action-drop-weapon
? [RimTalk-ExpandActions] ? 成功注入规则: expand-action-romance
? [RimTalk-ExpandActions] ? 成功注入规则: expand-action-inspiration
? [RimTalk-ExpandActions] ? 成功注入规则: expand-action-rest
? [RimTalk-ExpandActions] ? 成功注入规则: expand-action-gift
? [RimTalk-ExpandActions] ? 成功注入规则: expand-action-social-dining

[RimTalk-ExpandActions] 规则注入完成: 成功 7 条, 失败 0 条
```

**重点：** 必须看到 **"找到类型 ... 在程序集 RimTalk-ExpandMemory"** 这行日志！

### 第 3 步：验证 Mod 设置界面

1. 进入游戏主菜单
2. 点击 **选项** → **Mod 设置**
3. 找到 **RimTalk-ExpandActions**
4. 点击打开

**预期结果：**
```
RimTalk-ExpandActions v1.1.0

7种行为系统已启用（招募/投降/恋爱/灵感/休息/赠送/用餐）
所有行为默认100%成功率
自动注入规则功能已启用

详细设置请编辑配置文件或通过代码调用。
```

**? 不应该看到：**
```
Exception filling window for RimWorld.Dialog_ModSettings...
```

### 第 4 步：检查常识库（最重要！）

#### 方法 A: 使用开发者控制台（推荐）

按 **F12** 打开控制台，运行以下脚本：

```csharp
// 检查规则是否成功注入到 RimTalk-ExpandMemory
var memType = System.Type.GetType("RimTalk.Memory.MemoryManager, RimTalk-ExpandMemory");
if (memType != null)
{
    var getMethod = memType.GetMethod("GetCommonKnowledge", 
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
    
    if (getMethod != null)
    {
        var commonKnowledge = getMethod.Invoke(null, null);
        var getAllMethod = commonKnowledge.GetType().GetMethod("GetAllEntries");
        
        if (getAllMethod != null)
        {
            var entries = getAllMethod.Invoke(commonKnowledge, null);
            var list = entries as System.Collections.IEnumerable;
            
            int expandActionCount = 0;
            foreach (var entry in list)
            {
                var idProp = entry.GetType().GetProperty("id");
                string id = idProp?.GetValue(entry) as string;
                
                if (id != null && id.StartsWith("expand-action-"))
                {
                    Log.Message($"? 找到规则: {id}");
                    expandActionCount++;
                }
            }
            
            Log.Message($"==============================");
            Log.Message($"总共找到 {expandActionCount} 条 expand-action 规则");
            Log.Message($"==============================");
            
            if (expandActionCount == 7)
            {
                Log.Message("? 所有规则都已成功注入！");
            }
            else
            {
                Log.Error($"? 期望 7 条规则，但只找到 {expandActionCount} 条！");
            }
        }
    }
}
else
{
    Log.Error("? 未找到 RimTalk-ExpandMemory！请确认已安装并启用。");
}
```

**预期输出：**
```
? 找到规则: expand-action-recruit
? 找到规则: expand-action-drop-weapon
? 找到规则: expand-action-romance
? 找到规则: expand-action-inspiration
? 找到规则: expand-action-rest
? 找到规则: expand-action-gift
? 找到规则: expand-action-social-dining
==============================
总共找到 7 条 expand-action 规则
==============================
? 所有规则都已成功注入！
```

#### 方法 B: 检查 RimTalk-ExpandMemory 界面

1. 进入游戏
2. 打开 **Mod 设置** → **RimTalk-ExpandMemory**
3. 查找 **常识库管理** 或 **规则列表** 选项
4. 应该看到以下规则：
   - `expand-action-recruit` (招募)
   - `expand-action-drop-weapon` (投降)
   - `expand-action-romance` (恋爱)
   - `expand-action-inspiration` (灵感)
   - `expand-action-rest` (休息)
   - `expand-action-gift` (赠送)
   - `expand-action-social-dining` (社交用餐)

### 第 5 步：实战测试

#### 测试 1: 社交用餐

1. 进入游戏，加载存档
2. 选择两个殖民者
3. 用开发者控制台触发对话：

```csharp
// 假设选中了两个 Pawn
var pawns = Find.Selector.SelectedPawns.ToList();
if (pawns.Count >= 2)
{
    var p1 = pawns[0];
    var p2 = pawns[1];
    
    // 手动触发社交用餐（测试代码路径）
    RimTalkExpandActions.Memory.Actions.RimTalkActions.ExecuteSocialDining(p1, p2);
    
    Log.Message($"? 触发 {p1.Name.ToStringShort} 和 {p2.Name.ToStringShort} 的社交用餐");
}
```

**预期结果：**
- ? 两人走向食物
- ? 找到餐桌
- ? 开始一起吃饭
- ? 获得心情加成 (+3)

#### 测试 2: 对话招募（需要 AI）

1. 找一个访客或囚犯
2. 与其对话，说："加入我们吧"
3. 如果 AI 同意，应该输出：
```
"好的，我愿意加入你们！{\"action\": \"recruit\", \"target\": \"NPC名字\"}"
```

**预期结果：**
- ? JSON 被 AIResponsePostProcessor 解析
- ? 调用 ExecuteRecruit
- ? NPC 加入玩家派系
- ? 收到招募成功信件

#### 测试 3: 日志详细模式

在 Mod 设置中启用 **"启用详细日志"**，然后触发任何行为。

**应该看到：**
```
[RimTalk-ExpandActions] 对话已处理: 张三
[RimTalk-ExpandActions] 原始: 好的，我愿意加入你们！{"action": "recruit", "target": "张三"}
[RimTalk-ExpandActions] 清理: 好的，我愿意加入你们！
[RimTalk-ExpandActions] 解析到 JSON 指令: {"action": "recruit", "target": "张三"}
[RimTalk-ExpandActions] 执行动作: recruit
[RimTalk-ExpandActions] 尝试通过对话招募: 张三. 旧派系: 海盗派系
[RimTalk-ExpandActions] SetFaction 调用完成。当前派系: Player
[RimTalk-ExpandActions] 成功移除旧 Lord 逻辑。
[RimTalk-ExpandActions] 清除 张三 的客人/囚犯状态。
[RimTalk-ExpandActions] ? 成功通过对话招募: 张三
```

## ?? 故障排除

### 问题：日志中没有"找到类型"消息

**原因：** RimTalk-ExpandMemory 未正确加载

**检查：**
1. 确认 Mod 列表中有 **RimTalk-ExpandMemory**
2. 确认已启用
3. 检查加载顺序：
```
1. Harmony ?
2. Core ?
3. RimTalk ?
4. RimTalk-ExpandMemory ? ← 必须在这里
5. RimTalk-ExpandActions ?
```

### 问题：找到的规则少于 7 条

**原因：** 某些规则注入失败

**解决：**
1. 查看日志中的 "? 注入规则失败" 消息
2. 检查 RimTalk-ExpandMemory 版本是否兼容
3. 尝试手动重新注入（开发者控制台）：
```csharp
RimTalkExpandActions.Memory.Utils.RuleInjector.InjectAllRules();
```

### 问题：规则存在但 AI 不触发

**可能原因：**
1. AI 没有检索到规则（关键词不匹配）
2. 对话内容不符合触发条件
3. RimTalk-ExpandMemory 的 AI 设置有问题

**检查：**
- 查看 RimTalk-ExpandMemory 的 AI 提示词设置
- 确认规则的重要性（importance）足够高
- 尝试更明确的关键词触发

## ? 验证清单

完成以下所有项目，确认修复成功：

- [ ] **日志检查**
  - [ ] 看到 "找到类型 ... 在程序集 RimTalk-ExpandMemory"
  - [ ] 看到 "成功注入规则" x7
  - [ ] 规则注入完成: 成功 7 条, 失败 0 条

- [ ] **Mod 设置**
  - [ ] 打开 RimTalk-ExpandActions 设置无错误
  - [ ] 显示正确的版本和信息

- [ ] **常识库**
  - [ ] 控制台脚本找到 7 条规则
  - [ ] RimTalk-ExpandMemory 界面中可见规则

- [ ] **功能测试**
  - [ ] 社交用餐可以触发
  - [ ] 招募可以执行（如果有 NPC）
  - [ ] 详细日志显示完整执行过程

## ?? 相关文档

- `Docs/CommonKnowledge_Fix.md` - 常识库修复详情
- `Docs/FINAL_FIX_RimWorld16_API.md` - RimWorld 1.6 API 修复
- `Docs/ExecuteRecruit_Fix.md` - 招募增强日志
- `QUICK_REFERENCE.md` - 快速参考

---

**修复版本:** 75,776 bytes (2025/12/15 08:53:09)  
**提交:** ea14dbe  
**状态:** ? 所有问题已修复  
**准备就绪！启动游戏验证吧！** ??
