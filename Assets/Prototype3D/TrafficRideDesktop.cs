using System;
using System.Collections;
using UnityEngine;

public partial class TrafficRidePrototype
{
    const string DesktopFullscreenPreference = "sr2_desktop_fullscreen";
    const string DesktopWidthPreference = "sr2_desktop_window_width";
    const string DesktopHeightPreference = "sr2_desktop_window_height";
    int desktopWindowWidth = 1280, desktopWindowHeight = 720;
    bool desktopKnownFullscreen, desktopRequestedFullscreen, desktopStartupApplied;
    float desktopDisplaySettlesAt, desktopResizeSaveAt;

    bool IsDesktopPlayer => Application.platform == RuntimePlatform.WindowsPlayer ||
        Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer;
    bool DesktopFullscreen => Time.unscaledTime < desktopDisplaySettlesAt ? desktopRequestedFullscreen : Screen.fullScreen;
    string DesktopFullscreenShortcut => Application.platform == RuntimePlatform.OSXPlayer ? "F11 / CONTROL + COMMAND + F" : "F11 / ALT + ENTER";

    static bool HasDesktopDisplayOverride(string[] args)
    {
        foreach (string arg in args)
            if (arg.Equals("-popupwindow", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-batchmode", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-nographics", StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith("-screen-", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-window-mode", StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith("-window-mode=", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    Vector2Int DesktopDisplaySize()
    {
        int width = Display.main.systemWidth, height = Display.main.systemHeight;
        if (width <= 0 || height <= 0) { width = Screen.currentResolution.width; height = Screen.currentResolution.height; }
        return new Vector2Int(Mathf.Max(640, width), Mathf.Max(360, height));
    }

    void InitializeDesktopDisplay()
    {
        if (!IsDesktopPlayer) return;
        // The desktop menus remain ordinary mouse targets in either display mode.
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        desktopWindowWidth = PlayerPrefs.GetInt(DesktopWidthPreference, 1280);
        desktopWindowHeight = PlayerPrefs.GetInt(DesktopHeightPreference, 720);
        desktopKnownFullscreen = desktopRequestedFullscreen = Screen.fullScreen;
        // Capture/QA dimensions and explicit Unity player switches take precedence over preferences.
        if (qaMode || HasDesktopDisplayOverride(Environment.GetCommandLineArgs())) return;
        desktopStartupApplied = true;
        SetDesktopFullscreen(PlayerPrefs.GetInt(DesktopFullscreenPreference, 1) != 0, false);
    }

    void SetDesktopFullscreen(bool fullscreen, bool remember = true)
    {
        if (!IsDesktopPlayer) return;
        if (!Screen.fullScreen && fullscreen) { desktopWindowWidth = Screen.width; desktopWindowHeight = Screen.height; }
        var display = DesktopDisplaySize();
        // Leave room for the title bar and desktop when returning to a window.
        int width = Mathf.Clamp(desktopWindowWidth, 640, Mathf.Max(640, Mathf.RoundToInt(display.x * .9f)));
        int height = Mathf.Clamp(desktopWindowHeight, 360, Mathf.Max(360, Mathf.RoundToInt(display.y * .9f)));
        desktopRequestedFullscreen = fullscreen;
        desktopDisplaySettlesAt = Time.unscaledTime + .75f;
        Screen.SetResolution(fullscreen ? display.x : width, fullscreen ? display.y : height,
            fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        if (remember && !qaMode) {
            PlayerPrefs.SetInt(DesktopFullscreenPreference, fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(DesktopWidthPreference, width); PlayerPrefs.SetInt(DesktopHeightPreference, height);
            PlayerPrefs.Save();
        }
    }

    void ToggleDesktopFullscreen()
    {
        if (!IsDesktopPlayer || Time.unscaledTime < desktopDisplaySettlesAt) return;
        SetDesktopFullscreen(!Screen.fullScreen);
    }

    void TickDesktopDisplay()
    {
        if (!IsDesktopPlayer) return;
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool command = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool mac = Application.platform == RuntimePlatform.OSXPlayer;
        // Native Unity fullscreen shortcuts are disabled by the desktop build configuration.
        if (Input.GetKeyDown(KeyCode.F11) || (!mac && alt && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) ||
            (mac && command && control && Input.GetKeyDown(KeyCode.F))) ToggleDesktopFullscreen();
        if (Time.unscaledTime < desktopDisplaySettlesAt) return;
        bool fullscreen = Screen.fullScreen;
        if (fullscreen != desktopKnownFullscreen) {
            desktopKnownFullscreen = desktopRequestedFullscreen = fullscreen;
            if (!qaMode) { PlayerPrefs.SetInt(DesktopFullscreenPreference, fullscreen ? 1 : 0); PlayerPrefs.Save(); }
        }
        if (!fullscreen && (Screen.width != desktopWindowWidth || Screen.height != desktopWindowHeight)) {
            desktopWindowWidth = Screen.width; desktopWindowHeight = Screen.height;
            desktopResizeSaveAt = Time.unscaledTime + .5f;
        }
        if (!qaMode && desktopResizeSaveAt > 0 && Time.unscaledTime >= desktopResizeSaveAt) {
            desktopResizeSaveAt = 0;
            PlayerPrefs.SetInt(DesktopWidthPreference, desktopWindowWidth); PlayerPrefs.SetInt(DesktopHeightPreference, desktopWindowHeight);
            PlayerPrefs.Save();
        }
    }

    void QuitDesktopGame()
    {
        if (!IsDesktopPlayer || qaMode) return;
        PlayerPrefs.Save(); Application.Quit();
    }

    IEnumerator DesktopDisplayQA()
    {
        if (!IsDesktopPlayer) yield break;
        int width = Screen.width, height = Screen.height, savedChoice = PlayerPrefs.GetInt(DesktopFullscreenPreference, -1);
        FullScreenMode mode = Screen.fullScreenMode;
        bool fullscreen = Screen.fullScreen;
        Check(!desktopStartupApplied, "desktop QA retains command-line display dimensions at startup");
        SetDesktopFullscreen(!fullscreen, false);
        float deadline = Time.realtimeSinceStartup + 5;
        do { yield return null; } while (Screen.fullScreen == fullscreen && Time.realtimeSinceStartup < deadline);
        Check(Screen.fullScreen != fullscreen, "desktop fullscreen toggle changes the real player display mode");
        Screen.SetResolution(width, height, mode);
        deadline = Time.realtimeSinceStartup + 5;
        do { yield return null; } while ((Screen.fullScreenMode != mode || Screen.width != width || Screen.height != height) && Time.realtimeSinceStartup < deadline);
        desktopKnownFullscreen = desktopRequestedFullscreen = Screen.fullScreen;
        desktopDisplaySettlesAt = Time.unscaledTime + .75f;
        if (!fullscreen) { desktopWindowWidth = width; desktopWindowHeight = height; }
        Check(Screen.fullScreenMode == mode && Screen.width == width && Screen.height == height,
            "desktop display toggle restores exact screenshot dimensions and original mode");
        Check(PlayerPrefs.GetInt(DesktopFullscreenPreference, -1) == savedChoice, "desktop QA does not change the saved display preference");
        Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "desktop menu mouse remains visible and unlocked");
    }
}
