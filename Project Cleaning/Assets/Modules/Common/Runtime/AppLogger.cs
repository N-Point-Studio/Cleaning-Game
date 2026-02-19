using UnityEngine;

namespace Modules
{
    /// <summary>
    /// Class for logging. Use symbol DEBUG_MODE to display outside Unity Editor
    /// </summary>
    public static class AppLogger
    {
        public static void Log(object debug)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.Log($"<color=blue>Log:</color> {debug}");
#endif
        }

        public static void Log(object debug, Object context)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.Log($"<color=blue>Log:</color> {debug}", context);
#endif
        }

        public static void LogFormat(object debug, params object[] obj)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.LogFormat($"<color=blue>Log:</color> {debug}", obj);
#endif
        }

        public static void LogFormat(Object context, object debug, params object[] obj)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.LogFormat(context, $"<color=blue>Log:</color> {debug}", obj);
#endif
        }

        public static void LogWarning(object debug)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.LogWarning($"<color=yellow>Warning:</color> {debug}");
#endif
        }

        public static void LogWarning(Object context, object debug)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.LogWarning($"<color=yellow>Warning:</color> {debug}", context);
#endif
        }

        public static void LogWarningFormat(object debug, params object[] obj)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.LogWarningFormat($"<color=yellow>Warning:</color> {debug}", obj);
#endif
        }

        public static void LogWarningFormat(Object context, object debug, params object[] obj)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.LogWarningFormat(context, $"<color=yellow>Warning:</color> {debug}", obj);
#endif
        }

        public static void LogError(object debug)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.LogError($"<color=red>Error:</color> {debug}");
#endif
        }

        public static void LogError(object debug, Object context)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.LogError($"<color=red>Error:</color> {debug}", context);
#endif
        }

        public static void LogErrorFormat(object debug, params object[] obj)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.LogErrorFormat($"<color=red>Error:</color> {debug}", obj);
#endif
        }

        public static void LogErrorFormat(Object context, object debug, params object[] obj)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.LogErrorFormat(context, $"<color=red>Error:</color> {debug}", obj);
#endif
        }

        public static void DrawLine(Vector3 start, Vector3 end)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.DrawLine(start, end);
#endif
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.DrawLine(start, end, color);
#endif
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
        {
#if UNITY_EDITOR || DEBUG_MODE
            Debug.DrawLine(start, end, color, duration);
#endif
        }
    }
}