# 快速修复脚本 - 清除缓存并重新部署

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RimTalk-ExpandActions 快速修复脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. 关闭 RimWorld
Write-Host "1. 检查并关闭 RimWorld..." -ForegroundColor Yellow
$rimworld = Get-Process -Name "RimWorldWin64" -ErrorAction SilentlyContinue
if ($rimworld) {
    Write-Host "   发现 RimWorld 进程，正在关闭..." -ForegroundColor Yellow
    $rimworld | Stop-Process -Force
    Start-Sleep -Seconds 2
    Write-Host "   ? RimWorld 已关闭" -ForegroundColor Green
} else {
    Write-Host "   ? RimWorld 未运行" -ForegroundColor Green
}

# 2. 删除旧的 Mod 文件夹
Write-Host ""
Write-Host "2. 清除旧的 Mod 缓存..." -ForegroundColor Yellow
$modPath = "D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions"
if (Test-Path $modPath) {
    Remove-Item $modPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "   ? 已删除旧文件" -ForegroundColor Green
} else {
    Write-Host "   ? 无需清理" -ForegroundColor Green
}

# 3. 重新部署
Write-Host ""
Write-Host "3. 重新部署 Mod..." -ForegroundColor Yellow
& ".\Deploy.ps1"

# 4. 验证部署
Write-Host ""
Write-Host "4. 验证部署结果..." -ForegroundColor Yellow
$dllPath = "D:\steam\steamapps\common\RimWorld\Mods\RimTalk-ExpandActions\1.6\Assemblies\RimTalkExpandActions.dll"
if (Test-Path $dllPath) {
    $dll = Get-Item $dllPath
    Write-Host "   ? DLL 已部署" -ForegroundColor Green
    Write-Host "   文件大小: $($dll.Length) bytes" -ForegroundColor Gray
    Write-Host "   修改时间: $($dll.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "   ? DLL 部署失败" -ForegroundColor Red
}

# 5. 启动游戏（可选）
Write-Host ""
Write-Host "5. 是否启动 RimWorld？ (Y/N)" -ForegroundColor Yellow
$response = Read-Host
if ($response -eq "Y" -or $response -eq "y") {
    Write-Host "   正在启动 RimWorld..." -ForegroundColor Yellow
    Start-Process "D:\steam\steamapps\common\RimWorld\RimWorldWin64.exe"
    Write-Host "   ? RimWorld 已启动" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "修复完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "下一步：" -ForegroundColor Yellow
Write-Host "1. 加载游戏" -ForegroundColor Gray
Write-Host "2. 进入 Mod 设置检查是否还有错误" -ForegroundColor Gray
Write-Host "3. 如果仍有错误，请查看日志文件" -ForegroundColor Gray
Write-Host ""
