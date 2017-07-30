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
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    [SerializeField]
    private Text logText;
    private LuaSvr l;
    private int progress = 0;

    // Use this for initialization
    public void Start()
    {
#if UNITY_5
        Application.logMessageReceived += this.Log;
#else
        Application.RegisterLogCallback(this.log);
#endif
        l = new LuaSvr();
        l.Init(Tick, Complete, LuaSvrFlag.LSF_BASIC | LuaSvrFlag.LSF_EXTLIB);
    }

    public void Log(string cond, string trace, LogType lt)
    {
        logText.text += cond + "\n";
    }

    public void Tick(int p)
    {
        progress = p;
    }

    public void Complete()
    {
        l.Start("main");
        object o = LuaSvr.MainState.GetFunction("foo").Call(1, 2, 3);
        object[] array = (object[])o;
        for (int n = 0; n < array.Length; n++)
        {
            Debug.Log(array[n]);
        }

        string s = (string)LuaSvr.MainState.GetFunction("str").Call(new object[0]);
        Debug.Log(s);
    }

    public void OnGUI()
    {
        if (progress != 100)
        {
            GUI.Label(new Rect(0, 0, 100, 50), string.Format("Loading {0}%", progress));
        }
    }
}
