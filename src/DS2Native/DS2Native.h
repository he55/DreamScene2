// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the DS2NATIVE_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// DS2NATIVE_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef DS2NATIVE_EXPORTS
#define DS2NATIVE_API __declspec(dllexport)
#else
#define DS2NATIVE_API __declspec(dllimport)
#endif


#ifdef __cplusplus
extern "C" {
#endif

    DS2NATIVE_API ULONGLONG WINAPI DS2_GetLastInputTickCount(void);
    DS2NATIVE_API HWND WINAPI DS2_GetDesktopWindowHandle(void);
    DS2NATIVE_API int WINAPI DS2_TestScreen(RECT rect);
    DS2NATIVE_API void WINAPI DS2_SetWindowPosition(HWND hWnd, RECT rect);
    DS2NATIVE_API void WINAPI DS2_RestoreLastWindowPosition(void);
    DS2NATIVE_API void WINAPI DS2_RefreshDesktop(BOOL animated = FALSE);
    DS2NATIVE_API void WINAPI DS2_ToggleShowDesktopIcons(void);
    DS2NATIVE_API BOOL WINAPI DS2_IsVisibleDesktopIcons(void);
    DS2NATIVE_API BOOL WINAPI DS2_StartForwardMouseKeyboardMessage(HWND hWnd);
    DS2NATIVE_API void WINAPI DS2_EndForwardMouseKeyboardMessage(void);
    DS2NATIVE_API void WINAPI DS2_ToggleProcess(DWORD dwPID, BOOL bResumeProcess);

#ifdef __cplusplus
}
#endif
