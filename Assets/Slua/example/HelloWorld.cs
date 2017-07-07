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
using System.Collections;
using System.Collections.Generic;
using SLua;
using UnityEngine;

#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

[CustomLuaClass]
public struct FooStruct
{
    public float X, Y, Z, W;
    public int Mode;
}

public static class ExtensionTest
{
    public static List<int> Result { get; private set; }

    public static List<int> Func8(this HelloWorld helloWorld)
    {
        Result = new List<int>();
        helloWorld.Func8(Result);
        return Result;
    }
}

[CustomLuaClass]
public class FloatEvent : UnityEngine.Events.UnityEvent<float>
{
}

[CustomLuaClass]
public class ListViewEvent : UnityEngine.Events.UnityEvent<int, string>
{
}

[CustomLuaClass]
public class SLuaTest : MonoBehaviour
{
    public FloatEvent IntEvent;
}

[CustomLuaClass]
public class XXList : List<int>
{
}

[CustomLuaClass]
public abstract class AbsClass
{
    public int X;

    // this constructor should not been exported for test
    public AbsClass()
    {
    }
}

public class Ref
{
    public int Depth { get; set; }
}

[CustomLuaClass]
public class HelloWorld
{
    private int b;
    [DoNotToLua]
    private int a;

    private Color32 cc;
    private UnityEngine.Events.UnityAction someAct;
    private LuaFunction f;

    [DoNotToLua]
    public int A
    {
        get
        {
            return a;
        }
    }

    public object this[string path]
    {
        get
        {
            Debug.Log("get by string key");
            return "value";
        }

        set
        {
            Debug.Log("set by string key");
        }
    }

    public object this[int index]
    {
        get
        {
            Debug.Log("get by int key");
            return "int value";
        }

        set
        {
            Debug.Log("set by int key");
        }
    }

    [DoNotToLua]
    public static void DontExport()
    {
    }

    public static void Say()
    {
        Debug.Log("hello world");
    }

    public static byte[] Bytes()
    {
        return new byte[] { 51, 52, 53, 53 };
    }

    public static void Int16Array(short[] array)
    {
        foreach (short i in array)
        {
            Debug.Log("output int16 " + i);
        }
    }

    public static Vector3[] Vectors()
    {
        return new Vector3[] { Vector3.one, Vector3.zero, Vector3.up };
    }

    public static void NullF(int? a = null)
    {
        Debug.Log(a.HasValue ? a.ToString() : "null");
    }

    public static void SetV(LuaTable t)
    {
        Debug.Log("negative index test " + t[-2]);
        Debug.Log("zero index test " + t[0]);

        foreach (LuaTable.TablePair pair in t)
        {
            Debug.Log(string.Format("foreach LuaTable {0}-{1}", pair.Key, pair.Value));
            break;
        }

        IEnumerator<LuaTable.TablePair> iter = t.GetEnumerator();
        while (iter.MoveNext())
        {
            LuaTable.TablePair pair = iter.Current;
            Debug.Log(string.Format("foreach LuaTable {0}-{1}", pair.Key, pair.Value));
            break;
        }

        iter.Dispose();
    }

    public static int GetNegInt()
    {
        return -1;
    }

    public static LuaTable GetV()
    {
        LuaTable t = new LuaTable(LuaState.Main);
        t["name"] = "xiaoming";
        t[1] = "a";
        t[2] = "b";

        t["xxx"] = new LuaTable(LuaState.Main);
        ((LuaTable)t["xxx"])["yyy"] = 1;
        return t;
    }

    public static void OFunc(Type t)
    {
        Debug.Log(t.Name);
    }

    public static void OFunc(GameObject go)
    {
        Debug.Log(go.name);
    }

    public static void AFunc(int a)
    {
        Debug.Log("AFunc with int");
    }

    public static void AFunc(float a)
    {
        Debug.Log("AFunc with float");
    }

    public static void AFunc(string a)
    {
        Debug.Log("AFunc with string");
    }

    [LuaOverride("AFuncByDouble")]
    public static void AFunc(double a)
    {
        Debug.Log("AFunc with double");
    }

    public static void TestVec3(Vector3 v)
    {
        Debug.Log(string.Format("vec3 {0},{1},{2}", v.x, v.y, v.z));
    }

    public static void TestSet(GameObject go)
    {
        Transform t = go.transform;
        for (int i = 0; i < 200000; i++)
        {
            t.position = t.position;
        }
    }

    public static void Test2(GameObject go)
    {
        Vector3 v = Vector3.one;
        for (int i = 0; i < 200000; i++)
        {
            v.Normalize();
        }
    }

    public static void Test3(GameObject go)
    {
        Vector3 v = Vector3.one;
        for (int i = 0; i < 200000; i++)
        {
            v = Vector3.Normalize(v);
        }
    }

    public static void Test4(GameObject go)
    {
        Vector3 v = Vector3.one;
        Transform t = go.transform;
        for (int i = 0; i < 200000; i++)
        {
            t.position = v;
        }
    }

    public static Vector3 Test5(GameObject go)
    {
        Vector3 v = Vector3.zero;
        for (int i = 0; i < 200000; i++)
        {
            v = new Vector3(i, i, i);
        }

        return v;
    }

    public static void ByteArrayTest()
    {
        ByteArray ba = new ByteArray();
        ba.WriteLongInt(1L);
        ba.WriteLongInt(2L);
        ba.WriteLongInt(1024L);
        ba.Position = 0;
        Assert.IsTrue(ba.ReadLongInt() == 1L);
        Assert.IsTrue(ba.ReadLongInt() == 2L);
        Assert.IsTrue(ba.ReadLongInt() == 1024L);
    }

    public static void TransformArray(Transform[] arr)
    {
        Debug.Log("transformArray success.");
    }

    // test variant number for arguments passed in
    public static void Func6(string str, params object[] args)
    {
        Debug.Log(str);
        for (int n = 0; n < args.Length; n++)
        {
            Debug.Log(args[n]);
        }
    }

    public IEnumerator Y()
    {
        WWW www = new WWW("http://news.163.com");
        yield return www;
        Debug.Log("yield good");
    }

    public Dictionary<string, GameObject> Foo()
    {
        return new Dictionary<string, GameObject>();
    }

    public Dictionary<string, GameObject>[] Foos()
    {
        return new Dictionary<string, GameObject>[] { };
    }

    public int Gos(Dictionary<string, GameObject>[] x)
    {
        return x.Length;
    }

    public Dictionary<GameObject, string> Too()
    {
        return new Dictionary<GameObject, string>();
    }

    public List<GameObject> GetList()
    {
        return new List<GameObject> { new GameObject("1"), new GameObject("2") };
    }

    // this function exported, but get LuaObject.Error to call
    // generic function not support
    public void Generic<T>()
    {
        Debug.Log(typeof(T).Name);
    }

    public void Perf()
    {
        Profiler.BeginSample("create 1000000 vector3/cs");
        for (int n = 0; n < 1000000; n++)
        {
            Vector3 v = new Vector3(n, n, n);
            v.Normalize();
        }

        Profiler.EndSample();
    }

    public void Func7(LuaFunction func)
    {
        f = func;
        f.Call();
    }

    public void Func7(int a)
    {
        Debug.Log(a);
    }

    public void Func8(List<int> result)
    {
        result.Add(1);
    }
}
