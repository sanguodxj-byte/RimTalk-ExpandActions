# 验证修复 - 在 RimWorld F12 控制台中运行

## 步骤 1: 检查 DLL 版本

```csharp
var asm = System.AppDomain.CurrentDomain.GetAssemblies()
    .FirstOrDefault(a => a.GetName().Name == "RimTalkExpandActions");
    
if (asm != null)
{
    var location = asm.Location;
    var fileTime = System.IO.File.GetLastWriteTime(location);
    Log.Message($"? DLL 位置: {location}");
    Log.Message($"? DLL 时间: {fileTime}");
    Log.Message($"? DLL 大小: {new System.IO.FileInfo(location).Length} bytes");
}
else
{
    Log.Error("? DLL 未加载");
}
```

**期望输出：**
```
? DLL 位置: D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions\1.6\Assemblies\RimTalkExpandActions.dll
? DLL 时间: 2025/12/14 16:10:03
? DLL 大小: 82432 bytes
```

## 步骤 2: 检查方法签名

```csharp
var type = System.Type.GetType("RimTalkExpandActions.RimTalkExpandActionsMod, RimTalkExpandActions");
if (type != null)
{
    var method = type.GetMethod("DoSettingsWindowContents");
    if (method != null)
    {
        Log.Message($"? 方法存在: {method.Name}");
        
        // 检查方法体中的调用
        var methodBody = method.GetMethodBody();
        Log.Message($"? 方法体大小: {methodBody?.GetILAsByteArray()?.Length ?? 0} bytes");
    }
}
```

## 步骤 3: 测试设置界面

```csharp
// 尝试打开 Mod 设置
try
{
    Find.WindowStack.Add(new Dialog_ModSettings());
    Log.Message("? 设置窗口已打开 - 检查界面");
}
catch (System.Exception ex)
{
    Log.Error($"? 打开设置失败: {ex.Message}");
}
```

## 步骤 4: 验证 Listing_Standard.Label 方法

```csharp
var labelMethods = typeof(Verse.Listing_Standard)
    .GetMethods()
    .Where(m => m.Name == "Label")
    .ToArray();

Log.Message($"Label 方法数量: {labelMethods.Length}");
foreach (var m in labelMethods)
{
    var parameters = m.GetParameters();
    var paramStr = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Log.Message($"  - {m.Name}({paramStr})");
}
```

**RimWorld 1.6 期望输出：**
```
Label 方法数量: 1
  - Label(String label)
```

**如果看到 3 个参数的 Label 方法，说明是旧版本 RimWorld API**

## 预期结果

### ? 成功（修复生效）
```
? DLL 位置: ...
? DLL 时间: 2025/12/14 16:10:03
? DLL 大小: 82432 bytes
? 方法存在: DoSettingsWindowContents
? 设置窗口已打开 - 检查界面
```

**并且在设置界面中看到：**
```
RimTalk-ExpandActions v1.1.0

7种行为系统已启用（招募/投降/恋爱/灵感/休息/赠送/用餐）
所有行为默认100%成功率
自动注入规则功能已启用

详细设置请编辑配置文件或通过代码调用。
```

### ? 失败（仍有问题）

如果仍然看到：
```
Exception filling window for RimWorld.Dialog_ModSettings: 
System.MissingMethodException: Method not found: 
UnityEngine.Rect Verse.Listing_Standard.Label(string,single,string)
```

**可能原因：**
1. RimWorld 缓存了旧 DLL（需要完全删除 Mod 文件夹后重启）
2. 使用了错误的 RimWorld 版本（确认是 1.6）
3. 其他 Mod 冲突（尝试禁用其他 Mod）

## 故障排除

### 方法 1: 完全清理并重新安装

```powershell
# 1. 关闭 RimWorld
Get-Process -Name "RimWorldWin64" | Stop-Process -Force

# 2. 删除所有 Mod 缓存
Remove-Item "D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions" -Recurse -Force

# 3. 删除 RimWorld 的 Mod 配置缓存
Remove-Item "$env:LOCALAPPDATA\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml" -Force -ErrorAction SilentlyContinue

# 4. 重新部署
cd "C:\Users\Administrator\Desktop\rim mod\RimTalk-ExpandActions"
.\Deploy.ps1

# 5. 启动游戏
Start-Process "D:\steam\steamapps\common\RimWorld\RimWorldWin64.exe"
```

### 方法 2: 验证 RimWorld 版本

在控制台运行：
```csharp
Log.Message($"RimWorld 版本: {VersionControl.CurrentVersionStringWithRev}");
```

应该输出类似：
```
RimWorld 版本: 1.6.xxxx
```

### 方法 3: 禁用所有其他 Mod

1. 在 Mod 菜单中，禁用除了以下 Mod 之外的所有 Mod：
   - Harmony
   - Core
   - RimTalk
   - RimTalk-ExpandMemory
   - RimTalk-ExpandActions

2. 重启游戏

3. 测试设置界面

---

**当前 DLL 信息：**
- **大小:** 82,432 bytes
- **时间:** 2025/12/14 16:10:03
- **状态:** ? 已部署到游戏目录

**下一步：**
1. 启动 RimWorld
2. 按 F12 打开控制台
3. 运行上述验证脚本
4. 报告结果
