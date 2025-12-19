using System.Runtime.InteropServices;
using UnityEngine;

public class ClipboardService : IClipboardService
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaClass _unityPlayer;
    private AndroidJavaObject _currentActivity;
#endif

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _CopyToClipboard(string text);
#endif

    public ClipboardService()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        _currentActivity = _unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
#endif
    }

    public void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[ClipboardService] Attempted to copy empty text to clipboard");
            return;
        }

#if UNITY_EDITOR
        GUIUtility.systemCopyBuffer = text;
        Debug.Log($"[ClipboardService] Copied to clipboard (Editor): {text}");
#elif UNITY_ANDROID
        CopyToClipboardAndroid(text);
#elif UNITY_IOS
        CopyToClipboardIOS(text);
#else
        GUIUtility.systemCopyBuffer = text;
        Debug.Log($"[ClipboardService] Copied to clipboard (Fallback): {text}");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void CopyToClipboardAndroid(string text)
    {
        try
        {
            var context = _currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            var clipboard = context.Call<AndroidJavaObject>("getSystemService", "clipboard");
            var clipDataClass = new AndroidJavaClass("android.content.ClipData");
            var clipData = clipDataClass.CallStatic<AndroidJavaObject>("newPlainText", "label", text);
            clipboard.Call("setPrimaryClip", clipData);
            Debug.Log($"[ClipboardService] Copied to clipboard (Android): {text}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ClipboardService] Failed to copy to clipboard on Android: {e.Message}");
        }
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    private void CopyToClipboardIOS(string text)
    {
        try
        {
            _CopyToClipboard(text);
            Debug.Log($"[ClipboardService] Copied to clipboard (iOS): {text}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ClipboardService] Failed to copy to clipboard on iOS: {e.Message}");
        }
    }
#endif
}

