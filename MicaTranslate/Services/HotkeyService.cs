using MicaTranslate.Core.Interfaces.Services;
using MicaTranslate.Core.Models;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.System;
using WinRT.Interop;

namespace MicaTranslate.Services;

public class HotkeyService : IHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;
    private const int GWL_WNDPROC = -4;

    private IntPtr _hwnd;
    private IntPtr _oldWndProc;
    private WndProcDelegate? _newWndProc;

    public event Action? HotkeyPressed;

    public void Initialize(Window window)
    {
        _hwnd = WindowNative.GetWindowHandle(window);

        _newWndProc = WndProc;
        _oldWndProc = SetWindowLongPtr(_hwnd, GWL_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(_newWndProc));
    }

    public bool Register(HotkeyModel hotkey)
    {
        Unregister();

        ParseHotkey(hotkey, out uint modifiers, out uint key);

        return RegisterHotKey(_hwnd, HOTKEY_ID, modifiers, key);
    }

    public void Unregister()
    {
        if (_hwnd != IntPtr.Zero)
            UnregisterHotKey(_hwnd, HOTKEY_ID);
    }

    private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private static void ParseHotkey(
        HotkeyModel hotkey,
        out uint modifiers,
        out uint key)
    {
        modifiers = 0;
        key = 0;

        foreach (var vk in hotkey.Keys)
        {
            switch (vk)
            {
                case VirtualKey.Control:
                case VirtualKey.LeftControl:
                case VirtualKey.RightControl:
                    modifiers |= 0x0002; // MOD_CONTROL
                    break;

                case VirtualKey.Shift:
                case VirtualKey.LeftShift:
                case VirtualKey.RightShift:
                    modifiers |= 0x0004; // MOD_SHIFT
                    break;

                case VirtualKey.Menu:
                case VirtualKey.LeftMenu:
                case VirtualKey.RightMenu:
                    modifiers |= 0x0001; // MOD_ALT
                    break;

                case VirtualKey.LeftWindows:
                case VirtualKey.RightWindows:
                    modifiers |= 0x0008; // MOD_WIN
                    break;

                default:
                    key = (uint)vk;
                    break;
            }
        }
    }

    public bool IsAvailable(HotkeyModel hotkey)
    {
        ParseHotkey(hotkey, out uint modifiers, out uint key);

        const int TEST_ID = 9999;

        bool success = RegisterHotKey(
            IntPtr.Zero,
            TEST_ID,
            modifiers,
            key);

        if (success)
        {
            UnregisterHotKey(IntPtr.Zero, TEST_ID);
        }

        return success;
    }

    private delegate IntPtr WndProcDelegate(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(
        IntPtr hWnd,
        int nIndex,
        IntPtr newProc);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(
        IntPtr lpPrevWndFunc,
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam);
}