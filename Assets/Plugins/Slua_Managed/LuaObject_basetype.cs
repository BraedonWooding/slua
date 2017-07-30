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
using System.Reflection;

namespace SLua
{
    /* https://msdn.microsoft.com/zh-cn/library/s1ax56ch.aspx
     * 
     * null								LUA_TNIL
     * Value Types:
     * 	enum
     * 	struct:
     * 		Numeric types: 
     *	 		Integral Types: 		LUA_TNUMBER
     * 				sbyte  = SByte
     * 				byte   = Byte
     * 				char   = Char
     * 				short  = Int16
     * 				ushort = UInt16
     * 				int	   = Int32
     * 				uint   = UInt32
     * 				long   = Int64
     * 				ulong  = UInt64
     * 			Floating-Point Types: 	LUA_TNUMBER
     * 				float  = Single
     * 				double = Double
     * 		bool = Boolean				LUA_TBOOLEAN
     * 		User defined structs		LUA_TTABLE(Vector...) || non_cached@LUA_TUSERDATA
     * Reference Types:
     *  string							LUA_TSTRING
     * 	delegate						LUA_TFUNCTION
     * 	class、System.Type				LUA_TTABLE || cached@LUA_TUSERDATA
     * 	object							cached@LUA_TUSERDATA
     *  char[]							LUA_TSTRING
     * 	T[]								LUA_TTABLE limit support
     *  interface, dynamic 				unsupport
     * IntPtr							LUA_TLIGHTUSERDATA
     * 
     * 
     * every type should implement: 
     * 		public static bool CheckType(IntPtr ptr, int p, out T v)
     *		public static void PushValue(IntPtr ptr, T v)
     * 
    */
    public partial class LuaObject
    {
        private static Type monoType = typeof(Type).GetType();

        #region Enum
        public static bool CheckEnum<T>(IntPtr ptr, int p, out T o) where T : struct
        {
            int i = (int)LuaNativeMethods.luaL_checkinteger(ptr, p);
            o = (T)Enum.ToObject(typeof(T), i);

            return true;
        }

        public static void PushEnum(IntPtr ptr, int e)
        {
            LuaObject.PushValue(ptr, e);
        }
        #endregion

        #region Integral Types
        public static bool CheckType(IntPtr ptr, int p, out sbyte v)
        {
            v = (sbyte)LuaNativeMethods.luaL_checkinteger(ptr, p);
            return true;
        }

        public static void PushValue(IntPtr ptr, sbyte v)
        {
            LuaNativeMethods.lua_pushinteger(ptr, v);
        }

        public static bool CheckType(IntPtr ptr, int p, out byte v)
        {
            v = (byte)LuaNativeMethods.luaL_checkinteger(ptr, p);
            return true;
        }

        public static void PushValue(IntPtr ptr, byte i)
        {
            LuaNativeMethods.lua_pushinteger(ptr, i);
        }

        // why doesn't have a checkArray<byte[]> function accept lua string?
        // I think you should did a Buffer class to wrap byte[] pass/accept between mono and lua vm

        public static bool CheckType(IntPtr ptr, int p, out char c)
        {
            c = (char)LuaNativeMethods.luaL_checkinteger(ptr, p);
            return true;
        }

        public static void PushValue(IntPtr ptr, char v)
        {
            LuaNativeMethods.lua_pushinteger(ptr, v);
        }

        public static bool CheckArray(IntPtr ptr, int p, out char[] pars)
        {
            LuaNativeMethods.luaL_checktype(ptr, p, LuaTypes.TYPE_STRING);
            string s;
            CheckType(ptr, p, out s);
            pars = s.ToCharArray();
            return true;
        }

        public static bool CheckType(IntPtr ptr, int p, out short v)
        {
            v = (short)LuaNativeMethods.luaL_checkinteger(ptr, p);
            return true;
        }

        public static void PushValue(IntPtr ptr, short i)
        {
            LuaNativeMethods.lua_pushinteger(ptr, i);
        }

        public static bool CheckType(IntPtr ptr, int p, out ushort v)
        {
            v = (ushort)LuaNativeMethods.luaL_checkinteger(ptr, p);
            return true;
        }

        public static void PushValue(IntPtr ptr, ushort v)
        {
            LuaNativeMethods.lua_pushinteger(ptr, v);
        }

        public static bool CheckType(IntPtr ptr, int p, out int v)
        {
            v = (int)LuaNativeMethods.luaL_checkinteger(ptr, p);
            return true;
        }

        public static void PushValue(IntPtr ptr, int i)
        {
            LuaNativeMethods.lua_pushinteger(ptr, i);
        }

        public static bool CheckType(IntPtr ptr, int p, out uint v)
        {
            v = (uint)LuaNativeMethods.luaL_checkinteger(ptr, p);
            return true;
        }

        public static void PushValue(IntPtr ptr, uint o)
        {
            LuaNativeMethods.lua_pushnumber(ptr, o);
        }

        public static bool CheckType(IntPtr ptr, int p, out long v)
        {
#if LUA_5_3
            v = (long)LuaDLL.luaL_checkinteger(ptr, p);
#else
            v = (long)LuaNativeMethods.luaL_checknumber(ptr, p);
#endif
            return true;
        }

        public static void PushValue(IntPtr ptr, long i)
        {
#if LUA_5_3
            LuaDLL.lua_pushinteger(l,i);
#else
            LuaNativeMethods.lua_pushnumber(ptr, i);
#endif
        }

        public static bool CheckType(IntPtr ptr, int p, out ulong v)
        {
#if LUA_5_3
            v = (ulong)LuaDLL.luaL_checkinteger(ptr, p);
#else
            v = (ulong)LuaNativeMethods.luaL_checknumber(ptr, p);
#endif
            return true;
        }

        public static void PushValue(IntPtr ptr, ulong o)
        {
#if LUA_5_3
            LuaDLL.lua_pushinteger(ptr, (long)o);
#else
            LuaNativeMethods.lua_pushnumber(ptr, o);
#endif
        }

        #endregion

        #region Floating-Point Types
        public static bool CheckType(IntPtr ptr, int p, out float v)
        {
            v = (float)LuaNativeMethods.luaL_checknumber(ptr, p);
            return true;
        }

        public static void PushValue(IntPtr ptr, float o)
        {
            LuaNativeMethods.lua_pushnumber(ptr, o);
        }

        public static bool CheckType(IntPtr ptr, int p, out double v)
        {
            v = LuaNativeMethods.luaL_checknumber(ptr, p);
            return true;
        }

        public static void PushValue(IntPtr ptr, double d)
        {
            LuaNativeMethods.lua_pushnumber(ptr, d);
        }

        #endregion

        #region bool
        public static bool CheckType(IntPtr ptr, int p, out bool v)
        {
            LuaNativeMethods.luaL_checktype(ptr, p, LuaTypes.TYPE_BOOLEAN);
            v = LuaNativeMethods.lua_toboolean(ptr, p);
            return true;
        }

        public static void PushValue(IntPtr ptr, bool b)
        {
            LuaNativeMethods.lua_pushboolean(ptr, b);
        }

        #endregion

        #region string
        public static bool CheckType(IntPtr ptr, int p, out string v)
        {
            if (LuaNativeMethods.lua_isuserdata(ptr, p) > 0)
            {
                object o = CheckObj(ptr, p);
                if (o is string)
                {
                    v = o as string;
                    return true;
                }
            }
            else if (LuaNativeMethods.lua_isstring(ptr, p))
            {
                v = LuaNativeMethods.lua_tostring(ptr, p);
                return true;
            }

            v = null;
            return false;
        }

        public static bool CheckBinaryString(IntPtr ptr, int p, out byte[] bytes)
        {
            if (LuaNativeMethods.lua_isstring(ptr, p))
            {
                bytes = LuaNativeMethods.lua_tobytes(ptr, p);
                return true;
            }

            bytes = null;
            return false;
        }

        public static void PushValue(IntPtr ptr, string s)
        {
            LuaNativeMethods.lua_pushstring(ptr, s);
        }
        #endregion

        #region IntPtr
        public static bool CheckType(IntPtr ptr, int p, out IntPtr v)
        {
            v = LuaNativeMethods.lua_touserdata(ptr, p);
            return true;
        }
        #endregion

        #region LuaType
        public static bool CheckType(IntPtr ptr, int p, out LuaDelegate f)
        {
            LuaState state = LuaState.Get(ptr);

            p = LuaNativeMethods.lua_absindex(ptr, p);
            LuaNativeMethods.luaL_checktype(ptr, p, LuaTypes.TYPE_FUNCTION);

            LuaNativeMethods.lua_getglobal(ptr, DelgateTable);
            LuaNativeMethods.lua_pushvalue(ptr, p);
            LuaNativeMethods.lua_gettable(ptr, -2); // find function in __LuaDelegate table

            if (LuaNativeMethods.lua_isnil(ptr, -1))
            { // not found
                LuaNativeMethods.lua_pop(ptr, 1); // pop nil
                f = NewDelegate(ptr, p);
            }
            else
            {
                int fref = LuaNativeMethods.lua_tointeger(ptr, -1);
                LuaNativeMethods.lua_pop(ptr, 1); // pop ref value;
                f = state.DelegateMap[fref];
                if (f == null)
                {
                    f = NewDelegate(ptr, p);
                }
            }

            LuaNativeMethods.lua_pop(ptr, 1); // pop DelgateTable
            return true;
        }

        public static bool CheckType(IntPtr ptr, int p, out LuaThread lt)
        {
            if (LuaNativeMethods.lua_isnil(ptr, p))
            {
                lt = null;
                return true;
            }

            LuaNativeMethods.luaL_checktype(ptr, p, LuaTypes.TYPE_THREAD);
            LuaNativeMethods.lua_pushvalue(ptr, p);
            int fref = LuaNativeMethods.luaL_ref(ptr, LuaIndexes.LUARegistryIndex);
            lt = new LuaThread(ptr, fref);
            return true;
        }

        public static bool CheckType(IntPtr ptr, int p, out LuaFunction f)
        {
            if (LuaNativeMethods.lua_isnil(ptr, p))
            {
                f = null;
                return true;
            }

            LuaNativeMethods.luaL_checktype(ptr, p, LuaTypes.TYPE_FUNCTION);
            LuaNativeMethods.lua_pushvalue(ptr, p);
            int fref = LuaNativeMethods.luaL_ref(ptr, LuaIndexes.LUARegistryIndex);
            f = new LuaFunction(ptr, fref);
            return true;
        }

        public static bool CheckType(IntPtr ptr, int p, out LuaTable t)
        {
            if (LuaNativeMethods.lua_isnil(ptr, p))
            {
                t = null;
                return true;
            }

            LuaNativeMethods.luaL_checktype(ptr, p, LuaTypes.TYPE_TABLE);
            LuaNativeMethods.lua_pushvalue(ptr, p);
            int fref = LuaNativeMethods.luaL_ref(ptr, LuaIndexes.LUARegistryIndex);
            t = new LuaTable(ptr, fref);
            return true;
        }

        public static void PushValue(IntPtr ptr, LuaCSFunction f)
        {
            LuaState.PushCSFunction(ptr, f);
        }

        public static void PushValue(IntPtr ptr, LuaTable t)
        {
            if (t == null)
            {
                LuaNativeMethods.lua_pushnil(ptr);
            }
            else
            {
                t.Push(ptr);
            }
        }
        #endregion

        #region Type
        public static Type FindType(string qualifiedTypeName)
        {
            Type t = Type.GetType(qualifiedTypeName);

            if (t != null)
            {
                return t;
            }
            else
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int n = 0; n < assemblies.Length; n++)
                {
                    Assembly asm = assemblies[n];
                    t = asm.GetType(qualifiedTypeName);
                    if (t != null)
                    {
                        return t;
                    }
                }

                return null;
            }
        }

        public static bool CheckType(IntPtr ptr, int p, out Type t)
        {
            string tname = null;
            LuaTypes lt = LuaNativeMethods.lua_type(ptr, p);
            switch (lt)
            {
                case LuaTypes.TYPE_USERDATA:
                    object o = CheckObj(ptr, p);
                    if (o.GetType() != monoType)
                    {
                        throw new Exception(string.Format("{0} expect Type, got {1}", p, o.GetType().Name));
                    }

                    t = (Type)o;
                    return true;
                case LuaTypes.TYPE_TABLE:
                    LuaNativeMethods.lua_pushstring(ptr, "__type");
                    LuaNativeMethods.lua_rawget(ptr, p);
                    if (!LuaNativeMethods.lua_isnil(ptr, -1))
                    {
                        t = (Type)CheckObj(ptr, -1);
                        LuaNativeMethods.lua_pop(ptr, 1);
                        return true;
                    }
                    else
                    {
                        LuaNativeMethods.lua_pushstring(ptr, "__fullname");
                        LuaNativeMethods.lua_rawget(ptr, p);
                        tname = LuaNativeMethods.lua_tostring(ptr, -1);
                        LuaNativeMethods.lua_pop(ptr, 2);
                    }

                    break;

                case LuaTypes.TYPE_STRING:
                    CheckType(ptr, p, out tname);
                    break;
            }

            if (tname == null)
            {
                throw new Exception("expect string or type table");
            }

            t = LuaObject.FindType(tname);
            if (t != null && lt == LuaTypes.TYPE_TABLE)
            {
                LuaNativeMethods.lua_pushstring(ptr, "__type");
                PushLightObject(ptr, t);
                LuaNativeMethods.lua_rawset(ptr, p);
            }

            return t != null;
        }
        #endregion

        #region Struct
        public static bool CheckValueType<T>(IntPtr ptr, int p, out T v) where T : struct
        {
            v = (T)CheckObj(ptr, p);
            return true;
        }
        #endregion

        public static bool CheckNullable<T>(IntPtr ptr, int p, out T? v) where T : struct
        {
            if (LuaNativeMethods.lua_isnil(ptr, p))
            {
                v = null;
            }
            else
            {
                object o = CheckVar(ptr, p, typeof(T));
                if (o == null)
                {
                    v = null;
                }
                else
                {
                    v = new T?((T)o);
                }
            }

            return true;
        }

        #region Object
        public static bool CheckType<T>(IntPtr ptr, int p, out T o) where T : class
        {
            object obj = CheckVar(ptr, p);
            if (obj == null)
            {
                o = null;
                return true;
            }

            o = obj as T;
            if (o == null)
            {
                throw new Exception(string.Format("arg {0} is not type of {1}", p, typeof(T).Name));
            }

            return true;
        }
        #endregion
    }
}
