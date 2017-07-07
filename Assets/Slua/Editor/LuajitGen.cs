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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SLua
{
    public class LuajitGen
    {
        //---------------------------------------------
        // process 
        public static Process StartProcess(string command, string param, string workDir = "")
        {
            return StartProcess(command, param, workDir, DataReceived, ErrorReceived);
        }

        public static Process StartProcess(
            string command,
            string param,
            string workDir,
            DataReceivedEventHandler dataReceived,
            DataReceivedEventHandler errorReceived)
        {
            Process ps = new Process
            {
                StartInfo =
            {
                FileName = command,
                Arguments = param,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workDir,
            }
            };
            ps.OutputDataReceived += dataReceived;
            ps.ErrorDataReceived += errorReceived;
            ps.Start();
            ps.BeginOutputReadLine();
            ps.BeginErrorReadLine();

            return ps;
        }

        public static void CompileLuaJit(string[] src, string[] dst, JITBUILDTYPE buildType)
        {
            UnityEngine.Debug.Log("compileLuajit");
#if !UNITY_EDITOR_OSX
            string workDir = Application.dataPath + "/../jit/";
            Dictionary<JITBUILDTYPE, string> build = new Dictionary<JITBUILDTYPE, string>
        {
            { JITBUILDTYPE.X86, Application.dataPath + "/../jit/win/x86/luajit.exe" },
            { JITBUILDTYPE.X64, Application.dataPath + "/../jit/win/x64/luajit.exe" },
            { JITBUILDTYPE.GC64, Application.dataPath + "/../jit/win/gc64/luajit.exe" },
        };
            string exePath = build[buildType];
            Process[] processList = new Process[src.Length];
#else
        string workDir = Application.dataPath + "/../jit/";
        Dictionary<JITBUILDTYPE, string> build = new Dictionary<JITBUILDTYPE, string>
        {
            { JITBUILDTYPE.X86, Application.dataPath + "/../jit/mac/x86/luajit" },
            { JITBUILDTYPE.X64, Application.dataPath + "/../jit/mac/x64/luajit" },
            { JITBUILDTYPE.GC64, Application.dataPath + "/../jit/mac/gc64/luajit" },
        };

        string exePath = build[ buildType ];
        // Process[] psList = new Process[ src.Length ];
#endif
            for (int i = 0; i < src.Length; i++)
            {
                string srcLua = Application.dataPath + "/../" + src[i];
                string dstLua = Application.dataPath + "/../" + dst[i];
                string cmd = " -b " + srcLua + " " + dstLua;
#if !UNITY_EDITOR_OSX
                processList[i] = StartProcess(exePath, cmd, workDir);
#else
                var ps = StartProcess(exePath, cmd, workDir );
                ps.WaitForExit();
#endif
            }
#if !UNITY_EDITOR_OSX
            foreach (Process ps in processList)
            {
                if (ps != null && !ps.HasExited)
                {
                    ps.WaitForExit();
                }
            }
#endif
        }

        public static void ExportLuajit(string res, string ext, string jitluadir, JITBUILDTYPE buildType)
        {
            // delete
            AssetDatabase.DeleteAsset(jitluadir);

            string[] files = Directory.GetFiles(res, ext, SearchOption.AllDirectories);
            string[] dests = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                string xfile = files[i].Remove(0, res.Length);
                xfile = xfile.Replace("\\", "/");
                string file = files[i].Replace("\\", "/");

                string dest = jitluadir + "/" + xfile;
                string destName = dest.Substring(0, dest.Length - 3) + "bytes";

                string destDir = Path.GetDirectoryName(destName);

                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                files[i] = file;
                dests[i] = destName;
                // Debug.Log(file + ":" + destName);
            }

            CompileLuaJit(files, dests, buildType);
            AssetDatabase.Refresh();
        }

        [MenuItem("SLua/Compile Bytecode/luajitx86 for WIN32&Android ARMV7")]
        public static void ExportLuajitx86()
        {
            ExportLuajit("Assets/Slua/Resources/", "*.txt", "Assets/Slua/jit/jitx86", JITBUILDTYPE.X86);
        }

        [MenuItem("SLua/Compile Bytecode/luajitx64 for WIN64")]
        public static void ExportLuajitx64()
        {
            ExportLuajit("Assets/Slua/Resources/", "*.txt", "Assets/Slua/jit/jitx64", JITBUILDTYPE.X64);
        }

        [MenuItem("SLua/Compile Bytecode/luajitgc64 for MAC&ARM64")]
        public static void ExportLuajitgc64()
        {
            ExportLuajit("Assets/Slua/Resources/", "*.txt", "Assets/Slua/jit/jitgc64", JITBUILDTYPE.GC64);
        }

        private static void DataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (eventArgs.Data != null)
            {
                UnityEngine.Debug.Log(eventArgs.Data);
            }
        }

        private static void ErrorReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            if (eventArgs.Data != null)
            {
                UnityEngine.Debug.LogError(eventArgs.Data);
            }
        }
    }
}
