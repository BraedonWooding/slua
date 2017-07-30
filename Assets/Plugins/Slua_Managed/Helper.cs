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
using System.Reflection;

namespace SLua
{
    public class Helper : LuaObject
    {
        public const string ClassFunction = @"
local getmetatable = getmetatable
local function Class(base,static,instance)

    local mt = getmetatable(base)

    local class = static or {}
    setmetatable(class, 
        {
            __index = base,
            __call = function(...)
                local r = mt.__call(...)
                local ret = instance or {}

                local ins_ret = setmetatable(
                    {
                        __base = r,
                    },

                    {
                        __index = function(t, k)
                            local ret_field
                            ret_field = ret[k]
                            if nil == ret_field then
                                ret_field = r[k]
                            end

                            return ret_field
                        end,

                        __newindex = function(t,k,v)
                            if not pcall(function() r[k]=v end) then
                                rawset(t,k,v)
                            end
                        end,
                    })

                if ret.ctor then
                    ret.ctor(ins_ret, ...)
                end

                return ins_ret
            end,
        }
    )
    return class
end
return Class
";

        public static readonly LuaOut LuaOut = new LuaOut();

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int _iter(IntPtr ptr)
        {
            object obj = CheckObj(ptr, LuaNativeMethods.lua_upvalueindex(1));
            IEnumerator it = (IEnumerator)obj;
            if (it.MoveNext())
            {
                LuaObject.PushVar(ptr, it.Current);
                return 1;
            }
            else
            {
                if (obj is IDisposable)
                {
                    ((IDisposable)obj).Dispose();
                }
            }

            return 0;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Iterator(IntPtr ptr)
        {
            object o = CheckObj(ptr, 1);
            if (o is IEnumerable)
            {
                IEnumerable e = o as IEnumerable;
                IEnumerator iter = e.GetEnumerator();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushLightObject(ptr, iter);
                LuaNativeMethods.lua_pushcclosure(ptr, _iter, 1);
                return 2;
            }

            return Error(ptr, "passed in object isn't enumerable");
        }

        /// <summary>
        /// Create standard System.Action.
        /// </summary>
        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int CreateAction(IntPtr ptr)
        {
            try
            {
                LuaFunction func;
                CheckType(ptr, 1, out func);
                Action action = new Action(() =>
                {
                    func.Call();
                });

                LuaObject.PushValue(ptr, true);
                LuaObject.PushVar(ptr, action);
                return 2;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int CreateClass(IntPtr ptr)
        {
            try
            {
                string cls;
                CheckType(ptr, 1, out cls);
                Type t = LuaObject.FindType(cls);
                if (t == null)
                {
                    return Error(ptr, string.Format("Can't find {0} to create", cls));
                }

                ConstructorInfo[] cis = t.GetConstructors();
                ConstructorInfo target = null;
                for (int n = 0; n < cis.Length; n++)
                {
                    ConstructorInfo ci = cis[n];
                    if (LuaObject.MatchType(ptr, LuaNativeMethods.lua_gettop(ptr), 2, ci.GetParameters()))
                    {
                        target = ci;
                        break;
                    }
                }

                if (target != null)
                {
                    ParameterInfo[] pis = target.GetParameters();
                    object[] args = new object[pis.Length];
                    for (int n = 0; n < pis.Length; n++)
                    {
                        args[n] = LuaObject.ChangeType(LuaObject.CheckVar(ptr, n + 2), pis[n].ParameterType);
                    }

                    object ret = target.Invoke(args);
                    LuaObject.PushValue(ptr, true);
                    LuaObject.PushVar(ptr, ret);
                    return 2;
                }

                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int GetClass(IntPtr ptr)
        {
            try
            {
                string cls;
                CheckType(ptr, 1, out cls);
                Type t = LuaObject.FindType(cls);
                if (t == null)
                {
                    return Error(ptr, "Can't find {0} to create", cls);
                }

                LuaClassObject co = new LuaClassObject(t);
                LuaObject.PushValue(ptr, true);
                LuaObject.PushObject(ptr, co);
                return 2;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        /// <summary>
        /// Convert lua binary string to c# byte[].
        /// </summary>
        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ToBytes(IntPtr ptr)
        {
            try
            {
                byte[] bytes = null;
                LuaObject.CheckBinaryString(ptr, 1, out bytes);
                LuaObject.PushValue(ptr, true);
                LuaObject.PushObject(ptr, bytes);
                return 2;
            }
            catch (System.Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static new int ToString(IntPtr ptr)
        {
            try
            {
                object o = CheckObj(ptr, 1);
                if (o == null)
                {
                    LuaObject.PushValue(ptr, true);
                    LuaNativeMethods.lua_pushnil(ptr);
                    return 2;
                }

                LuaObject.PushValue(ptr, true);
                if (o is byte[])
                {
                    byte[] b = (byte[])o;
                    LuaNativeMethods.lua_pushlstring(ptr, b, b.Length);
                }
                else
                {
                    LuaObject.PushValue(ptr, o.ToString());
                }

                return 2;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int MakeArray(IntPtr ptr)
        {
            try
            {
                Type t;
                CheckType(ptr, 1, out t);
                LuaNativeMethods.luaL_checktype(ptr, 2, LuaTypes.TYPE_TABLE);
                int n = LuaNativeMethods.lua_rawlen(ptr, 2);
                Array array = Array.CreateInstance(t, n);
                for (int k = 0; k < n; k++)
                {
                    LuaNativeMethods.lua_rawgeti(ptr, 2, k + 1);
                    object obj = LuaObject.CheckVar(ptr, -1);
                    array.SetValue(LuaObject.ChangeType(obj, t), k);
                    LuaNativeMethods.lua_pop(ptr, 1);
                }

                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, array);
                return 2;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int As(IntPtr ptr)
        {
            try
            {
                if (!LuaObject.IsTypeTable(ptr, 2))
                {
                    return Error(ptr, "No matched type of param 2");
                }

                string meta = LuaNativeMethods.lua_tostring(ptr, -1);
                LuaNativeMethods.luaL_getmetatable(ptr, meta);
                LuaNativeMethods.lua_setmetatable(ptr, 1);
                LuaObject.PushValue(ptr, true);
                LuaNativeMethods.lua_pushvalue(ptr, 1);
                return 2;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int IsNull(IntPtr ptr)
        {
            try
            {
                LuaTypes t = LuaNativeMethods.lua_type(ptr, 1);
                LuaObject.PushValue(ptr, true);

                if (t == LuaTypes.TYPE_NIL)
                {
                    LuaObject.PushValue(ptr, true);
                }
                else if (t == LuaTypes.TYPE_USERDATA || LuaObject.IsLuaClass(ptr, 1))
                {
                    // LUA_TUSERDATA or LUA_TTABLE(Class inherited from Unity Native)
                    object o = LuaObject.CheckObj(ptr, 1);
                    if (o is UnityEngine.Object)
                    {
                        LuaObject.PushValue(ptr, ((UnityEngine.Object)o) == null);
                    }
                    else
                    {
                        LuaObject.PushValue(ptr, o.Equals(null));
                    }
                }
                else
                {
                    LuaObject.PushValue(ptr, false);
                }

                return 2;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int GetOut(IntPtr ptr)
        {
            LuaObject.PushValue(ptr, true);
            LuaObject.PushLightObject(ptr, LuaOut);
            return 2;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int GetVersion(IntPtr ptr)
        {
            LuaObject.PushValue(ptr, true);
            LuaObject.PushValue(ptr, LuaObject.VersionNumber);
            return 2;
        }

        public static void Register(IntPtr ptr)
        {
            LuaObject.GetTypeTable(ptr, "Slua");
            LuaObject.AddMember(ptr, CreateAction, false);
            LuaObject.AddMember(ptr, CreateClass, false);
            LuaObject.AddMember(ptr, GetClass, false);
            LuaObject.AddMember(ptr, Iterator, false);
            LuaObject.AddMember(ptr, ToString, false);
            LuaObject.AddMember(ptr, As, false);
            LuaObject.AddMember(ptr, IsNull, false);
            LuaObject.AddMember(ptr, MakeArray, false);
            LuaObject.AddMember(ptr, ToBytes, false);
            LuaObject.AddMember(ptr, "out", GetOut, null, false);
            LuaObject.AddMember(ptr, "version", GetVersion, null, false);

            LuaFunction function = LuaState.Get(ptr).DoString(ClassFunction) as LuaFunction;
            function.Push(ptr);
            LuaNativeMethods.lua_setfield(ptr, -3, "Class");
            LuaObject.CreateTypeMetatable(ptr, null, typeof(Helper));
        }
    }
}
