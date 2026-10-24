# Saves a PNG of the test game's window (the process launch_test.ps1 started).
param([Parameter(Mandatory = $true)][string]$Path)
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class WinRect {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
'@
$id = [int](Get-Content "$env:TEMP\museart_test_pid.txt" | Select-Object -First 1)
$h = (Get-Process -Id $id).MainWindowHandle
$r = New-Object WinRect+RECT
[WinRect]::GetClientRect($h, [ref]$r) | Out-Null
$p = New-Object WinRect+POINT
[WinRect]::ClientToScreen($h, [ref]$p) | Out-Null
$bmp = New-Object System.Drawing.Bitmap ($r.Right - $r.Left), ($r.Bottom - $r.Top)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($p.X, $p.Y, 0, 0, $bmp.Size)
$bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
"saved $Path"
