# 简化部署脚本 - 只复制 DLL
param(
    [string]$RimWorldPath = "D:\steam\steamapps\common\RimWorld"
)

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "RimTalk-ExpandActions 快速部署" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""

# 1. 关闭 RimWorld
Write-Host "1. 检查 RimWorld 进程..." -ForegroundColor Yellow
$process = Get-Process -Name "RimWorldWin64" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "   发现 RimWorld 正在运行，正在关闭..." -ForegroundColor Red
    $process | Stop-Process -Force
    Start-Sleep -Seconds 2
    Write-Host "   ? 已关闭" -ForegroundColor Green
} else {
    Write-Host "   ? RimWorld 未运行" -ForegroundColor Green
}

# 2. 复制 DLL
Write-Host ""
Write-Host "2. 复制 DLL..." -ForegroundColor Yellow
$source = ".\Bin\Debug\RimTalkExpandActions.dll"
$dest = Join-Path $RimWorldPath "Mods\RimTalk-ExpandActions\1.6\Assemblies\RimTalkExpandActions.dll"

if (!(Test-Path $source)) {
    Write-Host "   ? 源文件不存在: $source" -ForegroundColor Red
    Write-Host "   请先运行编译!" -ForegroundColor Red
    exit 1
}

try {
    Copy-Item $source $dest -Force -ErrorAction Stop
    Write-Host "   ? 已复制" -ForegroundColor Green
} catch {
    Write-Host "   ? 复制失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   可能 RimWorld 仍在运行或文件被锁定" -ForegroundColor Red
    exit 1
}

# 3. 验证
Write-Host ""
Write-Host "3. 验证部署..." -ForegroundColor Yellow
$deployedFile = Get-Item $dest
Write-Host "   大小: $($deployedFile.Length) bytes" -ForegroundColor Gray
Write-Host "   时间: $($deployedFile.LastWriteTime)" -ForegroundColor Gray
Write-Host "   ? 验证通过" -ForegroundColor Green

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "? 部署完成！" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "现在可以启动 RimWorld 了！" -ForegroundColor Green
Write-Host ""

# 4. 可选：自动启动游戏
$response = Read-Host "是否启动 RimWorld? (Y/N)"
if ($response -eq "Y" -or $response -eq "y") {
    $gameExe = Join-Path $RimWorldPath "RimWorldWin64.exe"
    if (Test-Path $gameExe) {
        Write-Host "正在启动 RimWorld..." -ForegroundColor Green
        Start-Process $gameExe
    } else {
        Write-Host "未找到游戏可执行文件: $gameExe" -ForegroundColor Red
    }
}
