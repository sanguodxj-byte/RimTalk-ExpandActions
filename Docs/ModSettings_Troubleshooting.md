# Mod 设置界面错误 - 故障排除指南

## ?? 错误信息

```
Exception filling window for RimWorld.Dialog_ModSettings: 
System.MissingMethodException: Method not found: 
UnityEngine.Rect Verse.Listing_Standard.Label(string,single,string)
[Ref 100548AE] Duplicate stacktrace, see ref for original
```

## ? 状态检查

### DLL 版本
- **工作区**: 82,432 bytes (2025/12/14 15:52:56)
- **游戏目录**: 82,432 bytes (2025/12/14 15:52:56)
- **状态**: ? 已同步

### 修复历史
- **提交 61267c2** (2025/12/14 14:51): 修复 `Listing_Standard.Label` 方法调用
- **提交 690effc** (2025/12/14 16:01): 增强 `ExecuteRecruit` 方法

## ?? 解决方案

### 方案 1: 清除 RimWorld 缓存（推荐）

RimWorld 可能缓存了旧的 DLL 副本。

```powershell
# 1. 关闭 RimWorld（如果正在运行）
Get-Process -Name "RimWorldWin64" -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. 删除 Mod 缓存
Remove-Item "D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions" -Recurse -Force -ErrorAction SilentlyContinue

# 3. 重新部署
cd "C:\Users\Administrator\Desktop\rim mod\RimTalk-ExpandActions"
.\Deploy.ps1

# 4. 启动游戏
Start-Process "D:\steam\steamapps\common\RimWorld\RimWorldWin64.exe"
```

### 方案 2: 验证 DLL 内容

检查 DLL 是否包含正确的代码：

```powershell
# 使用 ILSpy 或 dnSpy 反编译 DLL
# 查找 RimTalkExpandActionsMod.DoSettingsWindowContents 方法
# 应该看到：
# listingStandard.Label("文本");  // ? 只有一个参数
# 而不是：
# listingStandard.Label("文本", -1f, "提示");  // ? 三个参数
```

### 方案 3: 强制重新编译

确保使用最新的源代码编译：

```powershell
cd "C:\Users\Administrator\Desktop\rim mod\RimTalk-ExpandActions"

# 清理旧的构建文件
Remove-Item -Recurse -Force "Bin", "obj" -ErrorAction SilentlyContinue

# 重新编译
msbuild RimTalkExpandActions.csproj /t:Rebuild /p:Configuration=Debug

# 检查 DLL 时间戳
Get-Item "Bin\Debug\RimTalkExpandActions.dll" | Select-Object LastWriteTime

# 部署
.\Deploy.ps1
```

## ?? 诊断步骤

### 1. 确认 DLL 已加载

在 RimWorld 控制台（F12）中运行：

```csharp
// 检查 Mod 是否加载
var mod = LoadedModManager.RunningMods.FirstOrDefault(m => m.PackageId == "sanguo.rimtalk.expandactions");
if (mod != null)
    Log.Message($"Mod 已加载: {mod.Name}");
else
    Log.Error("Mod 未加载！");

// 检查类型是否存在
var type = typeof(RimTalkExpandActions.RimTalkExpandActionsMod);
Log.Message($"Mod 类型: {type.FullName}");
Log.Message($"程序集: {type.Assembly.Location}");
Log.Message($"程序集时间: {System.IO.File.GetLastWriteTime(type.Assembly.Location)}");
```

### 2. 检查方法签名

```csharp
// 获取 DoSettingsWindowContents 方法
var method = typeof(RimTalkExpandActions.RimTalkExpandActionsMod)
    .GetMethod("DoSettingsWindowContents", 
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

if (method != null)
{
    Log.Message($"方法存在: {method.Name}");
    Log.Message($"参数: {string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))}");
}
```

### 3. 检查 Listing_Standard.Label 方法

```csharp
// 检查 RimWorld API 版本
var labelMethods = typeof(Verse.Listing_Standard)
    .GetMethods()
    .Where(m => m.Name == "Label")
    .ToList();

Log.Message($"Label 方法数量: {labelMethods.Count}");
foreach (var m in labelMethods)
{
    var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Log.Message($"  - {m.Name}({pars})");
}
```

## ?? 正确的代码

### RimTalkExpandActionsMod.cs - DoSettingsWindowContents 方法

```csharp
public override void DoSettingsWindowContents(Rect inRect)
{
    Listing_Standard listingStandard = new Listing_Standard();
    listingStandard.Begin(inRect);
    
    // ? RimWorld 1.6 正确用法 - 只有一个参数
    listingStandard.Label("RimTalk-ExpandActions v1.1.0");
    listingStandard.Gap();
    listingStandard.Label("7种行为系统已启用（招募/投降/恋爱/灵感/休息/赠送/用餐）");
    listingStandard.Label("所有行为默认100%成功率");
    listingStandard.Label("自动注入规则功能已启用");
    listingStandard.Gap();
    listingStandard.Label("详细设置请编辑配置文件或通过代码调用。");
    
    listingStandard.End();
}
```

## ?? 常见问题

### Q1: 为什么 DLL 已更新但仍报错？

**A:** RimWorld 可能缓存了旧的 DLL。解决方法：
1. 完全关闭 RimWorld
2. 删除 Mod 文件夹
3. 重新部署
4. 启动游戏

### Q2: 如何确认 DLL 版本？

**A:** 检查文件修改时间和大小：
```powershell
Get-Item "D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions\1.6\Assemblies\RimTalkExpandActions.dll" | 
    Select-Object Length, LastWriteTime
```

期望值：
- **大小**: 82,432 bytes
- **时间**: 2025/12/14 15:52:56 或更新

### Q3: 为什么反编译 DLL 仍看到旧代码？

**A:** 可能是：
1. 反编译工具缓存了旧版本
2. DLL 没有正确部署
3. 查看了错误的 DLL 文件

解决：关闭反编译工具，重新打开正确的 DLL 文件

## ?? 快速测试

### 测试脚本

在 RimWorld 开发者控制台中运行：

```csharp
// 测试 Mod 设置
var mod = LoadedModManager.RunningModsListForReading
    .FirstOrDefault(m => m.PackageId == "sanguo.rimtalk.expandactions");

if (mod != null && mod.SettingsCategory() != null)
{
    Log.Message($"? Mod 设置存在: {mod.SettingsCategory()}");
    
    // 尝试打开设置窗口
    Find.WindowStack.Add(new Dialog_ModSettings());
    Log.Message("? 设置窗口已打开 - 检查是否有错误");
}
else
{
    Log.Error("? Mod 设置不存在");
}
```

### 预期结果

**成功:**
```
? Mod 设置存在: RimTalk-ExpandActions
? 设置窗口已打开 - 检查是否有错误
```

**失败:**
```
Exception filling window for RimWorld.Dialog_ModSettings...
```

## ?? 相关文档

- `Docs/Fix_ModSettings_Error.md` - 原始修复文档
- `Source/RimTalkExpandActionsMod.cs` - Mod 主类
- `Docs/ExecuteRecruit_Fix.md` - 最新的招募修复

## ?? 更新历史

| 日期 | 提交 | 描述 |
|------|------|------|
| 2025/12/14 14:51 | 61267c2 | 修复 Listing_Standard.Label 调用 |
| 2025/12/14 16:01 | 690effc | 增强 ExecuteRecruit 日志 |
| 2025/12/14 16:05 | 当前 | 故障排除指南 |

---

**当前状态:** DLL 已更新，需要清除缓存并重启游戏  
**推荐操作:** 运行方案 1 的清除缓存脚本
