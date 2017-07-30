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

using UnityEngine;
using SLua;

public class MultiState : MonoBehaviour
{
    private LuaSvr svr;
    private LuaState[] ls = new LuaState[10];

    private void Start()
    {
        svr = new LuaSvr();
        svr.Init(null, OnComplete);
    }

    private void OnComplete()
    {
        // Create 10 states
        for (int i = 0; i < 10; i++)
        {
            ls[i] = new LuaState();
            ls[i].DoString(string.Format("print('this is #{0} lua state')", i));
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < 10; i++)
        {
            ls[i].Dispose();
        }
    }
}
