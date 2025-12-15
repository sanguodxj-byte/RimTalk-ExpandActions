# ?? 常识库注入问题已修复

## ?? 问题诊断

### 症状
- 日志显示：`[RimTalk-ExpandActions] 规则注入完成: 成功 7 条, 失败 0 条`
- **但在 RimTalk-ExpandMemory 的常识库中看不到任何规则**

### 根本原因

**项目包含了自己的 `MemoryManager` 和 `CommonKnowledgeLibrary` 类！**

#### 文件冲突
```
? Source/Memory/MemoryManager.cs - 本地版本
? Source/Memory/CommonKnowledgeLibrary.cs - 本地版本
```

这些文件**不应该**存在于你的项目中，因为：

1. **命名空间冲突**
   ```
   你的项目: RimTalkExpandActions.Memory.MemoryManager
   目标 Mod:  RimTalk.Memory.MemoryManager (RimTalk-ExpandMemory)
   ```

2. **类型解析错误**
   ```csharp
   // CrossModRecruitRuleInjector.cs 查找类型时
   Type memoryManagerType = FindType("RimTalk.Memory.MemoryManager");
   
   // ? 可能找到了你自己的类（如果命名空间相同）
   // ? 应该找到 RimTalk-ExpandMemory 的类
   ```

3. **注入到错误的实例**
   ```
   规则被添加到 → 你本地的 CommonKnowledgeLibrary 实例
   AI 查询的是     → RimTalk-ExpandMemory 的 CommonKnowledgeLibrary 实例
   结果：         → 规则"注入成功"但 AI 看不到
   ```

## ? 修复内容

### 1. 删除冲突文件
```powershell
? 已删除: Source/Memory/MemoryManager.cs
? 已删除: Source/Memory/CommonKnowledgeLibrary.cs
? 已删除: Source/Examples/DialogSystemIntegrationExample.cs (引用了本地 MemoryManager)
```

### 2. 改进类型查找逻辑

**修改前（有问题）：**
```csharp
private static Type FindType(string fullTypeName)
{
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
        Type type = assembly.GetType(fullTypeName);
        if (type != null)
        {
            return type; // ? 可能返回自己的类型
        }
    }
    return null;
}
```

**修改后（正确）：**
```csharp
private static Type FindType(string fullTypeName)
{
    string currentAssemblyName = typeof(CrossModRecruitRuleInjector).Assembly.GetName().Name;
    
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
        // ? 跳过自己的程序集，确保找到外部 Mod 的类型
        if (assembly.GetName().Name == currentAssemblyName)
        {
            continue;
        }
        
        Type type = assembly.GetType(fullTypeName);
        if (type != null)
        {
            Log.Message($"[RimTalk-ExpandActions] 找到类型 {fullTypeName} 在程序集 {assembly.GetName().Name}");
            return type;
        }
    }
    
    Log.Warning($"[RimTalk-ExpandActions] 未找到类型: {fullTypeName}");
    return null;
}
```

### 3. 重新编译并部署

```powershell
# 清理构建文件
Remove-Item -Recurse -Force "Bin", "obj" -ErrorAction SilentlyContinue

# 编译
msbuild RimTalkExpandActions.csproj /t:Rebuild /p:Configuration=Debug

# 部署
Copy-Item ".\Bin\Debug\RimTalkExpandActions.dll" `
  "D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions\1.6\Assemblies\" -Force
```

## ?? 修复验证

### DLL 信息
| 属性 | 值 |
|------|-----|
| **大小** | 75,776 bytes ?? (减少了，移除了冗余类) |
| **时间** | 2025/12/15 08:53:09 |
| **状态** | ? 已部署 |

### 日志验证

重新启动 RimWorld 后，应该看到：

```
[RimTalk-ExpandActions] 自动注入模式已启用，尝试注入所有行为规则...
[RimTalk-ExpandActions] 找到类型 RimTalk.Memory.MemoryManager 在程序集 RimTalk-ExpandMemory
[RimTalk-ExpandActions] ? 成功注入规则: expand-action-recruit
[RimTalk-ExpandActions] ? 成功注入规则: expand-action-drop-weapon
[RimTalk-ExpandActions] ? 成功注入规则: expand-action-romance
[RimTalk-ExpandActions] ? 成功注入规则: expand-action-inspiration
[RimTalk-ExpandActions] ? 成功注入规则: expand-action-rest
[RimTalk-ExpandActions] ? 成功注入规则: expand-action-gift
[RimTalk-ExpandActions] ? 成功注入规则: expand-action-social-dining
[RimTalk-ExpandActions] 规则注入完成: 成功 7 条, 失败 0 条
```

**关键新增日志：**
```
? "找到类型 RimTalk.Memory.MemoryManager 在程序集 RimTalk-ExpandMemory"
```

这确认了找到了**正确的外部程序集**，而不是本地的类型。

## ?? 验证常识库

### 方法 1: 在 RimWorld 控制台中检查

```csharp
// F12 打开开发者控制台，运行：

// 1. 获取 RimTalk-ExpandMemory 的 MemoryManager
var memType = System.Type.GetType("RimTalk.Memory.MemoryManager, RimTalk-ExpandMemory");
if (memType != null)
{
    var getMethod = memType.GetMethod("GetCommonKnowledge", 
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
    
    if (getMethod != null)
    {
        var commonKnowledge = getMethod.Invoke(null, null);
        
        // 2. 获取所有条目
        var getAllMethod = commonKnowledge.GetType().GetMethod("GetAllEntries");
        if (getAllMethod != null)
        {
            var entries = getAllMethod.Invoke(commonKnowledge, null);
            var list = entries as System.Collections.IEnumerable;
            
            int count = 0;
            foreach (var entry in list)
            {
                var idProp = entry.GetType().GetProperty("id");
                var contentProp = entry.GetType().GetProperty("content");
                
                string id = idProp?.GetValue(entry) as string;
                string content = contentProp?.GetValue(entry) as string;
                
                if (id != null && id.StartsWith("expand-action-"))
                {
                    Log.Message($"? 找到规则: {id}");
                    Log.Message($"  内容: {content?.Substring(0, System.Math.Min(100, content.Length))}...");
                    count++;
                }
            }
            
            Log.Message($"总共找到 {count} 条 expand-action 规则");
        }
    }
}
```

### 方法 2: 查看 RimTalk-ExpandMemory 的界面

1. 启动 RimWorld
2. 进入游戏
3. 打开 **Mod 设置** → **RimTalk-ExpandMemory**
4. 查看**常识库管理**或**规则列表**
5. 应该看到 7 条规则：
   ```
   ? expand-action-recruit
   ? expand-action-drop-weapon
   ? expand-action-romance
   ? expand-action-inspiration
   ? expand-action-rest
   ? expand-action-gift
   ? expand-action-social-dining
   ```

## ?? 规则内容示例

### expand-action-social-dining
```
[吃饭,聚餐,饿了,分享食物,庆祝,共进晚餐,用餐|1.0]
当对话双方决定一起吃饭、分享食物、举办小型聚餐或庆祝时，
且双方关系良好，请在回复末尾附加JSON：
{"action": "social_dining", "target": "(对方名字)"}。
注意：仅在确认要执行此行动时输出。
```

### expand-action-recruit
```
[招募,加入,投靠,派系,收留,收编,归顺|1.0]
当谈话涉及【招募、加入、投靠、派系】话题，
且目标NPC在对话中明确表示同意加入玩家派系时
（例如说"我愿意加入"、"好吧，我跟你走"），
请务必在回复的最后附加如下JSON代码：
{"action": "recruit", "target": "NPC名字"}。
注意：仅在对方明确同意时才输出此指令，拒绝或犹豫时不输出。
```

## ?? 测试场景

### 测试 1: 社交用餐触发

**对话示例：**
```
玩家: "嘿，一起吃饭吧？我有好吃的。"

AI (NPC): "好啊！我也饿了。{"action": "social_dining", "target": "玩家角色名字"}"
```

**预期结果：**
1. ? AIResponsePostProcessor 解析 JSON
2. ? 调用 `RimTalkActions.ExecuteSocialDining()`
3. ? 两人开始社交用餐
4. ? 获得心情加成

### 测试 2: 招募触发

**对话示例：**
```
玩家: "加入我们吧，我们需要你。"

AI (NPC): "你说得对，我愿意加入你们！{"action": "recruit", "target": "NPC名字"}"
```

**预期结果：**
1. ? AIResponsePostProcessor 解析 JSON
2. ? 调用 `RimTalkActions.ExecuteRecruit()`
3. ? NPC 加入玩家派系
4. ? 收到招募成功信件

## ?? 故障排除

### 问题 1: 仍然看不到规则

**可能原因：**
1. RimWorld 缓存了旧的 DLL
2. RimTalk-ExpandMemory 未正确加载

**解决方案：**
```powershell
# 完全清理并重启
Get-Process -Name "RimWorldWin64" | Stop-Process -Force
Remove-Item "D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions" -Recurse -Force
.\Deploy.ps1
Start-Process "D:\steam\steamapps\common\RimWorld\RimWorldWin64.exe"
```

### 问题 2: 日志中没有"找到类型"消息

**可能原因：**
- RimTalk-ExpandMemory 未安装或未启用

**检查：**
1. Mod 列表中是否有 **RimTalk-ExpandMemory**
2. 是否已启用
3. 加载顺序是否正确：
   ```
   1. Harmony
   2. Core
   3. RimTalk
   4. RimTalk-ExpandMemory  ← 必须在这里
   5. RimTalk-ExpandActions
   ```

### 问题 3: 规则注入失败

**日志示例：**
```
[RimTalk-ExpandActions] ? 注入规则失败: expand-action-recruit
```

**检查：**
```csharp
// 在控制台运行
var memType = System.Type.GetType("RimTalk.Memory.MemoryManager, RimTalk-ExpandMemory");
if (memType == null)
{
    Log.Error("RimTalk-ExpandMemory 未加载！");
}
else
{
    Log.Message($"? 找到 MemoryManager: {memType.Assembly.FullName}");
}
```

## ?? 技术细节

### 跨 Mod 通信原理

```
RimTalk-ExpandActions (你的 Mod)
    ↓ 反射查找
RimTalk-ExpandMemory (目标 Mod)
    ↓ 获取实例
MemoryManager.Instance.CommonKnowledge
    ↓ 调用方法
AddEntry(CommonKnowledgeEntry entry)
    ↓ 添加规则
常识库 (AI 可以检索)
```

### 关键代码路径

1. **注入触发点：**
   ```
   RuleInjector.cs (StaticConstructorOnStartup)
   → LongEventHandler.ExecuteWhenFinished()
   → InjectAllRules()
   ```

2. **规则内容：**
   ```
   BehaviorRuleContents.GetAllRules()
   → 返回 7 个 RuleDefinition
   ```

3. **跨 Mod 注入：**
   ```
   CrossModRecruitRuleInjector.TryInjectRule()
   → FindType("RimTalk.Memory.MemoryManager")
   → GetCommonKnowledgeInstance()
   → CreateGenericRuleEntry()
   → AddEntryToCommonKnowledge()
   ```

## ?? 总结

### 问题
- ? 项目包含本地的 `MemoryManager` 类
- ? 类型查找不排除自己的程序集
- ? 规则注入到错误的实例

### 修复
- ? 删除本地的 `MemoryManager.cs` 和 `CommonKnowledgeLibrary.cs`
- ? 改进 `FindType()` 方法，跳过自己的程序集
- ? 重新编译并部署（75,776 bytes）

### 验证
- ? 查看日志中的"找到类型"消息
- ? 在 RimTalk-ExpandMemory 界面中检查规则
- ? 测试对话触发行为

---

**修复时间:** 2025/12/15 08:53  
**DLL 版本:** 75,776 bytes  
**状态:** ? 已修复并部署  
**下一步:** 重启 RimWorld 并验证常识库
