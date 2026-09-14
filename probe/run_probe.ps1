$ErrorActionPreference = 'Continue'
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class WinB {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@

$G = 'D:\Steam\steamapps\common\钟塔 Belfry'
$P = 'D:\DSHWorkBase\belfry_a11y\probe'
$log = "$G\BepInEx\probe_belfry.log"
$beplog = "$G\BepInEx\LogOutput.log"
$VK_RETURN = 0x0D; $VK_SPACE = 0x20; $VK_ESC = 0x1B

function Key([byte]$vk, [int]$times, [int]$gapMs) {
  for ($i = 0; $i -lt $times; $i++) {
    [WinB]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [WinB]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $gapMs
  }
}

Write-Output "=== 部署探针插件 ==="
New-Item -ItemType Directory -Force -Path "$G\BepInEx\plugins" | Out-Null
Copy-Item "$P\plugin\bin\Release\BelfryA11yProbe.dll" "$G\BepInEx\plugins\" -Force
Remove-Item $log -ErrorAction SilentlyContinue
Remove-Item $beplog -ErrorAction SilentlyContinue
Write-Output "已放入 BepInEx\plugins\"

Write-Output "=== 启动游戏 ==="
Start-Process "steam://rungameid/2373260"

$proc = $null
for ($i = 0; $i -lt 40; $i++) {
  Start-Sleep -Seconds 3
  $proc = Get-Process -Name 'The Belfry' -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($proc -and $proc.MainWindowHandle -ne 0) { break }
}
if (-not $proc) { Write-Output "进程未启动，中止"; return }
Write-Output ("进程 PID={0} 窗口句柄={1}" -f $proc.Id, $proc.MainWindowHandle)

Start-Sleep -Seconds 40
Write-Output "=== 聚焦窗口并发送按键 ==="
[WinB]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null
[WinB]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Seconds 2
Write-Output "--- 回车 x2 ---"
Key $VK_RETURN 2 2500
Start-Sleep -Seconds 8
Write-Output "--- 空格 x20（推进剧情）---"
Key $VK_SPACE 20 1400
Start-Sleep -Seconds 5

Write-Output ""
Write-Output "=== BepInEx LogOutput.log 前 60 行 ==="
Get-Content $beplog -ErrorAction SilentlyContinue -TotalCount 60

Write-Output ""
Write-Output "=== probe_belfry.log 行数: $((Get-Content $log -ErrorAction SilentlyContinue | Measure-Object).Count) ==="
Get-Content $log -ErrorAction SilentlyContinue -TotalCount 80

Write-Output ""
Write-Output "=== 截图 ==="
& "$P\screenshot.ps1" -Out "$P\shot_probe.png"
