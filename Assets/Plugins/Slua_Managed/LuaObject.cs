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
using UnityEngine;

namespace SLua
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct)]
    public class CustomLuaClassAttribute : System.Attribute
    {
        public CustomLuaClassAttribute()
        {
        }
    }

    public class DoNotToLuaAttribute : System.Attribute
    {
        public DoNotToLuaAttribute()
        {
        }
    }

    public class LuaBinderAttribute : System.Attribute
    {
        public int Order;

        public LuaBinderAttribute(int order)
        {
            this.Order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class StaticExportAttribute : System.Attribute
    {
        public StaticExportAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class LuaOverrideAttribute : System.Attribute
    {
        public string FunctionName;

        public LuaOverrideAttribute(string functionName)
        {
            this.FunctionName = functionName;
        }
    }

    public class OverloadLuaClassAttribute : System.Attribute
    {
        private Type targetType;

        public OverloadLuaClassAttribute(Type target)
        {
            TargetType = target;
        }

        public Type TargetType
        {
            get
            {
                return targetType;
            }

            set
            {
                targetType = value;
            }
        }
    }

    public class LuaOut
    {
    }

    public partial class LuaObject
    {
        public const int VersionNumber = 0x1201;
        public const string DelgateTable = "__LuaDelegate";

        protected static LuaCSFunction luaGC = new LuaCSFunction(LuaGC);
        protected static LuaCSFunction luaAdd = new LuaCSFunction(LuaAdd);
        protected static LuaCSFunction luaSub = new LuaCSFunction(LuaSub);
        protected static LuaCSFunction luaMul = new LuaCSFunction(LuaMul);
        protected static LuaCSFunction luaDiv = new LuaCSFunction(LuaDiv);
        protected static LuaCSFunction luaUnm = new LuaCSFunction(LuaUnm);
        protected static LuaCSFunction luaEq = new LuaCSFunction(LuaEq);
        protected static LuaCSFunction luaLt = new LuaCSFunction(LuaLt);
        protected static LuaCSFunction luaLe = new LuaCSFunction(LuaLe);
        protected static LuaCSFunction luaToString = new LuaCSFunction(ToString);
        protected static LuaFunction newIndexFunction;
        protected static LuaFunction indexFunction;

        public static void Init(IntPtr ptr)
        {
            string newindexfun = @"

local getmetatable=getmetatable
local rawget=rawget
local error=error
local type=type
local function newindex(ud,k,v)
    local t=getmetatable(ud)
    repeat
        local h=rawget(t,k)
        if h then
            if h[2] then
                h[2](ud,v)
                return
            else
                error('property '..k..' is read only')
            end
        end
        t=rawget(t,'__parent')
    until t==nil
    error('can not find '..k)
end

return newindex
";

            string indexfun = @"
local type=type
local error=error
local rawget=rawget
local getmetatable=getmetatable
local function index(ud,k)
    local t=getmetatable(ud)
    repeat
        local fun=rawget(t,k)
        local tp=type(fun)
        if tp=='function' then
            return fun
        elseif tp=='table' then
            local f=fun[1]
            if f then
                return f(ud)
            else
                error('property '..k..' isthis.Write only')
            end
        end
        t = rawget(t,'__parent')
    until t==nil
    error('Can not find '..k)
end

return index
";
            LuaState state = LuaState.Get(ptr);
            newIndexFunction = (LuaFunction)state.DoString(newindexfun);
            indexFunction = (LuaFunction)state.DoString(indexfun);

            // object method
            LuaNativeMethods.lua_createtable(ptr, 0, 4);
            AddMember(ptr, ToString);
            AddMember(ptr, GetHashCode);
            AddMember(ptr, Equals);
            AddMember(ptr, GetType);
            LuaNativeMethods.lua_setfield(ptr, LuaIndexes.LUARegistryIndex, "__luabaseobject");

            LuaArray.Init(ptr);
            LuaVarObject.Init(ptr);

            LuaNativeMethods.lua_newtable(ptr);
            LuaNativeMethods.lua_setglobal(ptr, DelgateTable);
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ToString(IntPtr ptr)
        {
            try
            {
                object obj = CheckVar(ptr, 1);
                PushValue(ptr, true);
                PushValue(ptr, obj.ToString());
                return 2;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int GetHashCode(IntPtr ptr)
        {
            try
            {
                object obj = CheckVar(ptr, 1);
                PushValue(ptr, true);
                PushValue(ptr, obj.GetHashCode());
                return 2;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Equals(IntPtr ptr)
        {
            try
            {
                object obj = CheckVar(ptr, 1);
                object other = CheckVar(ptr, 2);
                PushValue(ptr, true);
                PushValue(ptr, obj.Equals(other));
                return 2;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int GetType(IntPtr ptr)
        {
            try
            {
                object obj = CheckVar(ptr, 1);
                PushValue(ptr, true);
                PushObject(ptr, obj.GetType());
                return 2;
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        public static int GetOpFunction(IntPtr ptr, string f, string tip)
        {
            int err = PushTry(ptr);
            CheckLuaObject(ptr, 1);

            while (!LuaNativeMethods.lua_isnil(ptr, -1))
            {
                LuaNativeMethods.lua_getfield(ptr, -1, f);
                if (!LuaNativeMethods.lua_isnil(ptr, -1))
                {
                    LuaNativeMethods.lua_remove(ptr, -2);
                    break;
                }

                LuaNativeMethods.lua_pop(ptr, 1); // pop nil
                LuaNativeMethods.lua_getfield(ptr, -1, "__parent");
                LuaNativeMethods.lua_remove(ptr, -2); // pop base
            }

            if (LuaNativeMethods.lua_isnil(ptr, -1))
            {
                LuaNativeMethods.lua_pop(ptr, 1);
                throw new Exception(string.Format("No {0} operator", tip));
            }

            return err;
        }

        public static int LuaOp(IntPtr ptr, string f, string tip)
        {
            int err = GetOpFunction(ptr, f, tip);
            LuaNativeMethods.lua_pushvalue(ptr, 1);
            LuaNativeMethods.lua_pushvalue(ptr, 2);
            if (LuaNativeMethods.lua_pcall(ptr, 2, 1, err) != 0)
            {
                LuaNativeMethods.lua_pop(ptr, 1);
            }

            LuaNativeMethods.lua_remove(ptr, err);
            PushValue(ptr, true);
            LuaNativeMethods.lua_insert(ptr, -2);
            return 2;
        }

        public static int LuaUnaryOp(IntPtr ptr, string f, string tip)
        {
            int err = GetOpFunction(ptr, f, tip);
            LuaNativeMethods.lua_pushvalue(ptr, 1);
            if (LuaNativeMethods.lua_pcall(ptr, 1, 1, err) != 0)
            {
                LuaNativeMethods.lua_pop(ptr, 1);
            }

            LuaNativeMethods.lua_remove(ptr, err);
            PushValue(ptr, true);
            LuaNativeMethods.lua_insert(ptr, -2);
            return 2;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaAdd(IntPtr ptr)
        {
            try
            {
                return LuaOp(ptr, "op_Addition", "add");
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaSub(IntPtr ptr)
        {
            try
            {
                return LuaOp(ptr, "op_Subtraction", "sub");
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaMul(IntPtr ptr)
        {
            try
            {
                return LuaOp(ptr, "op_Multiply", "mul");
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaDiv(IntPtr ptr)
        {
            try
            {
                return LuaOp(ptr, "op_Division", "div");
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaUnm(IntPtr ptr)
        {
            try
            {
                return LuaUnaryOp(ptr, "op_UnaryNegation", "unm");
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaEq(IntPtr ptr)
        {
            try
            {
                return LuaOp(ptr, "op_Equality", "eq");
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaLt(IntPtr ptr)
        {
            try
            {
                return LuaOp(ptr, "op_LessThan", "lt");
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaLe(IntPtr ptr)
        {
            try
            {
                return LuaOp(ptr, "op_LessThanOrEqual", "le");
            }
            catch (Exception e)
            {
                return Error(ptr, e);
            }
        }

        public static void GetEnumTable(IntPtr ptr, string t)
        {
            NewTypeTable(ptr, t);
        }

        public static void GetTypeTable(IntPtr ptr, string t)
        {
            NewTypeTable(ptr, t);
            // for static
            LuaNativeMethods.lua_newtable(ptr);
            // for instance
            LuaNativeMethods.lua_newtable(ptr);
        }

        public static void NewTypeTable(IntPtr ptr, string name)
        {
            string[] subt = name.Split('.');

            LuaNativeMethods.lua_pushglobaltable(ptr);

            foreach (string t in subt)
            {
                LuaNativeMethods.lua_pushstring(ptr, t);
                LuaNativeMethods.lua_rawget(ptr, -2);
                if (LuaNativeMethods.lua_isnil(ptr, -1))
                {
                    LuaNativeMethods.lua_pop(ptr, 1);
                    LuaNativeMethods.lua_createtable(ptr, 0, 0);
                    LuaNativeMethods.lua_pushstring(ptr, t);
                    LuaNativeMethods.lua_pushvalue(ptr, -2);
                    LuaNativeMethods.lua_rawset(ptr, -4);
                }

                LuaNativeMethods.lua_remove(ptr, -2);
            }
        }

        public static void CreateTypeMetatable(IntPtr ptr, Type self)
        {
            CreateTypeMetatable(ptr, null, self, null);
        }

        public static void CreateTypeMetatable(IntPtr ptr, LuaCSFunction con, Type self)
        {
            CreateTypeMetatable(ptr, con, self, null);
        }

        public static void CheckMethodValid(LuaCSFunction f)
        {
#if UNITY_EDITOR
            if (f != null && !Attribute.IsDefined(f.Method, typeof(MonoPInvokeCallbackAttribute)))
            {
                Logger.LogError(string.Format("MonoPInvokeCallbackAttribute not defined for LuaCSFunction {0}.", f.Method));
            }
#endif
        }

        public static void CreateTypeMetatable(IntPtr ptr, LuaCSFunction con, Type self, Type parent)
        {
            CheckMethodValid(con);

            // set parent
            bool parentSet = false;
            LuaNativeMethods.lua_pushstring(ptr, "__parent");
            while (parent != null && parent != typeof(object) && parent != typeof(ValueType))
            {
                LuaNativeMethods.luaL_getmetatable(ptr, ObjectCache.GetAQName(parent));
                // if parentType is not exported to lua
                if (LuaNativeMethods.lua_isnil(ptr, -1))
                {
                    LuaNativeMethods.lua_pop(ptr, 1);
                    parent = parent.BaseType;
                }
                else
                {
                    LuaNativeMethods.lua_rawset(ptr, -3);

                    LuaNativeMethods.lua_pushstring(ptr, "__parent");
                    LuaNativeMethods.luaL_getmetatable(ptr, parent.FullName);
                    LuaNativeMethods.lua_rawset(ptr, -4);

                    parentSet = true;
                    break;
                }
            }

            if (!parentSet)
            {
                LuaNativeMethods.luaL_getmetatable(ptr, "__luabaseobject");
                LuaNativeMethods.lua_rawset(ptr, -3);
            }

            CompleteInstanceMeta(ptr, self);
            CompleteTypeMeta(ptr, con, self);

            LuaNativeMethods.lua_pop(ptr, 1); // pop type Table
        }

        public static void CompleteTypeMeta(IntPtr ptr, LuaCSFunction con, Type self)
        {
            LuaNativeMethods.lua_pushstring(ptr, ObjectCache.GetAQName(self));
            LuaNativeMethods.lua_setfield(ptr, -3, "__fullname");

            indexFunction.Push(ptr);
            LuaNativeMethods.lua_setfield(ptr, -2, "__index");

            newIndexFunction.Push(ptr);
            LuaNativeMethods.lua_setfield(ptr, -2, "__newindex");

            if (con == null)
            {
                con = NoConstructor;
            }

            PushValue(ptr, con);
            LuaNativeMethods.lua_setfield(ptr, -2, "__call");

            LuaNativeMethods.lua_pushcfunction(ptr, TypeToString);
            LuaNativeMethods.lua_setfield(ptr, -2, "__tostring");

            LuaNativeMethods.lua_pushvalue(ptr, -1);
            LuaNativeMethods.lua_setmetatable(ptr, -3);

            LuaNativeMethods.lua_setfield(ptr, LuaIndexes.LUARegistryIndex, self.FullName);
        }

        public static bool IsImplByLua(Type t)
        {
            return t == typeof(Color)
                || t == typeof(Vector2)
                || t == typeof(Vector3)
                || t == typeof(Vector4)
                || t == typeof(Quaternion);
        }

        public static void Register(IntPtr ptr, LuaCSFunction func, string ns)
        {
            CheckMethodValid(func);

            NewTypeTable(ptr, ns);
            PushValue(ptr, func);
            LuaNativeMethods.lua_setfield(ptr, -2, func.Method.Name);
            LuaNativeMethods.lua_pop(ptr, 1);
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int LuaGC(IntPtr ptr)
        {
            int index = LuaNativeMethods.luaS_rawnetobj(ptr, 1);
            if (index > 0)
            {
                ObjectCache t = ObjectCache.Get(ptr);
                t.GC(index);
            }

            return 0;
        }

        public static void GC(IntPtr ptr, int p, UnityEngine.Object o)
        {
            // set ud's metatable is nil avoid gc again
            LuaNativeMethods.lua_pushnil(ptr);
            LuaNativeMethods.lua_setmetatable(ptr, p);

            ObjectCache t = ObjectCache.Get(ptr);
            t.GC(o);
        }

        public static void CheckLuaObject(IntPtr ptr, int p)
        {
            LuaNativeMethods.lua_getmetatable(ptr, p);
            if (LuaNativeMethods.lua_isnil(ptr, -1))
            {
                LuaNativeMethods.lua_pop(ptr, 1);
                throw new Exception("expect luaobject as first argument");
            }
        }

        public static void PushObject(IntPtr ptr, object o)
        {
            ObjectCache oc = ObjectCache.Get(ptr);
            oc.Push(ptr, o);
        }

        public static void PushObject(IntPtr ptr, Array o)
        {
            ObjectCache oc = ObjectCache.Get(ptr);
            oc.Push(ptr, o);
        }

        // lightobj is non-exported object used for re-get from c#, not for lua
        public static void PushLightObject(IntPtr ptr, object t)
        {
            ObjectCache oc = ObjectCache.Get(ptr);
            oc.Push(ptr, t, false);
        }

        public static int PushTry(IntPtr ptr)
        {
            LuaState state = LuaState.Get(ptr);
            if (!state.IsMainThread())
            {
                Logger.LogError("Can't call lua function in bg thread");
                return 0;
            }

            return state.PushTry();
        }

        public static bool MatchType(IntPtr ptr, int p, LuaTypes lt, Type t)
        {
            if (t == typeof(object))
            {
                return true;
            }
            else if (t == typeof(Type) && IsTypeTable(ptr, p))
            {
                return true;
            }
            else if (t == typeof(char[]) || t == typeof(byte[]))
            {
                return lt == LuaTypes.TYPE_STRING;
            }

            switch (lt)
            {
                case LuaTypes.TYPE_NIL:
                    return !t.IsValueType && !t.IsPrimitive;
                case LuaTypes.TYPE_NUMBER:
#if LUA_5_3
                    if (LuaNativeMethods.lua_isinteger(ptr, p) > 0) {
                        return (t.IsPrimitive && t != typeof(float) && t != typeof(double)) || t.IsEnum;
                    }
                    else {
                        return t == typeof(float) || t == typeof(double);
                    }
#else
                    return t.IsPrimitive || t.IsEnum;
#endif
                case LuaTypes.TYPE_USERDATA:
                    object o = CheckObj(ptr, p);
                    Type ot = o.GetType();
                    return ot == t || ot.IsSubclassOf(t) || t.IsAssignableFrom(ot);
                case LuaTypes.TYPE_STRING:
                    return t == typeof(string);
                case LuaTypes.TYPE_BOOLEAN:
                    return t == typeof(bool);
                case LuaTypes.TYPE_TABLE:
                    if (t == typeof(LuaTable) || t.IsArray)
                    {
                        return true;
                    }
                    else if (t.IsValueType)
                    {
                        return true; // luaTypeCheck(ptr, p, t.Name);
                    }
                    else if (LuaNativeMethods.luaS_subclassof(ptr, p, t.Name) == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case LuaTypes.TYPE_FUNCTION:
                    return t == typeof(LuaFunction) || t.BaseType == typeof(MulticastDelegate);
                case LuaTypes.TYPE_THREAD:
                    return t == typeof(LuaThread);
            }

            return false;
        }

        public static bool IsTypeTable(IntPtr ptr, int p)
        {
            if (LuaNativeMethods.lua_type(ptr, p) != LuaTypes.TYPE_TABLE)
            {
                return false;
            }

            LuaNativeMethods.lua_pushstring(ptr, "__fullname");
            LuaNativeMethods.lua_rawget(ptr, p);
            if (LuaNativeMethods.lua_isnil(ptr, -1))
            {
                LuaNativeMethods.lua_pop(ptr, 1);
                return false;
            }

            return true;
        }

        public static bool IsLuaClass(IntPtr ptr, int p)
        {
            return LuaNativeMethods.luaS_subclassof(ptr, p, null) == 1;
        }

        public static bool IsLuaValueType(IntPtr ptr, int p)
        {
            return LuaNativeMethods.luaS_checkluatype(ptr, p, null) == 1;
        }

        public static bool MatchType(IntPtr ptr, int p, Type t1)
        {
            LuaTypes t = LuaNativeMethods.lua_type(ptr, p);
            return MatchType(ptr, p, t, t1);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, Type t1)
        {
            if (total - from + 1 != 1)
            {
                return false;
            }

            return MatchType(ptr, from, t1);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, Type t1, Type t2)
        {
            if (total - from + 1 != 2)
            {
                return false;
            }

            return MatchType(ptr, from, t1) && MatchType(ptr, from + 1, t2);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, Type t1, Type t2, Type t3)
        {
            if (total - from + 1 != 3)
            {
                return false;
            }

            return MatchType(ptr, from, t1) && MatchType(ptr, from + 1, t2) && MatchType(ptr, from + 2, t3);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, Type t1, Type t2, Type t3, Type t4)
        {
            if (total - from + 1 != 4)
            {
                return false;
            }

            return MatchType(ptr, from, t1) && MatchType(ptr, from + 1, t2) && MatchType(ptr, from + 2, t3) && MatchType(ptr, from + 3, t4);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5)
        {
            if (total - from + 1 != 5)
            {
                return false;
            }

            return MatchType(ptr, from, t1) && MatchType(ptr, from + 1, t2) && MatchType(ptr, from + 2, t3) && MatchType(ptr, from + 3, t4)
                && MatchType(ptr, from + 4, t5);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5, Type t6)
        {
            if (total - from + 1 != 6)
            {
                return false;
            }

            return MatchType(ptr, from, t1) && MatchType(ptr, from + 1, t2) && MatchType(ptr, from + 2, t3) && MatchType(ptr, from + 3, t4)
                && MatchType(ptr, from + 4, t5)
                && MatchType(ptr, from + 5, t6);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5, Type t6, Type t7)
        {
            if (total - from + 1 != 7)
            {
                return false;
            }

            return MatchType(ptr, from, t1) && MatchType(ptr, from + 1, t2) && MatchType(ptr, from + 2, t3) && MatchType(ptr, from + 3, t4)
                && MatchType(ptr, from + 4, t5)
                && MatchType(ptr, from + 5, t6)
                && MatchType(ptr, from + 6, t7);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5, Type t6, Type t7, Type t8)
        {
            if (total - from + 1 != 8)
            {
                return false;
            }

            return MatchType(ptr, from, t1) && MatchType(ptr, from + 1, t2) && MatchType(ptr, from + 2, t3) && MatchType(ptr, from + 3, t4)
                && MatchType(ptr, from + 4, t5)
                && MatchType(ptr, from + 5, t6)
                && MatchType(ptr, from + 6, t7)
                && MatchType(ptr, from + 7, t8);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5, Type t6, Type t7, Type t8, Type t9)
        {
            if (total - from + 1 != 9)
            {
                return false;
            }

            return MatchType(ptr, from, t1) && MatchType(ptr, from + 1, t2) && MatchType(ptr, from + 2, t3) && MatchType(ptr, from + 3, t4)
                && MatchType(ptr, from + 4, t5)
                && MatchType(ptr, from + 5, t6)
                && MatchType(ptr, from + 6, t7)
                && MatchType(ptr, from + 7, t8)
                && MatchType(ptr, from + 8, t9);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, Type t1, Type t2, Type t3, Type t4, Type t5, Type t6, Type t7, Type t8, Type t9, Type t10)
        {
            if (total - from + 1 != 10)
            {
                return false;
            }

            return MatchType(ptr, from, t1) && MatchType(ptr, from + 1, t2) && MatchType(ptr, from + 2, t3) && MatchType(ptr, from + 3, t4)
                && MatchType(ptr, from + 4, t5)
                    && MatchType(ptr, from + 5, t6)
                    && MatchType(ptr, from + 6, t7)
                    && MatchType(ptr, from + 7, t8)
                    && MatchType(ptr, from + 8, t9)
                    && MatchType(ptr, from + 9, t10);
        }

        public static bool MatchType(IntPtr ptr, int total, int from, params Type[] t)
        {
            if (total - from + 1 != t.Length)
            {
                return false;
            }

            for (int i = 0; i < t.Length; ++i)
            {
                if (!MatchType(ptr, from + i, t[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool MatchType(IntPtr ptr, int total, int from, ParameterInfo[] pars)
        {
            if (total - from + 1 != pars.Length)
            {
                return false;
            }

            for (int n = 0; n < pars.Length; n++)
            {
                int p = n + from;
                LuaTypes t = LuaNativeMethods.lua_type(ptr, p);
                if (!MatchType(ptr, p, t, pars[n].ParameterType))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool LuaTypeCheck(IntPtr ptr, int p, string t)
        {
            return LuaNativeMethods.luaS_checkluatype(ptr, p, t) != 0;
        }

        public static LuaDelegate NewDelegate(IntPtr ptr, int p)
        {
            LuaState state = LuaState.Get(ptr);

            LuaNativeMethods.lua_pushvalue(ptr, p); // push function

            int fref = LuaNativeMethods.luaL_ref(ptr, LuaIndexes.LUARegistryIndex); // new ref function
            LuaDelegate f = new LuaDelegate(ptr, fref);
            LuaNativeMethods.lua_pushvalue(ptr, p);
            LuaNativeMethods.lua_pushinteger(ptr, fref);
            LuaNativeMethods.lua_settable(ptr, -3); // __LuaDelegate[func]= fref
            state.DelegateMap[fref] = f;
            return f;
        }

        public static void RemoveDelgate(IntPtr ptr, int r)
        {
            LuaNativeMethods.lua_getglobal(ptr, DelgateTable);
            LuaNativeMethods.lua_getref(ptr, r); // push key
            LuaNativeMethods.lua_pushnil(ptr); // push nil value
            LuaNativeMethods.lua_settable(ptr, -3); // remove function from __LuaDelegate table
            LuaNativeMethods.lua_pop(ptr, 1); // pop __LuaDelegate
        }

        public static object CheckObj(IntPtr ptr, int p)
        {
            ObjectCache oc = ObjectCache.Get(ptr);
            return oc.Get(ptr, p);
        }

        public static bool CheckArray<T>(IntPtr ptr, int p, out T[] ta)
        {
            if (LuaNativeMethods.lua_type(ptr, p) == LuaTypes.TYPE_TABLE)
            {
                int n = LuaNativeMethods.lua_rawlen(ptr, p);
                ta = new T[n];
                for (int k = 0; k < n; k++)
                {
                    LuaNativeMethods.lua_rawgeti(ptr, p, k + 1);
                    object o = CheckVar(ptr, -1);
                    Type fromT = o.GetType();
                    Type toT = typeof(T);

                    if (toT.IsAssignableFrom(fromT))
                    {
                        ta[k] = (T)o;
                    }
                    else
                    {
                        ta[k] = (T)Convert.ChangeType(o, typeof(T));
                    }

                    LuaNativeMethods.lua_pop(ptr, 1);
                }

                return true;
            }
            else
            {
                Array array = CheckObj(ptr, p) as Array;
                ta = array as T[];
                return ta != null;
            }
        }

        public static bool CheckParams<T>(IntPtr ptr, int p, out T[] pars) where T : class
        {
            int top = LuaNativeMethods.lua_gettop(ptr);
            if (top - p >= 0)
            {
                pars = new T[top - p + 1];
                for (int n = p, k = 0; n <= top; n++, k++)
                {
                    CheckType(ptr, n, out pars[k]);
                }

                return true;
            }

            pars = new T[0];
            return true;
        }

        public static bool CheckValueParams<T>(IntPtr ptr, int p, out T[] pars) where T : struct
        {
            int top = LuaNativeMethods.lua_gettop(ptr);
            if (top - p >= 0)
            {
                pars = new T[top - p + 1];
                for (int n = p, k = 0; n <= top; n++, k++)
                {
                    CheckValueType(ptr, n, out pars[k]);
                }

                return true;
            }

            pars = new T[0];
            return true;
        }

        public static bool CheckParams(IntPtr ptr, int p, out float[] pars)
        {
            int top = LuaNativeMethods.lua_gettop(ptr);
            if (top - p >= 0)
            {
                pars = new float[top - p + 1];
                for (int n = p, k = 0; n <= top; n++, k++)
                {
                    CheckType(ptr, n, out pars[k]);
                }

                return true;
            }

            pars = new float[0];
            return true;
        }

        public static bool CheckParams(IntPtr ptr, int p, out int[] pars)
        {
            int top = LuaNativeMethods.lua_gettop(ptr);
            if (top - p >= 0)
            {
                pars = new int[top - p + 1];
                for (int n = p, k = 0; n <= top; n++, k++)
                {
                    CheckType(ptr, n, out pars[k]);
                }

                return true;
            }

            pars = new int[0];
            return true;
        }

        public static bool CheckParams(IntPtr ptr, int p, out string[] pars)
        {
            int top = LuaNativeMethods.lua_gettop(ptr);
            if (top - p >= 0)
            {
                pars = new string[top - p + 1];
                for (int n = p, k = 0; n <= top; n++, k++)
                {
                    CheckType(ptr, n, out pars[k]);
                }

                return true;
            }

            pars = new string[0];
            return true;
        }

        public static bool CheckParams(IntPtr ptr, int p, out char[] pars)
        {
            LuaNativeMethods.luaL_checktype(ptr, p, LuaTypes.TYPE_STRING);
            string s;
            CheckType(ptr, p, out s);
            pars = s.ToCharArray();
            return true;
        }

        public static object CheckVar(IntPtr ptr, int p, Type t)
        {
            object obj = CheckVar(ptr, p);
            try
            {
                if (t.IsEnum)
                {
                    // double to int
                    object number = Convert.ChangeType(obj, typeof(int));
                    return Enum.ToObject(t, number);
                }

                object convertObj = null;

                if (obj != null)
                {
                    if (t.IsInstanceOfType(obj))
                    {
                        convertObj = obj; // if t is parent of obj, ignore change type
                    }
                    else
                    {
                        convertObj = Convert.ChangeType(obj, t);
                    }
                }

                return obj == null ? null : convertObj;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("parameter {0} expected {1}, got {2}, exception: {3}", p, t.Name, obj == null ? "null" : obj.GetType().Name, e.Message));
            }
        }

        public static object CheckVar(IntPtr ptr, int p)
        {
            LuaTypes type = LuaNativeMethods.lua_type(ptr, p);
            switch (type)
            {
                case LuaTypes.TYPE_NUMBER:
                    return LuaNativeMethods.lua_tonumber(ptr, p);
                case LuaTypes.TYPE_STRING:
                    return LuaNativeMethods.lua_tostring(ptr, p);
                case LuaTypes.TYPE_BOOLEAN:
                    return LuaNativeMethods.lua_toboolean(ptr, p);
                case LuaTypes.TYPE_FUNCTION:
                    {
                        LuaFunction v;
                        CheckType(ptr, p, out v);
                        return v;
                    }

                case LuaTypes.TYPE_TABLE:
                    {
                        if (IsLuaValueType(ptr, p))
                        {
                            if (LuaTypeCheck(ptr, p, "Vector2"))
                            {
                                Vector2 v;
                                CheckType(ptr, p, out v);
                                return v;
                            }
                            else if (LuaTypeCheck(ptr, p, "Vector3"))
                            {
                                Vector3 v;
                                CheckType(ptr, p, out v);
                                return v;
                            }
                            else if (LuaTypeCheck(ptr, p, "Vector4"))
                            {
                                Vector4 v;
                                CheckType(ptr, p, out v);
                                return v;
                            }
                            else if (LuaTypeCheck(ptr, p, "Quaternion"))
                            {
                                Quaternion v;
                                CheckType(ptr, p, out v);
                                return v;
                            }
                            else if (LuaTypeCheck(ptr, p, "Color"))
                            {
                                Color c;
                                CheckType(ptr, p, out c);
                                return c;
                            }

                            Logger.LogError("unknown lua value type");
                            return null;
                        }
                        else if (IsLuaClass(ptr, p))
                        {
                            return CheckObj(ptr, p);
                        }
                        else
                        {
                            LuaTable v;
                            CheckType(ptr, p, out v);
                            return v;
                        }
                    }

                case LuaTypes.TYPE_USERDATA:
                    return LuaObject.CheckObj(ptr, p);
                case LuaTypes.TYPE_THREAD:
                    {
                        LuaThread lt;
                        CheckType(ptr, p, out lt);
                        return lt;
                    }

                default:
                    return null;
            }
        }

        public static void PushValue(IntPtr ptr, object o)
        {
            PushVar(ptr, o);
        }

        public static void PushValue(IntPtr ptr, Array a)
        {
            PushObject(ptr, a);
        }

        public static void PushVar(IntPtr ptr, object o)
        {
            if (o == null)
            {
                LuaNativeMethods.lua_pushnil(ptr);
                return;
            }

            Type t = o.GetType();
            LuaState.PushVarDelegate push;
            LuaState ls = LuaState.Get(ptr);
            if (ls.TryGetTypePusher(t, out push))
            {
                push(ptr, o);
            }
            else if (t.IsEnum)
            {
                PushEnum(ptr, Convert.ToInt32(o));
            }
            else if (t.IsArray)
            {
                PushObject(ptr, (Array)o);
            }
            else
            {
                PushObject(ptr, o);
            }
        }

        public static T CheckSelf<T>(IntPtr ptr)
        {
            object o = CheckObj(ptr, 1);
            if (o != null)
            {
                return (T)o;
            }

            throw new Exception("arg 1 expect self, but get null");
        }

        public static object CheckSelf(IntPtr ptr)
        {
            object o = CheckObj(ptr, 1);
            if (o == null)
            {
                throw new Exception("expect self, but get null");
            }

            return o;
        }

        public static void SetBack(IntPtr ptr, object o)
        {
            ObjectCache t = ObjectCache.Get(ptr);
            t.SetBack(ptr, 1, o);
        }

        public static void SetBack(IntPtr ptr, Vector3 v)
        {
            LuaNativeMethods.luaS_setDataVec(ptr, 1, v.x, v.y, v.z, float.NaN);
        }

        public static void SetBack(IntPtr ptr, Vector2 v)
        {
            LuaNativeMethods.luaS_setDataVec(ptr, 1, v.x, v.y, float.NaN, float.NaN);
        }

        public static void SetBack(IntPtr ptr, Vector4 v)
        {
            LuaNativeMethods.luaS_setDataVec(ptr, 1, v.x, v.y, v.z, v.w);
        }

        public static void SetBack(IntPtr ptr, Quaternion v)
        {
            LuaNativeMethods.luaS_setDataVec(ptr, 1, v.x, v.y, v.z, v.w);
        }

        public static void SetBack(IntPtr ptr, Color v)
        {
            LuaNativeMethods.luaS_setDataVec(ptr, 1, v.r, v.g, v.b, v.a);
        }

        public static int ExtractFunction(IntPtr ptr, int p)
        {
            int op = 0;
            LuaTypes t = LuaNativeMethods.lua_type(ptr, p);
            switch (t)
            {
                case LuaTypes.TYPE_NIL:
                case LuaTypes.TYPE_USERDATA:
                    op = 0;
                    break;

                case LuaTypes.TYPE_TABLE:

                    LuaNativeMethods.lua_rawgeti(ptr, p, 1);
                    LuaNativeMethods.lua_pushstring(ptr, "+=");
                    if (LuaNativeMethods.lua_rawequal(ptr, -1, -2) == 1)
                    {
                        op = 1;
                    }
                    else
                    {
                        op = 2;
                    }

                    LuaNativeMethods.lua_pop(ptr, 2);
                    LuaNativeMethods.lua_rawgeti(ptr, p, 2);
                    break;
                case LuaTypes.TYPE_FUNCTION:
                    LuaNativeMethods.lua_pushvalue(ptr, p);
                    break;
                default:
                    throw new Exception("expect valid Delegate");
            }

            return op;
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int NoConstructor(IntPtr ptr)
        {
            return Error(ptr, "Can't new this object");
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int TypeToString(IntPtr ptr)
        {
            LuaNativeMethods.lua_pushstring(ptr, "__fullname");
            LuaNativeMethods.lua_rawget(ptr, -2);
            return 1;
        }

        public static int Error(IntPtr ptr, Exception e)
        {
            LuaNativeMethods.lua_pushboolean(ptr, false);
            LuaNativeMethods.lua_pushstring(ptr, e.ToString());
            return 2;
        }

        public static int Error(IntPtr ptr, string err)
        {
            LuaNativeMethods.lua_pushboolean(ptr, false);
            LuaNativeMethods.lua_pushstring(ptr, err);
            return 2;
        }

        public static int Error(IntPtr ptr, string err, params object[] args)
        {
            err = string.Format(err, args);
            LuaNativeMethods.lua_pushboolean(ptr, false);
            LuaNativeMethods.lua_pushstring(ptr, err);
            return 2;
        }

        public static int Ok(IntPtr ptr)
        {
            LuaNativeMethods.lua_pushboolean(ptr, true);
            return 1;
        }

        public static int Ok(IntPtr ptr, int retCount)
        {
            LuaNativeMethods.lua_pushboolean(ptr, true);
            LuaNativeMethods.lua_insert(ptr, -(retCount + 1));
            return retCount + 1;
        }

        public static void Assert(bool cond, string err)
        {
            if (!cond)
            {
                throw new Exception(err);
            }
        }

        /// <summary>
        /// Change Type, alternative for Convert.ChangeType, but has exception handling.
        /// change fail, return origin value directly, useful for some LuaVarObject value assign.
        /// </summary>
        public static object ChangeType(object obj, Type t)
        {
            if (t == typeof(object))
            {
                return obj;
            }

            if (obj.GetType() == t)
            {
                return obj;
            }

            try
            {
                return Convert.ChangeType(obj, t);
            }
            catch
            {
                return obj;
            }
        }

        protected static void AddMember(IntPtr ptr, LuaCSFunction func)
        {
            CheckMethodValid(func);

            PushValue(ptr, func);
            string name = func.Method.Name;
            if (name.EndsWith("_s"))
            {
                name = name.Substring(0, name.Length - 2);
                LuaNativeMethods.lua_setfield(ptr, -3, name);
            }
            else
            {
                LuaNativeMethods.lua_setfield(ptr, -2, name);
            }
        }

        protected static void AddMember(IntPtr ptr, LuaCSFunction func, bool instance)
        {
            CheckMethodValid(func);

            PushValue(ptr, func);
            string name = func.Method.Name;
            LuaNativeMethods.lua_setfield(ptr, instance ? -2 : -3, name);
        }

        protected static void AddMember(IntPtr ptr, string name, LuaCSFunction get, LuaCSFunction set, bool instance)
        {
            CheckMethodValid(get);
            CheckMethodValid(set);

            int t = instance ? -2 : -3;

            LuaNativeMethods.lua_createtable(ptr, 2, 0);
            if (get == null)
            {
                LuaNativeMethods.lua_pushnil(ptr);
            }
            else
            {
                PushValue(ptr, get);
            }

            LuaNativeMethods.lua_rawseti(ptr, -2, 1);

            if (set == null)
            {
                LuaNativeMethods.lua_pushnil(ptr);
            }
            else
            {
                PushValue(ptr, set);
            }

            LuaNativeMethods.lua_rawseti(ptr, -2, 2);

            LuaNativeMethods.lua_setfield(ptr, t, name);
        }

        protected static void AddMember(IntPtr ptr, int v, string name)
        {
            LuaNativeMethods.lua_pushinteger(ptr, v);
            LuaNativeMethods.lua_setfield(ptr, -2, name);
        }

        private static void CompleteInstanceMeta(IntPtr ptr, Type self)
        {
            LuaNativeMethods.lua_pushstring(ptr, "__typename");
            LuaNativeMethods.lua_pushstring(ptr, self.Name);
            LuaNativeMethods.lua_rawset(ptr, -3);

            // for instance
            indexFunction.Push(ptr);
            LuaNativeMethods.lua_setfield(ptr, -2, "__index");

            newIndexFunction.Push(ptr);
            LuaNativeMethods.lua_setfield(ptr, -2, "__newindex");

            PushValue(ptr, luaAdd);
            LuaNativeMethods.lua_setfield(ptr, -2, "__add");
            PushValue(ptr, luaSub);
            LuaNativeMethods.lua_setfield(ptr, -2, "__sub");
            PushValue(ptr, luaMul);
            LuaNativeMethods.lua_setfield(ptr, -2, "__mul");
            PushValue(ptr, luaDiv);
            LuaNativeMethods.lua_setfield(ptr, -2, "__div");
            PushValue(ptr, luaUnm);
            LuaNativeMethods.lua_setfield(ptr, -2, "__unm");
            PushValue(ptr, luaEq);
            LuaNativeMethods.lua_setfield(ptr, -2, "__eq");
            PushValue(ptr, luaLe);
            LuaNativeMethods.lua_setfield(ptr, -2, "__le");
            PushValue(ptr, luaLt);
            LuaNativeMethods.lua_setfield(ptr, -2, "__lt");
            PushValue(ptr, luaToString);
            LuaNativeMethods.lua_setfield(ptr, -2, "__tostring");

            LuaNativeMethods.lua_pushcfunction(ptr, LuaGC);
            LuaNativeMethods.lua_setfield(ptr, -2, "__gc");

            if (self.IsValueType && IsImplByLua(self))
            {
                LuaNativeMethods.lua_pushvalue(ptr, -1);
                LuaNativeMethods.lua_setglobal(ptr, self.FullName + ".Instance");
            }

            LuaNativeMethods.lua_setfield(ptr, LuaIndexes.LUARegistryIndex, ObjectCache.GetAQName(self));
        }
    }
}
