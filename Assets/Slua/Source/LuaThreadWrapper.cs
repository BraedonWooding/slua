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

namespace SLua
{
    using UnityEngine;

    public class LuaThreadWrapper : LuaVar
    {
        private IntPtr thread;

        public LuaThreadWrapper(LuaFunction function) : base()
        {
            Logger.Log(string.Format("LuaThreadWrapper.ctor/1: {0}", LuaNativeMethods.lua_gettop(function.VariablePointer)));
            this.state = LuaState.Get(function.VariablePointer);
            this.thread = LuaNativeMethods.lua_newthread(function.VariablePointer);
            this.valueref = LuaNativeMethods.luaL_ref(function.VariablePointer, LuaIndexes.LUARegistryIndex);
            function.Push(function.VariablePointer);
            LuaNativeMethods.lua_xmove(function.VariablePointer, this.thread, 1);
            Logger.Log(string.Format("LuaThreadWrapper.ctor/2: {0}", LuaNativeMethods.lua_gettop(function.VariablePointer)));
        }

        ~LuaThreadWrapper()
        {
            Logger.Log("Deconstructing LuaThreadWrapper");
            this.Dispose(false);
        }

        public override void Dispose(bool disposeManagedResources)
        {
            base.Dispose(disposeManagedResources);
            this.thread = IntPtr.Zero;
        }

        public bool EqualsTo(IntPtr ptr)
        {
            return this.thread == ptr;
        }

        public bool Resume(out object retVal)
        {
            if (this.thread == IntPtr.Zero)
            {
                Logger.LogError("thread: already disposed?");
                retVal = null;
                return false;
            }

            int status = LuaNativeMethods.lua_status(this.thread);
            if (status != 0 && status != (int)LuaThreadStatus.LUA_YIELD)
            {
                Logger.LogError("thread: wrong status ?= " + status);
                retVal = null;
                return false;
            }

            int result = LuaNativeMethods.lua_resume(this.thread, 0);
            if (result != (int)LuaThreadStatus.LUA_YIELD)
            {
                if (result != 0)
                {
                    string error = LuaNativeMethods.lua_tostring(this.thread, -1);
                    Logger.LogError(string.Format("wrong result ?= {0} err: {1}", result, error));
                }

                retVal = null;
                return false;
            }

            int argsFromYield = LuaNativeMethods.lua_gettop(this.thread);
            retVal = this.TopObjects(argsFromYield);
            return true;
        }

        private object TopObjects(int args)
        {
            if (args == 0)
            {
                return null;
            }
            else if (args == 1)
            {
                object o = LuaObject.CheckVar(this.thread, -1);
                return o;
            }
            else
            {
                object[] o = new object[args];
                for (int n = 1; n <= args; n++)
                {
                    o[n - 1] = LuaObject.CheckVar(this.thread, n);
                }

                return o;
            }
        }
    }
}
