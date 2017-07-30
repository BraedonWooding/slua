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

using SLua;
using UnityEngine;

public class Perf : MonoBehaviour
{
    private LuaSvr l;
    private bool inited = false;

    private string logText = string.Empty;

    // Use this for initialization
    private void Start()
    {
        long startMem = System.GC.GetTotalMemory(true);

        float start = Time.realtimeSinceStartup;
        l = new LuaSvr();
        l.Init(null, () =>
        {
            Debug.Log("start cost: " + (Time.realtimeSinceStartup - start));

            long endMem = System.GC.GetTotalMemory(true);
            Debug.Log("startMem: " + startMem + ", endMem: " + endMem + ", " + "cost mem: " + (endMem - startMem));
            l.Start("perf");
            inited = true;
        });

#if UNITY_5
        Application.logMessageReceived += this.Log;
#else
        Application.RegisterLogCallback(this.log);
#endif
    }

    private void Log(string cond, string trace, LogType lt)
    {
        logText += cond;
        logText += "\n";
    }

    private void OnGUI()
    {
        if (!inited)
        {
            return;
        }

        if (GUI.Button(new Rect(10, 10, 120, 50), "Test1"))
        {
            logText = string.Empty;
            LuaSvr.MainState.GetFunction("test1").Call();
        }

        if (GUI.Button(new Rect(10, 100, 120, 50), "Test2"))
        {
            logText = string.Empty;
            LuaSvr.MainState.GetFunction("test2").Call();
        }

        if (GUI.Button(new Rect(10, 200, 120, 50), "Test3"))
        {
            logText = string.Empty;
            LuaSvr.MainState.GetFunction("test3").Call();
        }

        if (GUI.Button(new Rect(10, 300, 120, 50), "Test4"))
        {
            logText = string.Empty;
            LuaSvr.MainState.GetFunction("test4").Call();
        }

        if (GUI.Button(new Rect(200, 10, 120, 50), "Test5"))
        {
            logText = string.Empty;
            LuaSvr.MainState.GetFunction("test5").Call();
        }

        if (GUI.Button(new Rect(200, 100, 120, 50), "Test6 jit"))
        {
            logText = string.Empty;
            LuaSvr.MainState.GetFunction("test6").Call();
        }

        if (GUI.Button(new Rect(200, 200, 120, 50), "Test6 non-jit"))
        {
            logText = string.Empty;
            LuaSvr.MainState.GetFunction("test7").Call();
        }

        GUI.Label(new Rect(400, 200, 300, 50), logText);
    }
}
