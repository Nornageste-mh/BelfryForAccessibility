param(
  [int]$StartWait = 40,
  [int]$AdvanceCount = 45,
  [switch]$KeepRunning
)
$ErrorActionPreference = 'Continue'
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class WinS {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@

$G   = 'D:\Steam\steamapps\common\钟塔 Belfry'
$R   = 'D:\DSHWorkBase\belfry_a11y'
$log = "$G\BepInEx\LogOutput.log"

# 游戏原生已占用的键一个都不碰
$VK = @{ RETURN=0x0D; SPACE=0x20; TAB=0x09; DOWN=0x28; UP=0x26; ESC=0x1B; BACK=0x08; HOME=0x24 }

function Key([string]$name, [int]$times = 1, [int]$gapMs = 800) {
  $vk = $VK[$name]
  for ($i = 0; $i -lt $times; $i++) {
    [WinS]::keybd_event([byte]$vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 70
    [WinS]::keybd_event([byte]$vk, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $gapMs
  }
}
# BepInEx 的 DiskLogListener 是带缓冲异步刷盘的（默认 2 秒一次），
# 读日志前必须等够，否则永远读到上一拍的旧行。
function LogTail([int]$n = 40, [string]$pattern = '\[朗读\]') {
  Start-Sleep -Seconds 3
  if (-not (Test-Path $log)) { return @() }
  return @(Get-Content $log -ErrorAction SilentlyContinue |
           Select-String -Pattern $pattern | Select-Object -Last $n |
           ForEach-Object { ($_.Line -split '\] ')[-1] })
}

Write-Output "=== 部署补丁 ==="
Copy-Item "$R\mod\package\*" $G -Recurse -Force
Remove-Item "$G\BepInEx\plugins\BelfryA11yProbe.dll" -ErrorAction SilentlyContinue
Remove-Item $log -ErrorAction SilentlyContinue

Write-Output "=== 启动游戏 ==="
Start-Process "steam://rungameid/2373260"
$proc = $null
for ($i = 0; $i -lt 40; $i++) {
  Start-Sleep -Seconds 3
  $proc = Get-Process -Name 'The Belfry' -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($proc -and $proc.MainWindowHandle -ne 0) { break }
}
if (-not $proc) { Write-Output "进程未启动，中止"; return }
Write-Output ("PID={0}" -f $proc.Id)

Write-Output "=== 等待标题画面 ($StartWait 秒) ==="
Start-Sleep -Seconds $StartWait
[WinS]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null
[WinS]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Seconds 2

Write-Output "--- 1) Tab 进入导航模式 ---"
Key TAB 1 1200
LogTail 3 | ForEach-Object { Write-Output ("    " + $_) }

Write-Output "--- 2) 下移 2 项，确认顺序 ---"
Key DOWN 2 1500
LogTail 3 | ForEach-Object { Write-Output ("    " + $_) }

Write-Output "--- 3) Home 回到第 1 项 ---"
Key HOME 1 1500
LogTail 2 | ForEach-Object { Write-Output ("    " + $_) }

Write-Output "--- 4) 空格激活第 1 项（开始游戏）---"
Key SPACE 1 4000
Start-Sleep -Seconds 25

Write-Output "--- 5) 空格 x$AdvanceCount 推进剧情 ---"
Key SPACE $AdvanceCount 1000
Start-Sleep -Seconds 4

Write-Output "--- 6) 退格重读当前句 ---"
Key BACK 1 2500

Write-Output ""
Write-Output "=== 截图 ==="
& "$R\probe\screenshot.ps1" -Out "$R\probe\shot_mod2.png" | Out-Null
Copy-Item "$R\probe\shot_mod2.png" "$R\probe\evidence\shot_dialogue.png" -Force -EA SilentlyContinue

if (-not $KeepRunning) {
  Start-Sleep -Seconds 2
  Get-Process -Name 'The Belfry' -ErrorAction SilentlyContinue | Stop-Process -Force
}

Write-Output ""
Write-Output "=== 朗读序列（最后 60 条）==="
LogTail 60 | ForEach-Object { Write-Output $_ }
Write-Output ""
Write-Output "=== 补丁 / 错误日志 ==="
Get-Content $log -ErrorAction SilentlyContinue |
  Select-String -Pattern 'Harmony|A11y Reader\]|Error|Exception' |
  Select-Object -First 20 | ForEach-Object { $_.Line }
Copy-Item $log "$R\probe\evidence\session_dialogue.log" -Force -EA SilentlyContinue
