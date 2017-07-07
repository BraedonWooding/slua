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
using SLua;
using UnityEngine;

namespace SLua
{
    [OverloadLuaClass(typeof(GameObject))]
    public class MyGameObject : LuaObject
    {
        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Find_s(IntPtr ptr)
        {
            UnityEngine.Debug.Log("GameObject.Find overloaded my MyGameObject.Find");
            try
            {
                string a1;
                CheckType(ptr, 1, out a1);
                GameObject ret = UnityEngine.GameObject.Find(a1);
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }
    }
}

[CustomLuaClass]
public class Custom : MonoBehaviour
{
    private static Custom c;
    private static string vs = "xiaoming & hanmeimei";
    private LuaSvr l;
    private int v = 520;

    public int this[string key]
    {
        get
        {
            return key == "test" ? v : 0;
        }

        set
        {
            if (key == "test")
            {
                v = value;
            }
        }
    }

    // this exported function don't generate stub code if it had MonoPInvokeCallbackAttribute attribute, only register it
    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    public static int InstanceCustom(IntPtr ptr)
    {
        Custom self = (Custom)LuaObject.CheckSelf(ptr);
        LuaObject.PushValue(ptr, true);
        LuaNativeMethods.lua_pushstring(ptr, "xiaoming");
        LuaNativeMethods.lua_pushstring(ptr, "hanmeimei");
        LuaNativeMethods.lua_pushinteger(ptr, self.v);
        return 4;
    }

    // this exported function don't generate stub code, only register it
    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    [StaticExport]
    public static int StaticCustom(IntPtr ptr)
    {
        LuaObject.PushValue(ptr, true);
        LuaNativeMethods.lua_pushstring(ptr, vs);
        LuaObject.PushObject(ptr, c);
        return 3;
    }

    public string GetTypeName(Type t)
    {
        return t.Name;
    }

    public void Start()
    {
        c = this;
        l = new LuaSvr();
        l.Init(null, () =>
        {
            l.Start("custom");
        });
    }
}
