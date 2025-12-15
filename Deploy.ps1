# RimTalk-ExpandActions 部署脚本
# 自动将 Mod 文件复制到 RimWorld Mods 目录

param(
    [string]$RimWorldPath = "D:\steam\steamapps\common\RimWorld"
)

$ErrorActionPreference = "Stop"

# 项目根目录
$ProjectRoot = $PSScriptRoot
$ModName = "RimTalk-ExpandActions"
$TargetPath = Join-Path $RimWorldPath "Mods\$ModName"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RimTalk-ExpandActions 部署工具" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "源目录: $ProjectRoot" -ForegroundColor Yellow
Write-Host "目标目录: $TargetPath" -ForegroundColor Yellow
Write-Host ""

# 确保目标目录存在
if (-not (Test-Path $TargetPath)) {
    Write-Host "创建目标目录..." -ForegroundColor Green
    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
}

# 定义需要复制的文件夹
$FoldersToCopy = @(
    "About",
    "1.6",      # 包含 Assemblies 和 Defs
    "Defs"      # 确保 Defs 文件夹被复制
)

# 定义需要复制的根文件
$FilesToCopy = @(
    "LoadFolders.xml"
)

# 清理旧文件（可选）
Write-Host "清理目标目录中的旧文件..." -ForegroundColor Yellow

# 删除旧的 Defs 文件夹（确保完全更新）
$targetDefsPath = Join-Path $TargetPath "Defs"
if (Test-Path $targetDefsPath) {
    Remove-Item $targetDefsPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ? 已删除旧的 Defs 文件夹" -ForegroundColor Gray
}

# 删除旧的 1.6 文件夹中的 DLL
$targetAssembliesPath = Join-Path $TargetPath "1.6\Assemblies"
if (Test-Path $targetAssembliesPath) {
    Get-ChildItem $targetAssembliesPath -Filter "*.dll" | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "  ? 已删除旧的 DLL 文件" -ForegroundColor Gray
}

Write-Host ""

# 复制文件夹
foreach ($folder in $FoldersToCopy) {
    $sourcePath = Join-Path $ProjectRoot $folder
    $destPath = Join-Path $TargetPath $folder
    
    if (Test-Path $sourcePath) {
        Write-Host "正在复制: $folder" -ForegroundColor Green
        
        # 删除目标文件夹（如果存在）
        if (Test-Path $destPath) {
            Remove-Item $destPath -Recurse -Force
        }
        
        # 复制整个文件夹（包括所有子目录）
        Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
        
        # 统计文件数量
        $fileCount = (Get-ChildItem $destPath -Recurse -File).Count
        Write-Host "  ? 已复制 $fileCount 个文件" -ForegroundColor Gray
        
        # 如果是 Defs 文件夹，显示详细信息
        if ($folder -eq "Defs") {
            $subFolders = Get-ChildItem $destPath -Directory
            Write-Host "  包含子文件夹:" -ForegroundColor Gray
            foreach ($subFolder in $subFolders) {
                $subFileCount = (Get-ChildItem $subFolder.FullName -File).Count
                Write-Host "    - $($subFolder.Name) ($subFileCount 个文件)" -ForegroundColor DarkGray
            }
        }
    } else {
        Write-Host "  ? 跳过不存在的文件夹: $folder" -ForegroundColor DarkYellow
    }
}

Write-Host ""

# 复制根文件
foreach ($file in $FilesToCopy) {
    $sourcePath = Join-Path $ProjectRoot $file
    $destPath = Join-Path $TargetPath $file
    
    if (Test-Path $sourcePath) {
        Write-Host "正在复制: $file" -ForegroundColor Green
        Copy-Item -Path $sourcePath -Destination $destPath -Force
        Write-Host "  ? 已复制" -ForegroundColor Gray
    } else {
        Write-Host "  ? 跳过不存在的文件: $file" -ForegroundColor DarkYellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "部署完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 显示部署摘要
Write-Host "部署摘要:" -ForegroundColor Cyan
Write-Host "  - Mod 名称: $ModName" -ForegroundColor White
Write-Host "  - 目标路径: $TargetPath" -ForegroundColor White

# 检查关键文件
$keyFiles = @(
    "About\About.xml",
    "LoadFolders.xml",
    "1.6\Assemblies\RimTalk-ExpandActions.dll",
    "Defs\JobDefs\Jobs_SocialDining.xml",
    "Defs\InteractionDefs\Interaction_OfferFood.xml",
    "Defs\ThoughtDefs\Thoughts_SocialDining.xml"
)

Write-Host ""
Write-Host "关键文件检查:" -ForegroundColor Cyan
$allFilesOk = $true
foreach ($file in $keyFiles) {
    $fullPath = Join-Path $TargetPath $file
    if (Test-Path $fullPath) {
        $size = (Get-Item $fullPath).Length
        Write-Host "  ? $file ($size bytes)" -ForegroundColor Green
    } else {
        Write-Host "  ? $file (缺失!)" -ForegroundColor Red
        $allFilesOk = $false
    }
}

Write-Host ""

# 检查 Defs 文件夹结构
Write-Host "Defs 文件夹结构:" -ForegroundColor Cyan
$targetDefsPath = Join-Path $TargetPath "Defs"
if (Test-Path $targetDefsPath) {
    $defsSubFolders = Get-ChildItem $targetDefsPath -Directory
    foreach ($subFolder in $defsSubFolders) {
        $files = Get-ChildItem $subFolder.FullName -File
        Write-Host "  ?? $($subFolder.Name)" -ForegroundColor Yellow
        foreach ($file in $files) {
            Write-Host "    ? $($file.Name) ($($file.Length) bytes)" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "  ? Defs 文件夹不存在!" -ForegroundColor Red
    $allFilesOk = $false
}

Write-Host ""

if ($allFilesOk) {
    Write-Host "? 所有文件已正确部署！" -ForegroundColor Green
    Write-Host "现在可以启动 RimWorld 并在 Mod 列表中启用此 Mod！" -ForegroundColor Green
} else {
    Write-Host "?? 部署过程中发现缺失文件，请检查日志！" -ForegroundColor Yellow
}
