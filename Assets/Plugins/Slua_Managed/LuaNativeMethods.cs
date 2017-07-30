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
using System.Runtime.InteropServices;

namespace SLua
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
    public delegate int LuaCSFunction(IntPtr luaState);

    public delegate string LuaChunkReader(IntPtr luaState, ref ReaderInfo data, ref uint size);

    public delegate int LuaFunctionCallback(IntPtr luaState);

    public enum LuaTypes : int
    {
        TYPE_NONE = -1,
        TYPE_NIL = 0,
        TYPE_BOOLEAN = 1,
        TYPE_LIGHTUSERDATA = 2,
        TYPE_NUMBER = 3,
        TYPE_STRING = 4,
        TYPE_TABLE = 5,
        TYPE_FUNCTION = 6,
        TYPE_USERDATA = 7,
        TYPE_THREAD = 8,
    }

    public enum LuaGCOptions
    {
        LUA_GCSTOP = 0,
        LUA_GCRESTART = 1,
        LUA_GCCOLLECT = 2,
        LUA_GCCOUNT = 3,
        LUA_GCCOUNTB = 4,
        LUA_GCSTEP = 5,
        LUA_GCSETPAUSE = 6,
        LUA_GCSETSTEPMUL = 7,
    }

    public enum LuaThreadStatus : int
    {
        LUA_YIELD = 1,
        LUA_ERRRUN = 2,
        LUA_ERRSYNTAX = 3,
        LUA_ERRMEM = 4,
        LUA_ERRERR = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ReaderInfo
    {
        public string ChunkData { get; set; }

        public bool Finished { get; set; }
    }

    /// <summary>
    /// For callbacks.
    /// </summary>
    /// <remarks> It disables the warning 414, due to the fact that it is assigned in a different DLL. </remarks>
    public class MonoPInvokeCallbackAttribute : System.Attribute
    {
#pragma warning disable 414
        private Type type;
#pragma warning restore 414

        public MonoPInvokeCallbackAttribute(Type t)
        {
            this.type = t;
        }
    }

    public sealed class LuaIndexes
    {
#if LUA_5_3
        // for lua5.3
        public static int LUARegistryIndex = -1000000 - 1000;
#else
        // for lua5.1 or luajit
        public const int LUARegistryIndex = -10000;
        public const int LUAGlobalIndex = -10002;
#endif
    }

    public class LuaNativeMethods
    {
        public const int LUAMultRet = -1;
        public const string LUADLL = "slua";

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaS_openextlibs(IntPtr L);

        // Thread Funcs
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_tothread(IntPtr L, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_xmove(IntPtr from, IntPtr to, int n);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_newthread(IntPtr L);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_status(IntPtr L);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_pushthread(IntPtr L);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gc(IntPtr luaState, LuaGCOptions what, int data);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_typename(IntPtr luaState, int type);

        public static string lua_typenamestr(IntPtr luaState, LuaTypes type)
        {
            IntPtr p = lua_typename(luaState, (int)type);
            return Marshal.PtrToStringAnsi(p);
        }

        public static string luaL_typename(IntPtr luaState, int stackPos)
        {
            return LuaNativeMethods.lua_typenamestr(luaState, LuaNativeMethods.lua_type(luaState, stackPos));
        }

        public static bool lua_isfunction(IntPtr luaState, int stackPos)
        {
            return lua_type(luaState, stackPos) == LuaTypes.TYPE_FUNCTION;
        }

        public static bool lua_islightuserdata(IntPtr luaState, int stackPos)
        {
            return lua_type(luaState, stackPos) == LuaTypes.TYPE_LIGHTUSERDATA;
        }

        public static bool lua_istable(IntPtr luaState, int stackPos)
        {
            return lua_type(luaState, stackPos) == LuaTypes.TYPE_TABLE;
        }

        public static bool lua_isthread(IntPtr luaState, int stackPos)
        {
            return lua_type(luaState, stackPos) == LuaTypes.TYPE_THREAD;
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern string luaL_gsub(IntPtr luaState, string str, string pattern, string replacement);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_isuserdata(IntPtr luaState, int stackPos);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_rawequal(IntPtr luaState, int stackPos1, int stackPos2);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setfield(IntPtr luaState, int stackPos, string name);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_callmeta(IntPtr luaState, int stackPos, string name);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr luaL_newstate();

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_close(IntPtr luaState);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_openlibs(IntPtr luaState);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_loadstring(IntPtr luaState, string chunk);

        public static int luaL_dostring(IntPtr luaState, string chunk)
        {
            int result = LuaNativeMethods.luaL_loadstring(luaState, chunk);
            if (result != 0)
            {
                return result;
            }

            return LuaNativeMethods.lua_pcall(luaState, 0, -1, 0);
        }

        public static int lua_dostring(IntPtr luaState, string chunk)
        {
            return LuaNativeMethods.luaL_dostring(luaState, chunk);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_createtable(IntPtr luaState, int narr, int nrec);

        public static void lua_newtable(IntPtr luaState)
        {
            LuaNativeMethods.lua_createtable(luaState, 0, 0);
        }

#if LUA_5_3
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_getglobal(IntPtr luaState, string name);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setglobal(IntPtr luaState, string name);

        public static void lua_insert(IntPtr luaState, int newTop)
        {
            lua_rotate(luaState, newTop, 1);
        }

        public static void lua_pushglobaltable(IntPtr l)
        {
            lua_rawgeti(ptr, LuaIndexes.LUA_REGISTRYINDEX, 2); 
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_rotate(IntPtr luaState, int index, int n);

        public static int lua_rawlen(IntPtr luaState, int stackPos)
        {
            return LuaDLLWrapper.luaS_rawlen(luaState, stackPos);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_loadbufferx(IntPtr luaState, byte[] buff, int size, string name, IntPtr x);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_callk(IntPtr luaState, int nArgs, int nResults,int ctx,IntPtr k);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_pcallk(IntPtr luaState, int nArgs, int nResults, int errfunc,int ctx,IntPtr k);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_pcall(IntPtr luaState, int nArgs, int nResults, int errfunc);
        
        public static int lua_call(IntPtr luaState, int nArgs, int nResults)
        {
            return lua_callk(luaState, nArgs, nResults, 0, IntPtr.Zero);
        }

        public static int lua_pcall(IntPtr luaState, int nArgs, int nResults, int errfunc)
        {
            return luaS_pcall(luaState, nArgs, nResults, errfunc);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern double lua_tonumberx(IntPtr luaState, int index, IntPtr x);
        public static double lua_tonumber(IntPtr luaState, int index)
        {
            return lua_tonumberx(luaState, index, IntPtr.Zero);
        }        
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int64 lua_tointegerx(IntPtr luaState, int index,IntPtr x);
        public static int lua_tointeger(IntPtr luaState, int index)
        {
            return (int)lua_tointegerx(luaState, index, IntPtr.Zero);
        }


        public static int luaL_loadbuffer(IntPtr luaState, byte[] buff, int size, string name)
        {
            return luaL_loadbufferx(luaState, buff, size, name, IntPtr.Zero);
        }

        public static void lua_remove(IntPtr ptr,  int idx)
        {
            lua_rotate(ptr, (idx), -1);
            lua_pop(ptr, 1);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawgeti(IntPtr luaState, int tableIndex, Int64 index);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawseti(IntPtr luaState, int tableIndex, Int64 index);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushinteger(IntPtr luaState, Int64 i);

        public static Int64 luaL_checkinteger(IntPtr luaState, int stackPos) {
            luaL_CheckType(luaState, stackPos, LuaTypes.LUA_TNUMBER);
            return lua_tointegerx(luaState, stackPos, IntPtr.Zero);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_yield(IntPtr luaState,int nrets);

        public static int lua_yield(IntPtr luaState,int nrets) {
            return luaS_yield(luaState,nrets);
        }


        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_resume(IntPtr ptr,  IntPtr from, int narg);
        public static int lua_resume(IntPtr ptr,  int narg)
        {
            return lua_resume(ptr, IntPtr.Zero, narg);
        }

        public static void lua_replace(IntPtr luaState, int index) {
            lua_copy(luaState, -1, (index));
            lua_pop(luaState, 1);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_copy(IntPtr luaState,int from,int toidx);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_isinteger(IntPtr luaState, int p);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_compare(IntPtr luaState, int index1, int index2, int op);
        
        public static int lua_equal(IntPtr luaState, int index1, int index2)
        {
            return lua_compare(luaState, index1, index2, 0);
        }

#else
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_resume(IntPtr ptr, int narg);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_lessthan(IntPtr luaState, int stackPos1, int stackPos2);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_getfenv(IntPtr luaState, int stackPos);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_yield(IntPtr ptr, int nresults);

        public static void lua_getglobal(IntPtr luaState, string name)
        {
            LuaNativeMethods.lua_pushstring(luaState, name);
            LuaNativeMethods.lua_gettable(luaState, LuaIndexes.LUAGlobalIndex);
        }

        public static void lua_setglobal(IntPtr luaState, string name)
        {
            LuaNativeMethods.lua_pushstring(luaState, name);
            LuaNativeMethods.lua_insert(luaState, -2);
            LuaNativeMethods.lua_settable(luaState, LuaIndexes.LUAGlobalIndex);
        }

        public static void lua_pushglobaltable(IntPtr ptr)
        {
            LuaNativeMethods.lua_pushvalue(ptr, LuaIndexes.LUAGlobalIndex);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_insert(IntPtr luaState, int newTop);

        public static int lua_rawlen(IntPtr luaState, int stackPos)
        {
            return LuaWrapperNativeMethods.luaS_objlen(luaState, stackPos);
        }

        public static int lua_strlen(IntPtr luaState, int stackPos)
        {
            return lua_rawlen(luaState, stackPos);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_call(IntPtr luaState, int nArgs, int nResults);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_pcall(IntPtr luaState, int nArgs, int nResults, int errfunc);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern double lua_tonumber(IntPtr luaState, int index);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_tointeger(IntPtr luaState, int index);

        public static int luaL_loadbuffer(IntPtr luaState, byte[] buff, int size, string name)
        {
            return LuaWrapperNativeMethods.luaLS_loadbuffer(luaState, buff, size, name);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_remove(IntPtr luaState, int index);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawgeti(IntPtr luaState, int tableIndex, int index);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawseti(IntPtr luaState, int tableIndex, int index);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushinteger(IntPtr luaState, IntPtr i);

        public static void lua_pushinteger(IntPtr luaState, int i)
        {
            lua_pushinteger(luaState, (IntPtr)i);
        }

        public static int luaL_checkinteger(IntPtr luaState, int stackPos)
        {
            luaL_checktype(luaState, stackPos, LuaTypes.TYPE_NUMBER);
            return lua_tointeger(luaState, stackPos);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_replace(IntPtr luaState, int index);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_setfenv(IntPtr luaState, int stackPos);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_equal(IntPtr luaState, int index1, int index2);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_loadfile(IntPtr luaState, string filename);
#endif

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settop(IntPtr luaState, int newTop);

        public static void lua_pop(IntPtr luaState, int amount)
        {
            LuaNativeMethods.lua_settop(luaState, -amount - 1);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_gettable(IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawget(IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_settable(IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_rawset(IntPtr luaState, int index);

#if LUA_5_3
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_setmetatable(IntPtr luaState, int objIndex);
#else
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_setmetatable(IntPtr luaState, int objIndex);
#endif

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_getmetatable(IntPtr luaState, int objIndex);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushvalue(IntPtr luaState, int index);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_gettop(IntPtr luaState);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern LuaTypes lua_type(IntPtr luaState, int index);

        public static bool lua_isnil(IntPtr luaState, int index)
        {
            return LuaNativeMethods.lua_type(luaState, index) == LuaTypes.TYPE_NIL;
        }

        public static bool lua_isnumber(IntPtr luaState, int index)
        {
            return LuaWrapperNativeMethods.lua_isnumber(luaState, index) > 0;
        }

        public static bool lua_isboolean(IntPtr luaState, int index)
        {
            return LuaNativeMethods.lua_type(luaState, index) == LuaTypes.TYPE_BOOLEAN;
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_ref(IntPtr luaState, int registryIndex);

        public static void lua_getref(IntPtr luaState, int reference)
        {
            LuaNativeMethods.lua_rawgeti(luaState, LuaIndexes.LUARegistryIndex, reference);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_unref(IntPtr luaState, int registryIndex, int reference);

        public static void lua_unref(IntPtr luaState, int reference)
        {
            LuaNativeMethods.luaL_unref(luaState, LuaIndexes.LUARegistryIndex, reference);
        }

        public static bool lua_isstring(IntPtr luaState, int index)
        {
            return LuaWrapperNativeMethods.lua_isstring(luaState, index) > 0;
        }

        public static bool lua_iscfunction(IntPtr luaState, int index)
        {
            return LuaWrapperNativeMethods.lua_iscfunction(luaState, index) > 0;
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnil(IntPtr luaState);

        public static void luaL_checktype(IntPtr luaState, int p, LuaTypes t)
        {
            LuaTypes ct = LuaNativeMethods.lua_type(luaState, p);
            if (ct != t)
            {
                throw new Exception(string.Format("arg {0} expect {1}, got {2}", p, lua_typenamestr(luaState, t), lua_typenamestr(luaState, ct)));
            }
        }

        public static void lua_pushcfunction(IntPtr luaState, LuaCSFunction function)
        {
            IntPtr fn = Marshal.GetFunctionPointerForDelegate(function);
            lua_pushcclosure(luaState, fn, 0);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_tocfunction(IntPtr luaState, int index);

        public static bool lua_toboolean(IntPtr luaState, int index)
        {
            return LuaWrapperNativeMethods.lua_toboolean(luaState, index) > 0;
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr luaS_tolstring32(IntPtr luaState, int index, out int strLen);

        public static string lua_tostring(IntPtr luaState, int index)
        {
            int strlen;

            IntPtr str = luaS_tolstring32(luaState, index, out strlen); // fix il2cpp 64 bit
            string s = null;
            if (strlen > 0 && str != IntPtr.Zero)
            {
                s = Marshal.PtrToStringAnsi(str);

                // fallback method
                if (s == null)
                {
                    byte[] b = new byte[strlen];
                    Marshal.Copy(str, b, 0, strlen);
                    s = System.Text.Encoding.Default.GetString(b);
                }
            }

            return s == null ? string.Empty : s;
        }

        public static byte[] lua_tobytes(IntPtr luaState, int index)
        {
            int strlen;

            IntPtr str = luaS_tolstring32(luaState, index, out strlen); // fix il2cpp 64 bit

            if (str != IntPtr.Zero)
            {
                byte[] bytes = new byte[strlen];
                Marshal.Copy(str, bytes, 0, strlen);
                return bytes;
            }

            return null;
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_atpanic(IntPtr luaState, LuaCSFunction panicf);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushnumber(IntPtr luaState, double number);

        public static void lua_pushboolean(IntPtr luaState, bool value)
        {
            LuaWrapperNativeMethods.lua_pushboolean(luaState, value ? 1 : 0);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushstring(IntPtr luaState, string str);

        public static void lua_pushlstring(IntPtr luaState, byte[] str, int size)
        {
            LuaWrapperNativeMethods.luaS_pushlstring(luaState, str, size);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaL_newmetatable(IntPtr luaState, string meta);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_getfield(IntPtr luaState, int stackPos, string meta);

        public static void luaL_getmetatable(IntPtr luaState, string meta)
        {
            LuaNativeMethods.lua_getfield(luaState, LuaIndexes.LUARegistryIndex, meta);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr luaL_checkudata(IntPtr luaState, int stackPos, string meta);

        public static bool luaL_getmetafield(IntPtr luaState, int stackPos, string field)
        {
            return LuaWrapperNativeMethods.luaL_getmetafield(luaState, stackPos, field) > 0;
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_load(IntPtr luaState, LuaChunkReader chunkReader, ref ReaderInfo data, string chunkName);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_error(IntPtr luaState);

        public static bool lua_checkstack(IntPtr luaState, int extra)
        {
            return LuaWrapperNativeMethods.lua_checkstack(luaState, extra) > 0;
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int lua_next(IntPtr luaState, int index);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushlightuserdata(IntPtr luaState, IntPtr udata);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaL_where(IntPtr luaState, int level);

        public static double luaL_checknumber(IntPtr luaState, int stackPos)
        {
            luaL_checktype(luaState, stackPos, LuaTypes.TYPE_NUMBER);
            return lua_tonumber(luaState, stackPos);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_concat(IntPtr luaState, int n);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaS_newuserdata(IntPtr luaState, int val);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_rawnetobj(IntPtr luaState, int obj);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr lua_touserdata(IntPtr luaState, int index);

        public static int lua_absindex(IntPtr luaState, int index)
        {
            return index > 0 ? index : lua_gettop(luaState) + index + 1;
        }

        public static int lua_upvalueindex(int i)
        {
#if LUA_5_3
            return LuaIndexes.LUA_REGISTRYINDEX - i;
#else
            return LuaIndexes.LUAGlobalIndex - i;
#endif
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void lua_pushcclosure(IntPtr ptr, IntPtr f, int nup);

        public static void lua_pushcclosure(IntPtr ptr, LuaCSFunction f, int nup)
        {
            IntPtr fn = Marshal.GetFunctionPointerForDelegate(f);
            lua_pushcclosure(ptr, fn, nup);
        }

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_checkVector2(IntPtr ptr, int p, out float x, out float y);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_checkVector3(IntPtr ptr, int p, out float x, out float y, out float z);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_checkVector4(IntPtr ptr, int p, out float x, out float y, out float z, out float w);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_checkQuaternion(IntPtr ptr, int p, out float x, out float y, out float z, out float w);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_checkColor(IntPtr ptr, int p, out float x, out float y, out float z, out float w);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaS_pushVector2(IntPtr ptr, float x, float y);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaS_pushVector3(IntPtr ptr, float x, float y, float z);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaS_pushVector4(IntPtr ptr, float x, float y, float z, float w);
        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaS_pushQuaternion(IntPtr ptr, float x, float y, float z, float w);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaS_pushColor(IntPtr ptr, float x, float y, float z, float w);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void luaS_setDataVec(IntPtr ptr, int p, float x, float y, float z, float w);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_checkluatype(IntPtr ptr, int p, string t);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_pushobject(IntPtr ptr, int index, string t, bool gco, int cref);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_getcacheud(IntPtr ptr, int index, int cref);

        [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int luaS_subclassof(IntPtr ptr, int index, string t);
    }
}
