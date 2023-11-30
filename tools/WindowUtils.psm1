Add-Type @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public struct RECT
{
	public int left;
	public int top;
	public int right;
	public int bottom;

    public int Width { get { return right - left; } }
    public int Height { get { return bottom - top; } }
}

public class Win32
{
	public delegate void ThreadDelegate(IntPtr hWnd, IntPtr lParam);
	
	[DllImport("user32.dll")]
	public static extern bool EnumThreadWindows(int dwThreadId, ThreadDelegate lpfn, IntPtr lParam);
	
	[DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
	public static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int cch);
	
	[DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
	public static extern Int32 GetWindowTextLength(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern bool IsIconic(IntPtr hWnd);

	[DllImport("user32.dll")]
	public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, ExactSpelling = true, SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	public static string GetTitle(IntPtr hWnd) {
		var len = GetWindowTextLength(hWnd);
		StringBuilder title = new StringBuilder(len + 1);
		GetWindowText(hWnd, title, title.Capacity);
		return title.ToString();
	}
}
"@

Add-Type -AssemblyName System.Windows.Forms

function Move-Window([System.IntPtr]$WindowHandle, [System.Int32]$Monitor, [System.Int32]$X, [System.Int32]$Y, [switch]$FromRight, [switch]$FromBottom) {
    $screens = [System.Windows.Forms.Screen]::AllScreens

    if ($Monitor -lt 0 -or $Monitor -ge $screens.Count) {
        throw "Invalid monitor index: $Monitor"
    }

    $screen = $screens[$Monitor]
    
	$rect = New-Object RECT
	[void][Win32]::GetWindowRect($WindowHandle, [ref]$rect)

    if ($FromRight) {
        $X = $screen.Bounds.X + $screen.Bounds.Width - $rect.Width - $X
    } else {
        $X = $screen.Bounds.X + $X
    }

    if ($FromBottom) {
        $Y = $screen.Bounds.Y + $screen.Bounds.Height - $rect.Height - $Y
    } else {
        $Y = $screen.Bounds.Y + $Y
    }

	[void][Win32]::MoveWindow($WindowHandle, $X, $Y, $rect.Width, $rect.Height, $true)
}

function Show-Window([System.IntPtr]$WindowHandle) {
	[void][Win32]::ShowWindow($WindowHandle, 6)
	[void][Win32]::ShowWindow($WindowHandle, 1)
}

function Get-ChildWindow($Process) {
	$windows = New-Object System.Collections.ArrayList
	$Process.Threads.ForEach({
		[void][Win32]::EnumThreadWindows(
			$_.Id,
			{
				param($hwnd, $lparam)
				if (([Win32]::IsIconic($hwnd) -or [Win32]::IsWindowVisible($hwnd)) -and [Win32]::GetTitle($hwnd) -contains "Rounds") {
					$windows.Add($hwnd)
				}
			},
			0
		)
	})
	return $windows[0]
}