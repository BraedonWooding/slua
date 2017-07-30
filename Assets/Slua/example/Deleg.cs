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
using System.Collections.Generic;
using SLua;
using UnityEngine;

[CustomLuaClass]
public class Deleg : MonoBehaviour
{
    // This is edited, through LUA
#pragma warning disable 0649 // Disable not changed warning
    private static Action<int, Dictionary<int, object>> daction;
#pragma warning restore 0649

    private static GetBundleInfoDelegate bundleInfoDelegate;
    private static SimpleDelegate simpleDelegate1;

    private LuaSvr l;

    public delegate bool GetBundleInfoDelegate(string a1, int a2, int a3, ref int a4, out string a5, out int a6);

    public delegate void SimpleDelegate(string path, GameObject g);

    public static GetBundleInfoDelegate BundleInfo
    {
        get
        {
            return bundleInfoDelegate;
        }

        set
        {
            bundleInfoDelegate = value;
        }
    }

    public static SimpleDelegate SimpleDelegate1
    {
        get
        {
            return simpleDelegate1;
        }

        set
        {
            simpleDelegate1 = value;
        }
    }

    public static void CallID()
    {
        string url;
        int ver;
        int c = 3;
        if (bundleInfoDelegate != null)
        {
            bool ret = bundleInfoDelegate("/path", 1, 2, ref c, out url, out ver);
            Debug.Log(string.Format("{0},{1},{2}", ret, url, ver));
            Debug.Assert(c == 4, "C == 4");
            Debug.Assert(url == "http://www.sineysoft.com", "url == http://www.sinevsoft.com");
            Debug.Assert(ver == 1, "ver == 1");
        }

        if (SimpleDelegate1 != null)
        {
            SimpleDelegate1("GameObject", new GameObject("SimpleDelegate"));
        }
    }

    public static void SetCallback2(Action<int> a, Action<string> b)
    {
        if (a != null)
        {
            a(1);
        }

        if (b != null)
        {
            b("hello");
        }
    }

    public static void TestFunction(Func<int> f)
    {
        Debug.Log(string.Format("Func return {0}", f()));
    }

    public static void TestAction(Action<int, string> f)
    {
        f(1024, "caoliu");
    }

    public static void TestDAction(Action<int, Dictionary<int, object>> f)
    {
        f(1024, new Dictionary<int, object>());
    }

    public static void CallDAction()
    {
        if (daction != null)
        {
            daction(2048, new Dictionary<int, object>());
        }
    }

    public static Func<int, string, bool> GetFunc(Func<int, string, bool> f)
    {
        return f;
    }

    // Use this for initialization
    public void Start()
    {
        l = new LuaSvr();
        l.Init(null, () =>
        {
            l.Start("delegate");
        });
    }
}
