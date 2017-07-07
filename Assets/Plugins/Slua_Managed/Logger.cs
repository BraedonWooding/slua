#region License
// ====================================================
// Copyright(C) 2015 Siney/Pangweiwei siney@yeah.net
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
//
// Braedon Wooding braedonww@gmail.com, applied major changes to this project.
// ====================================================
#endregion

using System;
using UnityEngine;

namespace SLua
{
    /// <summary>
    /// A bridge between UnityEngine.Debug.LogXXX and standalone.LogXXX.
    /// </summary>
    public class Logger
    {
        public enum Level
        {
            Debug,
            Warning,
            Error
        }

        public static Action<Level, string> LogAction { get; private set; }

        public static void Log(string msg, bool hasStacktrace = false)
        {
            if (LogAction != null)
            {
                LogAction(Level.Debug, msg);
                return;
            }

            StackTraceLogType type = Application.GetStackTraceLogType(LogType.Log);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Debug.Log(msg, hasStacktrace ? FindScriptByMsg(msg) : null);
            Application.SetStackTraceLogType(LogType.Log, type);
        }

        public static void LogError(string msg, bool hasStacktrace = false)
        {
            if (LogAction != null)
            {
                LogAction(Level.Error, msg);
                return;
            }

            StackTraceLogType type = Application.GetStackTraceLogType(LogType.Error);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
            Debug.LogError(msg, hasStacktrace ? FindScriptByMsg(msg) : null);
            Application.SetStackTraceLogType(LogType.Error, type);
        }

        public static void LogWarning(string msg)
        {
            if (LogAction != null)
            {
                LogAction(Level.Warning, msg);
                return;
            }

            Debug.LogWarning(msg);
        }

        private static UnityEngine.Object FindScriptByMsg(string msg)
        {
#if UNITY_EDITOR
            string[] lines = msg.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 2; i < lines.Length; i++)
            {
                int idx = lines[i].IndexOf(":");
                if (idx < 0)
                {
                    continue;
                }

                string filename = lines[i].Substring(0, idx);
                idx = filename.LastIndexOf("/");
                if (idx >= 0)
                {
                    filename = filename.Substring(idx + 1);
                }

                filename = filename.Trim();
                string[] guids = UnityEditor.AssetDatabase.FindAssets(filename);
                filename = filename + ".txt";
                for (int j = 0; j < guids.Length; j++)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[j]);
                    if (System.IO.Path.GetFileName(path).Equals(filename))
                    {
                        return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    }
                }
            }
#endif
            return null;
        }
    }
}
