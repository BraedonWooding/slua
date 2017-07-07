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

namespace SLua
{
    public class LuaArray : LuaObject
    {
        private static Dictionary<string, ArrayPropFunction> propertyMethods = new Dictionary<string, ArrayPropFunction>();

        public delegate int ArrayPropFunction(IntPtr ptr, Array a);

        public static int ToTable(IntPtr ptr, Array o)
        {
            if (o == null)
            {
                LuaNativeMethods.lua_pushnil(ptr);
                return 1;
            }

            LuaNativeMethods.lua_createtable(ptr, o.Length, 0);
            for (int n = 0; n < o.Length; n++)
            {
                LuaObject.PushVar(ptr, o.GetValue(n));
                LuaNativeMethods.lua_rawseti(ptr, -2, n + 1);
            }

            return 1;
        }

        public static int Length(IntPtr ptr, Array a)
        {
            LuaObject.PushValue(ptr, a.Length);
            return 1;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Len(IntPtr ptr)
        {
            Array a = (Array)CheckSelf(ptr);
            LuaObject.PushValue(ptr, a.Length);
            return 1;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaIndex(IntPtr ptr)
        {
            try
            {
                Array a = (Array)CheckSelf(ptr);
                if (LuaNativeMethods.lua_type(ptr, 2) == LuaTypes.LUA_TSTRING)
                {
                    string mn;
                    LuaObject.CheckType(ptr, 2, out mn);
                    ArrayPropFunction fun;
                    if (propertyMethods.TryGetValue(mn, out fun))
                    {
                        LuaObject.PushValue(ptr, true);
                        return fun(ptr, a) + 1;
                    }
                    else
                    {
                        throw new Exception("Can't find property named " + mn);
                    }
                }
                else
                {
                    int i;
                    LuaObject.CheckType(ptr, 2, out i);
                    LuaObject.Assert(i > 0, "index base 1");
                    LuaObject.PushValue(ptr, true);
                    LuaObject.PushVar(ptr, a.GetValue(i - 1));
                    return 2;
                }
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaNewIndex(IntPtr ptr)
        {
            try
            {
                Array a = (Array)CheckSelf(ptr);
                int i;
                LuaObject.CheckType(ptr, 2, out i);
                LuaObject.Assert(i > 0, "index base 1");
                object o = CheckVar(ptr, 3);
                Type et = a.GetType().GetElementType();
                a.SetValue(LuaObject.ChangeType(o, et), i - 1);
                return LuaObject.Ok(ptr);
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static new int ToString(IntPtr ptr)
        {
            Array a = (Array)LuaObject.CheckSelf(ptr);
            LuaObject.PushValue(ptr, string.Format("Array<{0}>", a.GetType().GetElementType().Name));
            return 1;
        }

        public static new void Init(IntPtr ptr)
        {
            propertyMethods["Table"] = ToTable;
            propertyMethods["Length"] = Length;
            LuaNativeMethods.lua_createtable(ptr, 0, 5);
            LuaObject.PushValue(ptr, LuaIndex);
            LuaNativeMethods.lua_setfield(ptr, -2, "__index");
            LuaObject.PushValue(ptr, LuaNewIndex);
            LuaNativeMethods.lua_setfield(ptr, -2, "__newindex");
            LuaNativeMethods.lua_pushcfunction(ptr, LuaObject.LuaGC);
            LuaNativeMethods.lua_setfield(ptr, -2, "__gc");
            LuaNativeMethods.lua_pushcfunction(ptr, ToString);
            LuaNativeMethods.lua_setfield(ptr, -2, "__tostring");
            LuaNativeMethods.lua_pushcfunction(ptr, Len);
            LuaNativeMethods.lua_setfield(ptr, -2, "__len");
            LuaNativeMethods.lua_setfield(ptr, LuaIndexes.LUARegistryIndex, "LuaArray");
        }
    }
}
