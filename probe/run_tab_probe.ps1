param([int]$StartWait = 38)
$ErrorActionPreference = 'Continue'
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class WinQ {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
$G   = 'D:\Steam\steamapps\common\钟塔 Belfry'
$log = "$G\BepInEx\LogOutput.log"
$VK = @{ TAB=0x09; DOWN=0x28; SPACE=0x20 }

function Key([string]$name, [int]$times = 1, [int]$gapMs = 800) {
  $vk = $VK[$name]
  for ($i = 0; $i -lt $times; $i++) {
    [WinQ]::keybd_event([byte]$vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 70
    [WinQ]::keybd_event([byte]$vk, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $gapMs
  }
}

Get-Process -Name 'The Belfry' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
Remove-Item $log -ErrorAction SilentlyContinue
Start-Process "steam://rungameid/2373260"
$proc = $null
for ($i = 0; $i -lt 40; $i++) {
  Start-Sleep -Seconds 3
  $proc = Get-Process -Name 'The Belfry' -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($proc -and $proc.MainWindowHandle -ne 0) { break }
}
if (-not $proc) { Write-Output "进程未启动"; return }
Start-Sleep -Seconds $StartWait
[WinQ]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null
[WinQ]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Seconds 2

Write-Output "--- Tab #1 ---"; Key TAB 1 3000
Write-Output "--- Tab #2 ---"; Key TAB 1 3000
Write-Output "--- Tab #3 ---"; Key TAB 1 3000
Write-Output "--- Down x3 ---"; Key DOWN 3 2500
Write-Output "--- Tab #4（退出） ---"; Key TAB 1 3000
Write-Output "--- Space（应当交回游戏，不进剧情也无妨）---"; Key SPACE 1 3000

Start-Sleep -Seconds 4
Get-Process -Name 'The Belfry' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 3

Write-Output ""
Write-Output "=== 状态迁移日志 ==="
Get-Content $log | Select-String -Pattern 'UiNav|朗读' | ForEach-Object { ($_.Line -replace '^\[Info\s*:\s*The Belfry A11y Reader\]\s*','') }
