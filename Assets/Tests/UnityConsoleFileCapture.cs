#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace DarkTreeFPS.Tests
{
    /// <summary>
    /// Writes every Unity Console message and stack trace to a project-local file while
    /// Play Mode is running, so test results can be inspected after the session.
    /// </summary>
    internal static class UnityConsoleFileCapture
    {
        private static readonly object FileLock = new object();
        private static string logPath;
        private static string captureFailure;

        internal static string LogPath => logPath;
        internal static string CaptureFailure => captureFailure;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeginCapture()
        {
            Application.logMessageReceived -= OnLogMessage;
            logPath = Path.GetFullPath(Path.Combine(
                Application.dataPath, "../Temp/UnityTestLogs/unity-console.log"));
            captureFailure = null;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.WriteAllText(logPath,
                    "Unity Console capture started: " + DateTime.Now.ToString("O") + Environment.NewLine);
                Application.logMessageReceived += OnLogMessage;
                AppendRecord("CAPTURE", "All Unity Console messages are being recorded.", logPath);
            }
            catch (Exception exception)
            {
                captureFailure = exception.ToString();
            }
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            AppendRecord(type.ToString(), condition, stackTrace);
        }

        private static void AppendRecord(string type, string condition, string stackTrace)
        {
            if (string.IsNullOrEmpty(logPath))
                return;

            var record = "[" + DateTime.Now.ToString("O") + "] [" + type + "] " + condition + Environment.NewLine;
            if (!string.IsNullOrEmpty(stackTrace))
                record += stackTrace + Environment.NewLine;

            try
            {
                lock (FileLock)
                    File.AppendAllText(logPath, record);
            }
            catch (Exception exception)
            {
                captureFailure = exception.ToString();
                Application.logMessageReceived -= OnLogMessage;
            }
        }
    }
}
#endif
