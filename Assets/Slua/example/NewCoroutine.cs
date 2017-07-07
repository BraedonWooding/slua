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

using System.Collections;
using SLua;
using UnityEngine;

[CustomLuaClass]
public class NewCoroutine : MonoBehaviour
{
    public static IEnumerator MyMethod(string test, float time, LuaFunction func)
    {
        Debug.Log(test);
        yield return new WaitForSeconds(time);
        func.Call();
    }

    public void Start()
    {
        LuaSvr svr = new LuaSvr();
        svr.Init(null, () =>
        {
            LuaFunction func = (LuaFunction)svr.Start("new_coroutine");
            func.Call(this);
            func.Dispose();
        });
    }
}
