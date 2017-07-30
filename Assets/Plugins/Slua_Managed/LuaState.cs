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
using System.Text;
using UnityEngine;

namespace SLua
{
    public abstract class LuaVar : IDisposable
    {
        protected LuaState state = null;
        protected int valueref = 0;

        public LuaVar()
        {
            state = null;
        }

        public LuaVar(LuaState l, int r)
        {
            state = l;
            valueref = r;
        }

        public LuaVar(IntPtr ptr, int r)
        {
            state = LuaState.Get(ptr);
            valueref = r;
        }

        ~LuaVar()
        {
            Dispose(false);
        }

        public IntPtr VariablePointer
        {
            get
            {
                return state.StatePointer;
            }
        }

        public int Reference
        {
            get
            {
                return valueref;
            }
        }

        public static bool operator ==(LuaVar x, LuaVar y)
        {
            if ((object)x == null || (object)y == null)
            {
                return (object)x == (object)y;
            }

            return Equals(x, y) == 1;
        }

        public static bool operator !=(LuaVar x, LuaVar y)
        {
            if ((object)x == null || (object)y == null)
            {
                return (object)x != (object)y;
            }

            return Equals(x, y) != 1;
        }

        public static int Equals(LuaVar lhs, LuaVar rhs)
        {
            // TODO: CHECK IF THIS IS ACTUALLY CORRECT?
            lhs.Push(lhs.VariablePointer);
            rhs.Push(lhs.VariablePointer);
            int ok = LuaNativeMethods.lua_equal(lhs.VariablePointer, -1, -2);
            LuaNativeMethods.lua_pop(lhs.VariablePointer, 2);
            return ok;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposeManagedResources)
        {
            if (valueref != 0)
            {
                LuaState.UnRefAction act = (IntPtr ptr, int r) =>
                {
                    LuaNativeMethods.lua_unref(ptr, r);
                };
                state.GCRef(act, valueref);
                valueref = 0;
            }
        }

        public void Push(IntPtr ptr)
        {
            LuaNativeMethods.lua_getref(ptr, valueref);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is LuaVar)
            {
                return this == (LuaVar)obj;
            }

            return false;
        }
    }

    public class LuaThread : LuaVar
    {
        public LuaThread(IntPtr ptr, int r) : base(ptr, r)
        {
        }
    }

    public class LuaDelegate : LuaFunction
    {
        public LuaDelegate(IntPtr ptr, int r) : base(ptr, r)
        {
        }

        public object Delegate { get; set; }

        public override void Dispose(bool disposeManagedResources)
        {
            if (valueref != 0)
            {
                LuaState.UnRefAction act = (IntPtr ptr, int r) =>
                {
                    LuaObject.RemoveDelgate(ptr, r);
                    LuaNativeMethods.lua_unref(ptr, r);
                };
                state.GCRef(act, valueref);
                valueref = 0;
            }
        }
    }

    public class LuaFunction : LuaVar
    {
        public LuaFunction(LuaState state, int r) : base(state, r)
        {
        }

        public LuaFunction(IntPtr ptr, int r) : base(ptr, r)
        {
        }

        public bool ProtectedCall(int args, int errfunc)
        {
            if (!state.IsMainThread())
            {
                Logger.LogError("Can't call lua function in bg thread");
                return false;
            }

            LuaNativeMethods.lua_getref(VariablePointer, valueref);

            if (!LuaNativeMethods.lua_isfunction(VariablePointer, -1))
            {
                LuaNativeMethods.lua_pop(VariablePointer, 1);
                throw new Exception("Call invalid function.");
            }

            LuaNativeMethods.lua_insert(VariablePointer, -args - 1);
            if (LuaNativeMethods.lua_pcall(VariablePointer, args, -1, errfunc) != 0)
            {
                LuaNativeMethods.lua_pop(VariablePointer, 1);
                return false;
            }

            return true;
        }

        public bool InnerCall(int args, int errfunc)
        {
            bool ret = ProtectedCall(args, errfunc);
            LuaNativeMethods.lua_remove(VariablePointer, errfunc);
            return ret;
        }

        public object Call()
        {
            int error = LuaObject.PushTry(state.StatePointer);

            if (InnerCall(0, error))
            {
                return state.TopObjects(error - 1);
            }

            return null;
        }

        public object Call(params object[] args)
        {
            int error = LuaObject.PushTry(state.StatePointer);

            for (int n = 0; args != null && n < args.Length; n++)
            {
                LuaObject.PushVar(VariablePointer, args[n]);
            }

            if (InnerCall(args != null ? args.Length : 0, error))
            {
                return state.TopObjects(error - 1);
            }

            return null;
        }

        public object Call(object a1)
        {
            int error = LuaObject.PushTry(state.StatePointer);
            LuaObject.PushVar(state.StatePointer, a1);

            if (InnerCall(1, error))
            {
                return state.TopObjects(error - 1);
            }

            return null;
        }

        public object Call(LuaTable self, params object[] args)
        {
            int error = LuaObject.PushTry(state.StatePointer);
            LuaObject.PushVar(VariablePointer, self);

            for (int n = 0; args != null && n < args.Length; n++)
            {
                LuaObject.PushVar(VariablePointer, args[n]);
            }

            if (InnerCall((args != null ? args.Length : 0) + 1, error))
            {
                return state.TopObjects(error - 1);
            }

            return null;
        }

        public object Call(object a1, object a2)
        {
            int error = LuaObject.PushTry(state.StatePointer);
            LuaObject.PushVar(state.StatePointer, a1);
            LuaObject.PushVar(state.StatePointer, a2);

            if (InnerCall(2, error))
            {
                return state.TopObjects(error - 1);
            }

            return null;
        }

        public object Call(object a1, object a2, object a3)
        {
            int error = LuaObject.PushTry(state.StatePointer);

            LuaObject.PushVar(state.StatePointer, a1);
            LuaObject.PushVar(state.StatePointer, a2);
            LuaObject.PushVar(state.StatePointer, a3);
            if (InnerCall(3, error))
            {
                return state.TopObjects(error - 1);
            }

            return null;
        }

        // you can add call method with specific type rather than object type to avoid gc alloc, like
        // public object call(int a1,float a2,string a3,object a4)

        // using specific type to avoid type boxing/unboxing
    }

    public class LuaTable : LuaVar, IEnumerable<LuaTable.TablePair>
    {
        public LuaTable(IntPtr ptr, int r) : base(ptr, r)
        {
        }

        public LuaTable(LuaState state, int r) : base(state, r)
        {
        }

        public LuaTable(LuaState state) : base(state, 0)
        {
            LuaNativeMethods.lua_newtable(VariablePointer);
            valueref = LuaNativeMethods.luaL_ref(VariablePointer, LuaIndexes.LUARegistryIndex);
        }

        public bool IsEmpty
        {
            get
            {
                int top = LuaNativeMethods.lua_gettop(VariablePointer);
                LuaNativeMethods.lua_getref(VariablePointer, this.Reference);
                LuaNativeMethods.lua_pushnil(VariablePointer);
                bool ret = LuaNativeMethods.lua_next(VariablePointer, -2) > 0;
                LuaNativeMethods.lua_settop(VariablePointer, top);
                return !ret;
            }
        }

        public object this[string key]
        {
            get
            {
                return state.GetObject(valueref, key);
            }

            set
            {
                state.SetObject(valueref, key, value);
            }
        }

        public object this[int index]
        {
            get
            {
                return state.GetObject(valueref, index);
            }

            set
            {
                state.SetObject(valueref, index, value);
            }
        }

        public object Invoke(string func, params object[] args)
        {
            using (LuaFunction function = (LuaFunction)this[func])
            {
                if (function != null)
                {
                    return function.Call(args);
                }
            }

            throw new Exception(string.Format("Can't find {0} function", func));
        }

        public int Length()
        {
            int n = LuaNativeMethods.lua_gettop(VariablePointer);
            Push(VariablePointer);
            int l = LuaNativeMethods.lua_rawlen(VariablePointer, -1);
            LuaNativeMethods.lua_settop(VariablePointer, n);
            return l;
        }

        public IEnumerator<TablePair> GetEnumerator()
        {
            return new LuaTable.Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct TablePair
        {
            public TablePair(object key, object value)
            {
                Key = key;
                Value = value;
            }

            public object Key { get; private set; }

            public object Value { get; private set; }
        }

        public class Enumerator : IEnumerator<TablePair>, IDisposable
        {
            private LuaTable table;
            private int tableIndex = -1;
            private int iterPhase = 0;

            public Enumerator(LuaTable table)
            {
                this.table = table;
                Reset();
            }

            public TablePair Current
            {
                get
                {
                    return new TablePair(LuaObject.CheckVar(table.VariablePointer, -2), LuaObject.CheckVar(table.VariablePointer, -1));
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public bool MoveNext()
            {
                if (tableIndex < 0)
                {
                    return false;
                }

                if (iterPhase == 0)
                {
                    LuaNativeMethods.lua_pushnil(table.VariablePointer);
                    iterPhase = 1;
                }
                else
                {
                    LuaNativeMethods.lua_pop(table.VariablePointer, 1);
                }

                // var ty = LuaDLL.lua_type(t.L, -1);
                bool ret = LuaNativeMethods.lua_next(table.VariablePointer, tableIndex) > 0;
                if (!ret)
                {
                    iterPhase = 2;
                }

                return ret;
            }

            public void Reset()
            {
                LuaNativeMethods.lua_getref(table.VariablePointer, table.Reference);
                tableIndex = LuaNativeMethods.lua_gettop(table.VariablePointer);
            }

            public void Dispose()
            {
                if (iterPhase == 1)
                {
                    LuaNativeMethods.lua_pop(table.VariablePointer, 2);
                }

                LuaNativeMethods.lua_remove(table.VariablePointer, tableIndex);
            }
        }
    }

    public class LuaState : IDisposable
    {
        public WeakDictionary<int, LuaDelegate> DelegateMap = new WeakDictionary<int, LuaDelegate>();
        public LuaSvrGameObject gameObject;

        private static Dictionary<IntPtr, LuaState> statemap = new Dictionary<IntPtr, LuaState>();
        private static IntPtr oldptr = IntPtr.Zero;
        private static LuaState oldstate = null;
        private static StringBuilder s = new StringBuilder();
        private static LuaCSFunction errorFunc = new LuaCSFunction(ErrorReport);

        private Queue<UnrefPair> refQueue;
        private int callCSFunctionRef = 0;
        private Dictionary<Type, PushVarDelegate> typePushMap = new Dictionary<Type, PushVarDelegate>();
        private int errorRef = 0;
        private IntPtr statePointer;
        private int mainThread = 0;

        public LuaState()
        {
            if (mainThread == 0)
            {
                mainThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            }

            statePointer = LuaNativeMethods.luaL_newstate();
            statemap[statePointer] = this;

            if (Main == null)
            {
                Main = this;
            }

            refQueue = new Queue<UnrefPair>();
            ObjectCache.Make(statePointer);

            LuaNativeMethods.lua_atpanic(statePointer, PanicCallback);

            LuaNativeMethods.luaL_openlibs(statePointer);

            string callCSFunction = @"
local assert = assert
local function check(ok,...)
    assert(ok, ...)
    return ...
end
return function(cs_func)
    return function(...)
        return check(cs_func(...))
    end
end
";

            LuaNativeMethods.lua_dostring(statePointer, callCSFunction);
            callCSFunctionRef = LuaNativeMethods.luaL_ref(statePointer, LuaIndexes.LUARegistryIndex);
            SetupPushVar();
            ProtectedCall(statePointer, Init);
        }

        public delegate byte[] LoaderDelegate(string fn);

        public delegate void UnRefAction(IntPtr ptr, int r);

        public delegate void OutputDelegate(string msg);

        public delegate void PushVarDelegate(IntPtr ptr, object o);

        public static LoaderDelegate LoaderEvent { get; set; }

        public static OutputDelegate LogEvent { get; set; }

        public static OutputDelegate ErrorEvent { get; set; }

        public static LuaState Main { get; private set; }

        public IntPtr StatePointer
        {
            get
            {
                if (!IsMainThread())
                {
                    Logger.LogError("Can't access lua in background thread");
                    throw new Exception("Can't access lua in background thread");
                }

                if (statePointer == IntPtr.Zero)
                {
                    Logger.LogError("LuaState had been destroyed, can't used yet");
                    throw new Exception("LuaState had been destroyed, can't used yet");
                }

                return statePointer;
            }

            set
            {
                statePointer = value;
            }
        }

        public IntPtr Handle
        {
            get
            {
                return statePointer;
            }
        }

        public int Top
        {
            get
            {
                return LuaNativeMethods.lua_gettop(statePointer);
            }
        }

        public object this[string path]
        {
            get
            {
                return this.GetObject(path);
            }

            set
            {
                this.SetObject(path, value);
            }
        }

        public static LuaState Get(IntPtr ptr)
        {
            if (ptr == oldptr)
            {
                return oldstate;
            }

            LuaState ls;
            if (statemap.TryGetValue(ptr, out ls))
            {
                oldptr = ptr;
                oldstate = ls;
                return ls;
            }

            LuaNativeMethods.lua_getglobal(ptr, "__main_state");
            if (LuaNativeMethods.lua_isnil(ptr, -1))
            {
                LuaNativeMethods.lua_pop(ptr, 1);
                return null;
            }

            IntPtr nl = LuaNativeMethods.lua_touserdata(ptr, -1);
            LuaNativeMethods.lua_pop(ptr, 1);
            if (nl != ptr)
            {
                return Get(nl);
            }

            return null;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ErrorReport(IntPtr ptr)
        {
            LuaNativeMethods.lua_getglobal(ptr, "debug");
            LuaNativeMethods.lua_getfield(ptr, -1, "traceback");
            LuaNativeMethods.lua_pushvalue(ptr, 1);
            LuaNativeMethods.lua_pushnumber(ptr, 2);
            LuaNativeMethods.lua_call(ptr, 2, 1);
            LuaNativeMethods.lua_remove(ptr, -2);
            string error = LuaNativeMethods.lua_tostring(ptr, -1);
            LuaNativeMethods.lua_pop(ptr, 1);

            Logger.LogError(error, true);
            if (ErrorEvent != null)
            {
                ErrorEvent(error);
            }

            LuaNativeMethods.lua_getglobal(ptr, "dumpstack");
            LuaNativeMethods.lua_call(ptr, 0, 0);

            return 0;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Import(IntPtr ptr)
        {
            try
            {
                LuaNativeMethods.luaL_checktype(ptr, 1, LuaTypes.TYPE_STRING);
                string str = LuaNativeMethods.lua_tostring(ptr, 1);

                string[] ns = str.Split('.');

                LuaNativeMethods.lua_pushglobaltable(ptr);

                for (int n = 0; n < ns.Length; n++)
                {
                    LuaNativeMethods.lua_getfield(ptr, -1, ns[n]);
                    if (!LuaNativeMethods.lua_istable(ptr, -1))
                    {
                        return LuaObject.Error(ptr, "expect {0} is type table", ns);
                    }

                    LuaNativeMethods.lua_remove(ptr, -2);
                }

                LuaNativeMethods.lua_pushnil(ptr);
                while (LuaNativeMethods.lua_next(ptr, -2) != 0)
                {
                    string key = LuaNativeMethods.lua_tostring(ptr, -2);
                    LuaNativeMethods.lua_getglobal(ptr, key);
                    if (!LuaNativeMethods.lua_isnil(ptr, -1))
                    {
                        LuaNativeMethods.lua_pop(ptr, 1);
                        return LuaObject.Error(ptr, "{0} had existed, import can't overload it.", key);
                    }

                    LuaNativeMethods.lua_pop(ptr, 1);
                    LuaNativeMethods.lua_setglobal(ptr, key);
                }

                LuaNativeMethods.lua_pop(ptr, 1);

                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ProtectedCall(IntPtr ptr)
        {
            int status;
            if (LuaNativeMethods.lua_type(ptr, 1) != LuaTypes.TYPE_FUNCTION)
            {
                return LuaObject.Error(ptr, "arg 1 expect function");
            }

            LuaNativeMethods.luaL_checktype(ptr, 1, LuaTypes.TYPE_FUNCTION);
            status = LuaNativeMethods.lua_pcall(ptr, LuaNativeMethods.lua_gettop(ptr) - 1, LuaNativeMethods.LUAMultRet, 0);
            LuaNativeMethods.lua_pushboolean(ptr, status == 0);
            LuaNativeMethods.lua_insert(ptr, 1);
            return LuaNativeMethods.lua_gettop(ptr);  /* return status + all results */
        }

        public static void ProtectedCall(IntPtr ptr, LuaCSFunction f)
        {
            int err = LuaObject.PushTry(ptr);
            LuaNativeMethods.lua_pushcfunction(ptr, f);
            if (LuaNativeMethods.lua_pcall(ptr, 0, 0, err) != 0)
            {
                LuaNativeMethods.lua_pop(ptr, 1);
            }

            LuaNativeMethods.lua_remove(ptr, err);
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Print(IntPtr ptr)
        {
            int n = LuaNativeMethods.lua_gettop(ptr);
            s.Length = 0;

            LuaNativeMethods.lua_getglobal(ptr, "tostring");

            for (int i = 1; i <= n; i++)
            {
                if (i > 1)
                {
                    s.Append("    ");
                }

                LuaNativeMethods.lua_pushvalue(ptr, -1);
                LuaNativeMethods.lua_pushvalue(ptr, i);

                LuaNativeMethods.lua_call(ptr, 1, 1);
                s.Append(LuaNativeMethods.lua_tostring(ptr, -1));
                LuaNativeMethods.lua_pop(ptr, 1);
            }

            LuaNativeMethods.lua_settop(ptr, n);

            LuaNativeMethods.lua_getglobal(ptr, "debug");
            LuaNativeMethods.lua_getfield(ptr, -1, "traceback");
            LuaNativeMethods.lua_call(ptr, 0, 1);
            s.Append("\n");
            s.Append(LuaNativeMethods.lua_tostring(ptr, -1));
            LuaNativeMethods.lua_pop(ptr, 1);
            Logger.Log(s.ToString(), true);

            if (LogEvent != null)
            {
                LogEvent(s.ToString());
            }

            return 0;
        }

        // copy from print()
        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int PrintError(IntPtr ptr)
        {
            int n = LuaNativeMethods.lua_gettop(ptr);
            s.Length = 0;

            LuaNativeMethods.lua_getglobal(ptr, "tostring");

            for (int i = 1; i <= n; i++)
            {
                if (i > 1)
                {
                    s.Append("    ");
                }

                LuaNativeMethods.lua_pushvalue(ptr, -1);
                LuaNativeMethods.lua_pushvalue(ptr, i);

                LuaNativeMethods.lua_call(ptr, 1, 1);
                s.Append(LuaNativeMethods.lua_tostring(ptr, -1));
                LuaNativeMethods.lua_pop(ptr, 1);
            }

            LuaNativeMethods.lua_settop(ptr, n);

            LuaNativeMethods.lua_getglobal(ptr, "debug");
            LuaNativeMethods.lua_getfield(ptr, -1, "traceback");
            LuaNativeMethods.lua_call(ptr, 0, 1);
            s.Append("\n");
            s.Append(LuaNativeMethods.lua_tostring(ptr, -1));
            LuaNativeMethods.lua_pop(ptr, 1);
            Logger.LogError(s.ToString(), true);

            if (ErrorEvent != null)
            {
                ErrorEvent(s.ToString());
            }

            return 0;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LoadFile(IntPtr ptr)
        {
            Loader(ptr);

            if (LuaNativeMethods.lua_isnil(ptr, -1))
            {
                string fileName = LuaNativeMethods.lua_tostring(ptr, 1);
                return LuaObject.Error(ptr, "Can't find {0}", fileName);
            }

            return 2;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int DoFile(IntPtr ptr)
        {
            int n = LuaNativeMethods.lua_gettop(ptr);

            Loader(ptr);
            if (!LuaNativeMethods.lua_toboolean(ptr, -2))
            {
                return 2;
            }
            else
            {
                if (LuaNativeMethods.lua_isnil(ptr, -1))
                {
                    string fileName = LuaNativeMethods.lua_tostring(ptr, 1);
                    return LuaObject.Error(ptr, "Can't find {0}", fileName);
                }

                int k = LuaNativeMethods.lua_gettop(ptr);
                LuaNativeMethods.lua_call(ptr, 0, LuaNativeMethods.LUAMultRet);
                k = LuaNativeMethods.lua_gettop(ptr);
                return k - n;
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int PanicCallback(IntPtr ptr)
        {
            string reason = string.Format("unprotected error in call to Lua API ({0})", LuaNativeMethods.lua_tostring(ptr, -1));
            throw new Exception(reason);
        }

        public static void PushCSFunction(IntPtr ptr, LuaCSFunction function)
        {
            LuaNativeMethods.lua_getref(ptr, Get(ptr).callCSFunctionRef);
            LuaNativeMethods.lua_pushcclosure(ptr, function, 0);
            LuaNativeMethods.lua_call(ptr, 1, 1);
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Loader(IntPtr ptr)
        {
            string fileName = LuaNativeMethods.lua_tostring(ptr, 1);
            byte[] bytes = LoadFile(fileName);
            if (bytes != null)
            {
                if (LuaNativeMethods.luaL_loadbuffer(ptr, bytes, bytes.Length, "@" + fileName) == 0)
                {
                    LuaObject.PushValue(ptr, true);
                    LuaNativeMethods.lua_insert(ptr, -2);
                    return 2;
                }
                else
                {
                    string errstr = LuaNativeMethods.lua_tostring(ptr, -1);
                    return LuaObject.Error(ptr, errstr);
                }
            }

            LuaObject.PushValue(ptr, true);
            LuaNativeMethods.lua_pushnil(ptr);
            return 2;
        }

        public static byte[] LoadFile(string functionName)
        {
            try
            {
                byte[] bytes;
                if (LoaderEvent != null)
                {
                    bytes = LoaderEvent(functionName);
                }
                else
                {
                    functionName = functionName.Replace(".", "/");

                    TextAsset asset = null;
#if UNITY_EDITOR
                    if (SLuaSetting.Instance.JitType == JITBUILDTYPE.none)
                    {
                        asset = (TextAsset)Resources.Load(functionName);
                    }
                    else if (SLuaSetting.Instance.JitType == JITBUILDTYPE.X86)
                    {
                        asset = (TextAsset)UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Slua/jit/jitx86/" + functionName + ".bytes");
                    }
                    else if (SLuaSetting.Instance.JitType == JITBUILDTYPE.X64)
                    {
                        asset = (TextAsset)UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Slua/jit/jitx64/" + functionName + ".bytes");
                    }
                    else if (SLuaSetting.Instance.JitType == JITBUILDTYPE.GC64)
                    {
                        asset = (TextAsset)UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Slua/jit/jitgc64/" + functionName + ".bytes");
                    }
#else
                    asset = (TextAsset)Resources.Load(fn);
#endif
                    if (asset == null)
                    {
                        return null;
                    }

                    bytes = asset.bytes;
                }

                return bytes;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Ensure remove BOM from bytes.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] CleanUTF8Bom(byte[] bytes)
        {
            if (bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                byte[] oldBytes = bytes;
                bytes = new byte[bytes.Length - 3];
                Array.Copy(oldBytes, 3, bytes, 0, bytes.Length);
            }

            return bytes;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Init(IntPtr ptr)
        {
            LuaNativeMethods.lua_pushlightuserdata(ptr, ptr);
            LuaNativeMethods.lua_setglobal(ptr, "__main_state");

            LuaNativeMethods.lua_pushcfunction(ptr, Print);
            LuaNativeMethods.lua_setglobal(ptr, "print");

            LuaNativeMethods.lua_pushcfunction(ptr, PrintError);
            LuaNativeMethods.lua_setglobal(ptr, "printerror");

            LuaNativeMethods.lua_pushcfunction(ptr, ProtectedCall);
            LuaNativeMethods.lua_setglobal(ptr, "pcall");

            PushCSFunction(ptr, Import);
            LuaNativeMethods.lua_setglobal(ptr, "import");

            string resumefunc = @"
local resume = coroutine.resume
local function check(co, ok, err, ...)
    if not ok then UnityEngine.Debug.LogError(debug.traceback(co,err)) end
    return ok, err, ...
end
coroutine.resume=function(co,...)
    return check(co, resume(co,...))
end
";

            // overload resume function for report error
            LuaState.Get(ptr).DoString(resumefunc);

            // https://github.com/pkulchenko/MobDebug/blob/master/src/mobdebug.lua#L290
            // Dump only 3 stacks, or it will return null (I don't know why)
            string dumpstackfunc = @"
local printerror=printerror
dumpstack=function()
  function vars(f)
    local dump = string.Emptystring.Empty
    local func = debug.getinfo(f, string.Emptyfstring.Empty).func
    local i = 1
    local locals = {}
    -- get locals
    while true do
      local name, value = debug.getlocal(f, i)
      if not name then break end
      if string.sub(name, 1, 1) ~= '(' then 
        dump = dump ..  string.Empty    string.Empty .. name .. string.Empty=string.Empty .. tostring(value) .. string.Empty\nstring.Empty 
      end
      i = i + 1
    end
    -- get varargs (these use negative indices)
    i = 1
    while true do
      local name, value = debug.getlocal(f, -i)
      -- `not name` should be enough, but LuaJIT 2.0.0 incorrectly reports `(*temporary)` names here
      if not name or name ~= string.Empty(*vararg)string.Empty then break end
      dump = dump ..  string.Empty    string.Empty .. name .. string.Empty=string.Empty .. tostring(value) .. string.Empty\nstring.Empty
      i = i + 1
    end
    -- get upvalues
    i = 1
    while func do -- check for func as it may be nil for tail calls
      local name, value = debug.getupvalue(func, i)
      if not name then break end
      dump = dump ..  string.Empty    string.Empty .. name .. string.Empty=string.Empty .. tostring(value) .. string.Empty\nstring.Empty
      i = i + 1
    end
    return dump
  end
  local dump = string.Emptystring.Empty
  for i = 3, 100 do
    local source = debug.getinfo(i, string.EmptySstring.Empty)
    if not source then break end
    dump = dump .. string.Empty- stackstring.Empty .. tostring(i-2) .. string.Empty\nstring.Empty
    dump = dump .. vars(i+1)
    if source.what == 'main' then break end
  end
  printerror(dump)
end
";

            LuaState.Get(ptr).DoString(dumpstackfunc);

#if UNITY_ANDROID
            // fix android performance drop with JIT on according to luajit mailist post
            LuaState.get(ptr).doString("if jit then require('jit.opt').start('sizemcode=256','maxmcode=256') for i=1,1000 do end end");
#endif

            PushCSFunction(ptr, DoFile);
            LuaNativeMethods.lua_setglobal(ptr, "dofile");

            PushCSFunction(ptr, LoadFile);
            LuaNativeMethods.lua_setglobal(ptr, "loadfile");

            PushCSFunction(ptr, Loader);
            int loaderFunc = LuaNativeMethods.lua_gettop(ptr);

            LuaNativeMethods.lua_getglobal(ptr, "package");
#if LUA_5_3
            LuaNativeMethods.lua_getfield(ptr, -1, "searchers");
#else
            LuaNativeMethods.lua_getfield(ptr, -1, "loaders");
#endif
            int loaderTable = LuaNativeMethods.lua_gettop(ptr);

            // Shift table elements right
            for (int e = LuaNativeMethods.lua_rawlen(ptr, loaderTable) + 1; e > 2; e--)
            {
                LuaNativeMethods.lua_rawgeti(ptr, loaderTable, e - 1);
                LuaNativeMethods.lua_rawseti(ptr, loaderTable, e);
            }

            LuaNativeMethods.lua_pushvalue(ptr, loaderFunc);
            LuaNativeMethods.lua_rawseti(ptr, loaderTable, 2);
            LuaNativeMethods.lua_settop(ptr, 0);
            return 0;
        }

        public void OpenLibrary()
        {
            LuaTimer.Register(StatePointer);
            LuaCoroutine.Register(StatePointer, gameObject);
        }

        public void OpenExternalLibrary()
        {
            LuaNativeMethods.luaS_openextlibs(StatePointer);
            LuaSocketMini.Register(StatePointer);
        }

        public void BindUnity()
        {
            LuaSvr.DoBind(StatePointer);
            LuaValueType.Register(StatePointer);
        }

        public IEnumerator BindUnity(Action<int> tickAction, Action onComplete)
        {
            yield return LuaSvr.DoBind(StatePointer, tickAction, onComplete);
            LuaValueType.Register(StatePointer);
        }

        public bool IsMainThread()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId == mainThread;
        }

        public void Close()
        {
            DestroyGameObject();

            if (StatePointer != IntPtr.Zero)
            {
                Logger.Log("Finalizing Lua State.");

                // be careful, if you close lua vm, make sure you don't use lua state again,
                // comment this line as default for avoid unexpected crash.
                LuaNativeMethods.lua_close(StatePointer);

                ObjectCache.Delete(StatePointer);
                ObjectCache.Clear();

                statemap.Remove(StatePointer);
                oldptr = IntPtr.Zero;
                oldstate = null;
                StatePointer = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        public virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                Close();
            }
        }

        public object DoString(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);

            object obj;
            if (DoBuffer(bytes, "temp buffer", out obj))
            {
                return obj;
            }

            return null;
        }

        public object DoString(string str, string chunkname)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);

            object obj;
            if (DoBuffer(bytes, chunkname, out obj))
            {
                return obj;
            }

            return null;
        }

        public object DoFile(string fn)
        {
            byte[] bytes = LoadFile(fn);
            if (bytes == null)
            {
                Logger.LogError(string.Format("Can't find {0}", fn));
                return null;
            }

            object obj;
            if (DoBuffer(bytes, "@" + fn, out obj))
            {
                return obj;
            }

            return null;
        }

        public bool DoBuffer(byte[] bytes, string fn, out object ret)
        {
            // ensure no utf-8 bom, LuaJIT can read BOM, but Lua cannot!
            bytes = CleanUTF8Bom(bytes);
            ret = null;
            int errfunc = LuaObject.PushTry(statePointer);
            if (LuaNativeMethods.luaL_loadbuffer(statePointer, bytes, bytes.Length, fn) == 0)
            {
                if (LuaNativeMethods.lua_pcall(statePointer, 0, LuaNativeMethods.LUAMultRet, errfunc) != 0)
                {
                    LuaNativeMethods.lua_pop(statePointer, 2);
                    return false;
                }

                LuaNativeMethods.lua_remove(statePointer, errfunc); // pop error function
                ret = TopObjects(errfunc - 1);
                return true;
            }

            string err = LuaNativeMethods.lua_tostring(statePointer, -1);
            LuaNativeMethods.lua_pop(statePointer, 2);
            throw new Exception("File " + fn + ": " + err);
        }

        public object GetObject(string key)
        {
            LuaNativeMethods.lua_pushglobaltable(statePointer);
            object o = GetObject(key.Split(new char[] { '.' }));
            LuaNativeMethods.lua_pop(statePointer, 1);
            return o;
        }

        public void SetObject(string key, object v)
        {
            LuaNativeMethods.lua_pushglobaltable(statePointer);
            SetObject(key.Split(new char[] { '.' }), v);
            LuaNativeMethods.lua_pop(statePointer, 1);
        }

        public object GetObject(string[] remainingPath)
        {
            object returnValue = null;
            for (int i = 0; i < remainingPath.Length; i++)
            {
                LuaNativeMethods.lua_pushstring(statePointer, remainingPath[i]);
                LuaNativeMethods.lua_gettable(statePointer, -2);
                returnValue = this.GetObject(statePointer, -1);
                LuaNativeMethods.lua_remove(statePointer, -2);
                if (returnValue == null)
                {
                    break;
                }
            }

            return returnValue;
        }

        public object GetObject(int reference, string field)
        {
            int oldTop = LuaNativeMethods.lua_gettop(statePointer);
            LuaNativeMethods.lua_getref(statePointer, reference);
            object returnValue = GetObject(field.Split(new char[] { '.' }));
            LuaNativeMethods.lua_settop(statePointer, oldTop);
            return returnValue;
        }

        public object GetObject(int reference, int index)
        {
            if (index >= 1)
            {
                LuaNativeMethods.lua_getref(statePointer, reference);
                LuaNativeMethods.lua_rawgeti(statePointer, -1, index);
                object returnValue = GetObject(statePointer, -1);
                LuaNativeMethods.lua_pop(statePointer, 2);
                return returnValue;
            }
            else
            {
                LuaNativeMethods.lua_getref(statePointer, reference);
                LuaNativeMethods.lua_pushinteger(statePointer, index);
                LuaNativeMethods.lua_gettable(statePointer, -2);
                object returnValue = GetObject(statePointer, -1);
                LuaNativeMethods.lua_pop(statePointer, 2);
                return returnValue;
            }
        }

        public object GetObject(int reference, object field)
        {
            int oldTop = LuaNativeMethods.lua_gettop(statePointer);
            LuaNativeMethods.lua_getref(statePointer, reference);
            LuaObject.PushObject(statePointer, field);
            LuaNativeMethods.lua_gettable(statePointer, -2);
            object returnValue = GetObject(statePointer, -1);
            LuaNativeMethods.lua_settop(statePointer, oldTop);
            return returnValue;
        }

        public void SetObject(string[] remainingPath, object o)
        {
            int top = LuaNativeMethods.lua_gettop(statePointer);
            for (int i = 0; i < remainingPath.Length - 1; i++)
            {
                LuaNativeMethods.lua_pushstring(statePointer, remainingPath[i]);
                LuaNativeMethods.lua_gettable(statePointer, -2);
            }

            LuaNativeMethods.lua_pushstring(statePointer, remainingPath[remainingPath.Length - 1]);
            LuaObject.PushVar(statePointer, o);
            LuaNativeMethods.lua_settable(statePointer, -3);
            LuaNativeMethods.lua_settop(statePointer, top);
        }

        public void SetObject(int reference, string field, object o)
        {
            int oldTop = LuaNativeMethods.lua_gettop(statePointer);
            LuaNativeMethods.lua_getref(statePointer, reference);
            SetObject(field.Split(new char[] { '.' }), o);
            LuaNativeMethods.lua_settop(statePointer, oldTop);
        }

        public void SetObject(int reference, int index, object o)
        {
            if (index >= 1)
            {
                LuaNativeMethods.lua_getref(statePointer, reference);
                LuaObject.PushVar(statePointer, o);
                LuaNativeMethods.lua_rawseti(statePointer, -2, index);
                LuaNativeMethods.lua_pop(statePointer, 1);
            }
            else
            {
                LuaNativeMethods.lua_getref(statePointer, reference);
                LuaNativeMethods.lua_pushinteger(statePointer, index);
                LuaObject.PushVar(statePointer, o);
                LuaNativeMethods.lua_settable(statePointer, -3);
                LuaNativeMethods.lua_pop(statePointer, 1);
            }
        }

        public void SetObject(int reference, object field, object o)
        {
            int oldTop = LuaNativeMethods.lua_gettop(statePointer);
            LuaNativeMethods.lua_getref(statePointer, reference);
            LuaObject.PushObject(statePointer, field);
            LuaObject.PushObject(statePointer, o);
            LuaNativeMethods.lua_settable(statePointer, -3);
            LuaNativeMethods.lua_settop(statePointer, oldTop);
        }

        public object TopObjects(int from)
        {
            int top = LuaNativeMethods.lua_gettop(statePointer);
            int args = top - from;
            if (args == 0)
            {
                return null;
            }
            else if (args == 1)
            {
                object o = LuaObject.CheckVar(statePointer, top);
                LuaNativeMethods.lua_pop(statePointer, 1);
                return o;
            }
            else
            {
                object[] o = new object[args];
                for (int n = 1; n <= args; n++)
                {
                    o[n - 1] = LuaObject.CheckVar(statePointer, from + n);
                }

                LuaNativeMethods.lua_settop(statePointer, from);
                return o;
            }
        }

        public object GetObject(IntPtr ptr, int p)
        {
            p = LuaNativeMethods.lua_absindex(ptr, p);
            return LuaObject.CheckVar(ptr, p);
        }

        public LuaFunction GetFunction(string key)
        {
            return (LuaFunction)this[key];
        }

        public LuaTable GetTable(string key)
        {
            return (LuaTable)this[key];
        }

        public void GCRef(UnRefAction act, int r)
        {
            UnrefPair u = new UnrefPair()
            {
                Act = act,
                R = r
            };
            lock (refQueue)
            {
                refQueue.Enqueue(u);
            }
        }

        public void CheckRef()
        {
            int cnt = 0;

            // fix il2cpp lock issue on iOS
            lock (refQueue)
            {
                cnt = refQueue.Count;
            }

            IntPtr l = StatePointer;
            for (int n = 0; n < cnt; n++)
            {
                UnrefPair u;
                lock (refQueue)
                {
                    u = refQueue.Dequeue();
                }

                u.Act(statePointer, u.R);
            }
        }

        public void RegisterPushVar(Type t, PushVarDelegate d)
        {
            typePushMap[t] = d;
        }

        public bool TryGetTypePusher(Type t, out PushVarDelegate d)
        {
            return typePushMap.TryGetValue(t, out d);
        }

        public void SetupPushVar()
        {
            typePushMap[typeof(float)] = (IntPtr ptr, object o) =>
            {
                LuaNativeMethods.lua_pushnumber(ptr, (float)o);
            };

            typePushMap[typeof(double)] = (IntPtr ptr, object o) =>
            {
                LuaNativeMethods.lua_pushnumber(ptr, (double)o);
            };

            typePushMap[typeof(int)] =
                (IntPtr ptr, object o) =>
                {
                    LuaNativeMethods.lua_pushinteger(ptr, (int)o);
                };

            typePushMap[typeof(uint)] =
                (IntPtr ptr, object o) =>
                {
                    LuaNativeMethods.lua_pushnumber(ptr, Convert.ToUInt32(o));
                };

            typePushMap[typeof(short)] =
                (IntPtr ptr, object o) =>
                {
                    LuaNativeMethods.lua_pushinteger(ptr, (short)o);
                };

            typePushMap[typeof(ushort)] =
               (IntPtr ptr, object o) =>
               {
                   LuaNativeMethods.lua_pushinteger(ptr, (ushort)o);
               };

            typePushMap[typeof(sbyte)] =
               (IntPtr ptr, object o) =>
               {
                   LuaNativeMethods.lua_pushinteger(ptr, (sbyte)o);
               };

            typePushMap[typeof(byte)] =
               (IntPtr ptr, object o) =>
               {
                   LuaNativeMethods.lua_pushinteger(ptr, (byte)o);
               };

            typePushMap[typeof(long)] =
                typePushMap[typeof(ulong)] =
                (IntPtr ptr, object o) =>
                {
#if LUA_5_3
                    LuaNativeMethods.lua_pushinteger(ptr, (long)o);
#else
                    LuaNativeMethods.lua_pushnumber(ptr, System.Convert.ToDouble(o));
#endif
                };

            typePushMap[typeof(string)] = (IntPtr ptr, object o) =>
            {
                LuaNativeMethods.lua_pushstring(ptr, (string)o);
            };

            typePushMap[typeof(bool)] = (IntPtr ptr, object o) =>
            {
                LuaNativeMethods.lua_pushboolean(ptr, (bool)o);
            };

            typePushMap[typeof(LuaTable)] =
                typePushMap[typeof(LuaFunction)] =
                typePushMap[typeof(LuaThread)] =
                (IntPtr ptr, object o) =>
                {
                    ((LuaVar)o).Push(ptr);
                };

            typePushMap[typeof(LuaCSFunction)] = (IntPtr ptr, object o) =>
            {
                LuaObject.PushValue(ptr, (LuaCSFunction)o);
            };
        }

        public int PushTry()
        {
            if (errorRef == 0)
            {
                LuaNativeMethods.lua_pushcfunction(statePointer, LuaState.errorFunc);
                LuaNativeMethods.lua_pushvalue(statePointer, -1);
                errorRef = LuaNativeMethods.luaL_ref(statePointer, LuaIndexes.LUARegistryIndex);
            }
            else
            {
                LuaNativeMethods.lua_getref(statePointer, errorRef);
            }

            return LuaNativeMethods.lua_gettop(statePointer);
        }

        public void CreateGameObject()
        {
            if (gameObject == null
#if UNITY_EDITOR
                && UnityEditor.EditorApplication.isPlaying
#endif
                )
            {
                GameObject go = new GameObject("LuaSvrProxy");
                gameObject = go.AddComponent<LuaSvrGameObject>();
                GameObject.DontDestroyOnLoad(go);
                gameObject.OnUpdate = this.Tick;
                gameObject.State = this;
            }
        }

        public void DestroyGameObject()
        {
            if (gameObject == null
#if UNITY_EDITOR
                && UnityEditor.EditorApplication.isPlaying
#endif
                )
            {
                GameObject go = gameObject.gameObject;
                GameObject.Destroy(go);
                GameObject.Destroy(gameObject);
            }
        }

        protected virtual void Tick()
        {
            CheckRef();
        }

        public struct UnrefPair
        {
            private UnRefAction act;
            private int r;

            public int R
            {
                get
                {
                    return r;
                }

                set
                {
                    r = value;
                }
            }

            public UnRefAction Act
            {
                get
                {
                    return act;
                }

                set
                {
                    act = value;
                }
            }
        }
    }
}
