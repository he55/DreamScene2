// DS2Native.cpp : Defines the exported functions for the DLL.
//

#include "framework.h"
#include "DS2Native.h"
#include <ShlObj.h>


ULONGLONG WINAPI DS2_GetLastInputTickCount(void) {
    LASTINPUTINFO lii = { sizeof(LASTINPUTINFO),0 };
    GetLastInputInfo(&lii);
    ULONGLONG tick = GetTickCount64();
    return tick - lii.dwTime;
}


HWND WINAPI DS2_GetDesktopWindowHandle(void) {
    HWND hWnd1 = FindWindow("Progman", NULL);
    SendMessageTimeout(hWnd1,
        0x052c,
        NULL,
        NULL,
        SMTO_NORMAL,
        1000,
        NULL);

    static HWND g_hWnd = NULL;
    EnumWindows([](HWND hWnd, LPARAM) {
        HWND hWnd2 = FindWindowEx(hWnd, NULL, "SHELLDLL_DefView", NULL);
        if (hWnd2) {
            g_hWnd = FindWindowEx(NULL, hWnd, "WorkerW", NULL);
            return FALSE;
        }
        return TRUE;
        }, NULL);

    return g_hWnd;
}


int WINAPI DS2_TestScreen(RECT rect) {
    static HWND g_hWnd = NULL;
    if (!g_hWnd) {
        EnumWindows([](HWND hWnd, LPARAM) {
            HWND hWnd1 = FindWindowEx(hWnd, NULL, "SHELLDLL_DefView", NULL);
            if (hWnd1) {
                g_hWnd = FindWindowEx(hWnd1, NULL, "SysListView32", NULL);
                return FALSE;
            }
            return TRUE;
            }, NULL);
    }

    const int offset = 4;
    int ic = 0;
    int x = rect.left + offset;
    int y = rect.top + offset;
    int w = rect.right - rect.left - offset * 2;
    int h = rect.bottom - rect.top - offset * 2;

    POINT ps[9] = {
        {x,y},      {x + (w / 2),y},      {x + w,y},
        {x,y + (h / 2)},{x + (w / 2),y + (h / 2)},{x + w,y + (h / 2)},
        {x,y + h},    {x + (w / 2),y + h},    {x + w,y + h}
    };

    for (size_t i = 0; i < 9; i++)
    {
        HWND hWnd2 = WindowFromPoint(ps[i]);
        if (hWnd2 == g_hWnd) {
            ++ic;
        }
    }
    return ic;
}


typedef struct {
    HWND hWnd;
    HWND hWndParent;
    RECT rect;
    LONG dwStyle;
} LASTWINDOWINFO;

LASTWINDOWINFO g_lwi;


void WINAPI DS2_SetWindowPosition(HWND hWnd, RECT rect) {
    ShowWindow(hWnd, SW_RESTORE);

    RECT rect2 = { 0 };
    GetWindowRect(hWnd, &rect2);
    LONG dwStyle = GetWindowLong(hWnd, GWL_STYLE);
    HWND hWndParent = GetParent(hWnd);
    g_lwi = { hWnd,hWndParent,rect2,dwStyle };

    SetWindowLong(hWnd, GWL_STYLE, dwStyle & (~WS_CAPTION) & (~WS_SYSMENU) & (~WS_THICKFRAME));
    SetWindowPos(hWnd,
        HWND_TOP,
        rect.left,
        rect.top,
        rect.right - rect.left,
        rect.bottom - rect.top,
        SWP_SHOWWINDOW);
}


void WINAPI DS2_RestoreLastWindowPosition(void) {
    if (g_lwi.hWnd) {
        SetParent(g_lwi.hWnd, g_lwi.hWndParent);
        SetWindowLong(g_lwi.hWnd, GWL_STYLE, g_lwi.dwStyle);
        SetWindowPos(g_lwi.hWnd,
            HWND_TOP,
            g_lwi.rect.left,
            g_lwi.rect.top,
            g_lwi.rect.right - g_lwi.rect.left,
            g_lwi.rect.bottom - g_lwi.rect.top,
            SWP_SHOWWINDOW);
    }
    g_lwi = { 0 };
}


void WINAPI DS2_RefreshDesktop(BOOL animated) {
    if (animated) {
        HRESULT nRet = CoInitialize(NULL);
        if (SUCCEEDED(nRet)) {
            IDesktopWallpaper* pDesktopWallpaper = NULL;
            nRet = CoCreateInstance(CLSID_DesktopWallpaper, 0, CLSCTX_LOCAL_SERVER, IID_IDesktopWallpaper, (void**)&pDesktopWallpaper);
            if (SUCCEEDED(nRet)) {
                LPWSTR path = NULL;
                pDesktopWallpaper->GetWallpaper(NULL, &path);
                if (path && wcslen(path)) {
                    pDesktopWallpaper->SetWallpaper(NULL, path);
                }
                else {
                    COLORREF color;
                    pDesktopWallpaper->GetBackgroundColor(&color);
                    pDesktopWallpaper->SetBackgroundColor(color);
                }
                pDesktopWallpaper->Release();
            }
            CoUninitialize();
        }
    }
    else {
        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, NULL, SPIF_UPDATEINIFILE);
    }
}


BOOL WINAPI DS2_IsVisibleDesktopIcons(void) {
    SHELLSTATE state = { 0 };
    SHGetSetSettings(&state, SSF_HIDEICONS, FALSE);
    return !state.fHideIcons;
}


HWND GetDesktopSHELLDLL_DefView()
{
    HWND hShellViewWin = NULL;
    HWND hWorkerW = NULL;

    HWND hProgman = FindWindow("Progman", "Program Manager");
    HWND hDesktopWnd = GetDesktopWindow();

    // If the main Program Manager window is found
    if (hProgman != NULL)
    {
        // Get and load the main List view window containing the icons.
        hShellViewWin = FindWindowEx(hProgman, NULL, "SHELLDLL_DefView", NULL);
        if (hShellViewWin == NULL)
        {
            // When this fails (picture rotation is turned ON, toggledesktop shell cmd used ), then look for the WorkerW windows list to get the
            // correct desktop list handle.
            // As there can be multiple WorkerW windows, iterate through all to get the correct one
            do
            {
                hWorkerW = FindWindowEx(hDesktopWnd, hWorkerW, "WorkerW", NULL);
                hShellViewWin = FindWindowEx(hWorkerW, NULL, "SHELLDLL_DefView", NULL);
            } while (hShellViewWin == NULL && hWorkerW != NULL);
        }
    }
    return hShellViewWin;
}


void WINAPI DS2_ToggleShowDesktopIcons(void) {
    HWND hShellViewWin = GetDesktopSHELLDLL_DefView();
    if (hShellViewWin) {
        SendMessage(hShellViewWin, WM_COMMAND, 0x7402, NULL);
    }
}


BOOL DS2_IsDesktop(void) {
    // Thanks: https://stackoverflow.com/a/56812642
    HWND hProgman = FindWindow("Progman", "Program Manager");
    HWND hWorkerW = NULL;

    // Get and load the main List view window containing the icons.
    HWND   hShellViewWin = FindWindowEx(hProgman, NULL, "SHELLDLL_DefView", NULL);
    if (!hShellViewWin)
    {
        HWND hDesktopWnd = GetDesktopWindow();

        // When this fails (picture rotation is turned ON, toggledesktop shell cmd used ), then look for the WorkerW windows list to get the
        // correct desktop list handle.
        // As there can be multiple WorkerW windows, iterate through all to get the correct one
        do
        {
            hWorkerW = FindWindowEx(hDesktopWnd, hWorkerW, "WorkerW", NULL);
            hShellViewWin = FindWindowEx(hWorkerW, NULL, "SHELLDLL_DefView", NULL);
        } while (!hShellViewWin && hWorkerW);
    }

    HWND hForegroundWindow = GetForegroundWindow();
    return hForegroundWindow == hWorkerW || hForegroundWindow == hProgman;
}


HWND g_hWnd = NULL;

LRESULT CALLBACK LowLevelKeyboardProc(int    nCode, WPARAM wParam, LPARAM lParam) {
    if (nCode == HC_ACTION) {
        if (DS2_IsDesktop()) {
            KBDLLHOOKSTRUCT* p = (KBDLLHOOKSTRUCT*)lParam;

            if (wParam == WM_KEYDOWN) {
                int lp = 1 | (p->scanCode << 16) | (1 << 24) | (0 << 29) | (0 << 30) | (0 << 31);
                PostMessage(g_hWnd, (UINT)wParam, p->vkCode, lp);
            }
            else if (wParam == WM_KEYUP) {
                int lp = 1 | (p->scanCode << 16) | (1 << 24) | (0 << 29) | (1 << 30) | (1 << 31);
                PostMessage(g_hWnd, (UINT)wParam, p->vkCode, lp);
            }
        }
    }
    return CallNextHookEx(NULL, nCode, wParam, lParam);
}


LRESULT CALLBACK LowLevelMouseProc(int    nCode, WPARAM wParam, LPARAM lParam) {
    if (nCode == HC_ACTION) {
        MSLLHOOKSTRUCT* p = (MSLLHOOKSTRUCT*)lParam;
        LONG lp = MAKELONG(p->pt.x, p->pt.y);

        if (DS2_IsDesktop()) {
            if (wParam == WM_MOUSEMOVE) {
                PostMessage(g_hWnd, (UINT)wParam, MK_XBUTTON1, lp);
            }
            else  if (wParam == WM_LBUTTONDOWN || wParam == WM_LBUTTONUP) {
                PostMessage(g_hWnd, (UINT)wParam, MK_LBUTTON, lp);
            }
            else  if (wParam == WM_MOUSEWHEEL) {
                // TODO:
            }
        }
        else  if (wParam == WM_MOUSEMOVE) {
            RECT rect = { 0 };
            GetWindowRect(GetForegroundWindow(), &rect);

            if (!PtInRect(&rect, p->pt)) {
                PostMessage(g_hWnd, (UINT)wParam, MK_XBUTTON1, lp);
            }
        }
    }
    return CallNextHookEx(NULL, nCode, wParam, lParam);
}


HHOOK g_hLowLevelMouseHook = NULL;
HHOOK g_hLowLevelKeyboardHook = NULL;

BOOL WINAPI DS2_StartForwardMouseKeyboardMessage(HWND hWnd) {
    g_hWnd = hWnd;

    HMODULE hModule = GetModuleHandle(NULL);
    g_hLowLevelMouseHook = SetWindowsHookEx(WH_MOUSE_LL, LowLevelMouseProc, hModule, NULL);
    if (!g_hLowLevelMouseHook) {
        return FALSE;
    }

    g_hLowLevelKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, LowLevelKeyboardProc, hModule, NULL);
    return TRUE;
}


void WINAPI DS2_EndForwardMouseKeyboardMessage(void) {
    if (g_hLowLevelMouseHook) {
        UnhookWindowsHookEx(g_hLowLevelMouseHook);
        g_hLowLevelMouseHook = NULL;
    }

    if (g_hLowLevelKeyboardHook) {
        UnhookWindowsHookEx(g_hLowLevelKeyboardHook);
        g_hLowLevelKeyboardHook = NULL;
    }
}


typedef LONG(NTAPI* NtSuspendProcess)(IN HANDLE ProcessHandle);
typedef LONG(NTAPI* NtResumeProcess)(IN HANDLE ProcessHandle);

void WINAPI DS2_ToggleProcess(DWORD dwPID, BOOL bResumeProcess) {
    HMODULE hModule = GetModuleHandle("ntdll");
    if (hModule) {
        HANDLE processHandle = OpenProcess(PROCESS_SUSPEND_RESUME, FALSE, dwPID);

        if (bResumeProcess)
        {
            NtResumeProcess pfnNtResumeProcess = (NtResumeProcess)GetProcAddress(hModule, "NtResumeProcess");
            pfnNtResumeProcess(processHandle);
        }
        else
        {
            NtSuspendProcess pfnNtSuspendProcess = (NtSuspendProcess)GetProcAddress(hModule, "NtSuspendProcess");
            pfnNtSuspendProcess(processHandle);
        }
        CloseHandle(processHandle);
    }
}
