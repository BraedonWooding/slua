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

// uncomment this will use static binder(class BindCustom/BindUnity), 
// init will not use reflection to speed up the speed
// #define USE_STATIC_BINDER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SLua
{
    public enum LuaSvrFlag
    {
        LSF_BASIC,
        LSF_EXTLIB,
        LSF_3RDDLL,
    }

    public class LuaSvr
    {
        protected static LuaSvrGameObject lgo;
        protected int errorReported = 0;

        public LuaSvr()
        {
            LuaState luaState = new LuaState();
            this.LUAState = luaState;
        }

        public bool Initialized { get; private set; }

        public LuaState LUAState { get; private set; }

        public object Start(string main)
        {
            if (main != null)
            {
                LUAState.DoFile(main);
                using (LuaFunction func = (LuaFunction)LUAState["main"])
                {
                    if (func != null)
                    {
                        return func.Call();
                    }
                }
            }

            return null;
        }

        public void Tick()
        {
            if (!Initialized)
            {
                return;
            }

            if (LuaNativeMethods.lua_gettop(LUAState.StatePointer) != errorReported)
            {
                errorReported = LuaNativeMethods.lua_gettop(LUAState.StatePointer);
                Logger.LogError(string.Format("Some function not remove temp value({0}) from lua stack. You should fix it.", LuaNativeMethods.luaL_typename(LUAState.StatePointer, errorReported)));
            }

            LUAState.CheckRef();
            LuaTimer.Tick(Time.deltaTime);
        }

        public void Init(Action<int> tick, Action complete, LuaSvrFlag flag = LuaSvrFlag.LSF_BASIC | LuaSvrFlag.LSF_EXTLIB)
        {
            if (lgo == null)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlaying)
                {
                    GameObject go = new GameObject("LuaSvrProxy");
                    lgo = go.AddComponent<LuaSvrGameObject>();
                    GameObject.DontDestroyOnLoad(go);
                }
#else
                GameObject go = new GameObject("LuaSvrProxy");
                lgo = go.AddComponent<LuaSvrGameObject>();
                GameObject.DontDestroyOnLoad(go);
#endif
            }

            IntPtr ptr = LUAState.StatePointer;
            LuaObject.Init(ptr);

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                DoBind(ptr);
                DoInit(ptr, flag);
                complete();
                CheckTop(ptr);
            }
            else
            {
#endif
                lgo.StartCoroutine(DoBind(ptr, tick, () =>
                    {
                        DoInit(ptr, flag);
                        complete();
                        CheckTop(ptr);
                    }));
#if UNITY_EDITOR
            }
#endif
        }

        public List<Action<IntPtr>> CollectBindInfo()
        {
            List<Action<IntPtr>> list = new List<Action<IntPtr>>();

#if !USE_STATIC_BINDER
            Assembly[] ams = AppDomain.CurrentDomain.GetAssemblies();

            List<Type> bindlist = new List<Type>();
            for (int n = 0; n < ams.Length; n++)
            {
                Assembly a = ams[n];
                Type[] ts = null;
                try
                {
                    ts = a.GetExportedTypes();
                }
                catch
                {
                    continue;
                }

                for (int k = 0; k < ts.Length; k++)
                {
                    Type t = ts[k];
                    if (t.IsDefined(typeof(LuaBinderAttribute), false))
                    {
                        bindlist.Add(t);
                    }
                }
            }

            bindlist.Sort(new System.Comparison<Type>((Type a, Type b) =>
            {
                LuaBinderAttribute la = System.Attribute.GetCustomAttribute(a, typeof(LuaBinderAttribute)) as LuaBinderAttribute;
                LuaBinderAttribute lb = System.Attribute.GetCustomAttribute(b, typeof(LuaBinderAttribute)) as LuaBinderAttribute;

                return la.Order.CompareTo(lb.Order);
            }));

            for (int n = 0; n < bindlist.Count; n++)
            {
                Type t = bindlist[n];
                Action<IntPtr>[] sublist = (Action<IntPtr>[])t.GetMethod("GetBindList").Invoke(null, null);
                list.AddRange(sublist);
            }
#else
            var assemblyName = "Assembly-CSharp";
            Assembly assembly = Assembly.Load(assemblyName);
            list.AddRange(getBindList(assembly,"SLua.BindUnity"));
            list.AddRange(getBindList(assembly,"SLua.BindUnityUI"));
            list.AddRange(getBindList(assembly,"SLua.BindDll"));
            list.AddRange(getBindList(assembly,"SLua.BindCustom"));
#endif
            return list;
        }

        protected void DoBind(IntPtr ptr)
        {
            List<Action<IntPtr>> list = CollectBindInfo();

            int count = list.Count;
            for (int n = 0; n < count; n++)
            {
                Action<IntPtr> action = list[n];
                action(ptr);
            }
        }

        protected Action<IntPtr>[] GetBindList(Assembly assembly, string ns)
        {
            Type t = assembly.GetType(ns);
            if (t != null)
            {
                return (Action<IntPtr>[])t.GetMethod("GetBindList").Invoke(null, null);
            }

            return new Action<IntPtr>[0];
        }

        protected void DoInit(IntPtr ptr, LuaSvrFlag flag)
        {
            LuaTimer.Register(ptr);
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
            {
                LuaCoroutine.Register(ptr, lgo);
            }
#else
            Lua_SLua_ByteArray.reg(ptr);
#endif
            Helper.Register(ptr);
            LuaValueType.Register(ptr);
            if ((flag & LuaSvrFlag.LSF_EXTLIB) != 0)
            {
                LuaNativeMethods.luaS_openextlibs(ptr);
                LuaSocketMini.Register(ptr);
            }

            if ((flag & LuaSvrFlag.LSF_3RDDLL) != 0)
            {
                Lua3rdDLL.Open(ptr);
            }

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
            {
#endif
                lgo.State = LUAState;
                lgo.OnUpdate = this.Tick;
                lgo.Init();
#if UNITY_EDITOR
            }
#endif
            Initialized = true;
        }

        protected void CheckTop(IntPtr ptr)
        {
            if (LuaNativeMethods.lua_gettop(LUAState.StatePointer) != errorReported)
            {
                Logger.LogError("Some function not remove temp value from lua stack. You should fix it.");
                errorReported = LuaNativeMethods.lua_gettop(LUAState.StatePointer);
            }
        }

        private IEnumerator DoBind(IntPtr ptr, Action<int> tickAction, Action complete)
        {
            Action<int> tick = (int p) =>
            {
                if (tickAction != null)
                {
                    tickAction(p);
                }
            };

            tick(0);
            List<Action<IntPtr>> list = CollectBindInfo();
            tick(2);

            int bindProgress = 2;
            int lastProgress = bindProgress;
            for (int n = 0; n < list.Count; n++)
            {
                Action<IntPtr> action = list[n];
                action(ptr);
                bindProgress = (int)(((float)n / list.Count) * 98.0) + 2;
                if (tickAction != null && lastProgress != bindProgress && bindProgress % 5 == 0)
                {
                    tick(bindProgress);
                    yield return null;
                }
            }

            tick(100);
            complete();
        }
    }
}
