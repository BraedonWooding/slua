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
using System.Reflection;

namespace SLua
{
    /// <summary>
    /// This class use reflection and not completed so you shouldn't this.  Write your code for your purpose.
    /// </summary>
    public class LuaVarObject : LuaObject
    {
        /// <summary>
        /// A cache list of MemberInfo, for reflection optimize.
        /// </summary>
        private static Dictionary<Type, Dictionary<string, List<MemberInfo>>> cachedMemberInfos = new Dictionary<Type, Dictionary<string, List<MemberInfo>>>();

        public class MethodWrapper
        {
            private object self;
            private IList<MemberInfo> mis;

            public MethodWrapper(object self, IList<MemberInfo> mi)
            {
                this.self = self;
                this.mis = mi;
            }

            [MonoPInvokeCallback(typeof(LuaCSFunction))]
            public static int LUAIndex(IntPtr ptr)
            {
                try
                {
                    ObjectCache oc = ObjectCache.Get(ptr);
                    object self = oc.Get(ptr, 1);

                    LuaTypes t = LuaNativeMethods.lua_type(ptr, 2);
                    switch (t)
                    {
                        case LuaTypes.TYPE_STRING:
                            return IndexString(ptr, self, LuaNativeMethods.lua_tostring(ptr, 2));
                        case LuaTypes.TYPE_NUMBER:
                            return IndexInt(ptr, self, LuaNativeMethods.lua_tointeger(ptr, 2));
                        default:
                            return IndexObject(ptr, self, CheckObj(ptr, 2));
                    }
                }
                catch (Exception e)
                {
                    return Error(ptr, e);
                }
            }

            public static int IndexObject(IntPtr ptr, object self, object key)
            {
                if (self is IDictionary)
                {
                    IDictionary dict = self as IDictionary;
                    object v = dict[key];
                    LuaObject.PushValue(ptr, true);
                    PushVar(ptr, v);
                    return 2;
                }

                return 0;
            }

            public static Type GetType(object o)
            {
                if (o is LuaClassObject)
                {
                    return (o as LuaClassObject).GetClsType();
                }

                return o.GetType();
            }

            public static int IndexString(IntPtr ptr, object self, string key)
            {
                Type t = GetType(self);

                if (self is IDictionary)
                {
                    if (t.IsGenericType && t.GetGenericArguments()[0] != typeof(string))
                    {
                        goto IndexProperty;
                    }

                    object v = (self as IDictionary)[key];
                    if (v != null)
                    {
                        LuaObject.PushValue(ptr, true);
                        PushVar(ptr, v);
                        return 2;
                    }
                }

                IndexProperty:

                IList<MemberInfo> mis = GetCacheMembers(t, key);
                if (mis == null || mis.Count == 0)
                {
                    return Error(ptr, "Can't find " + key);
                }

                LuaObject.PushValue(ptr, true);
                MemberInfo mi = mis[0];
                switch (mi.MemberType)
                {
                    case MemberTypes.Property:
                        PropertyInfo p = (PropertyInfo)mi;
                        MethodInfo get = p.GetGetMethod(true);
                        PushVar(ptr, get.Invoke(self, null));
                        break;
                    case MemberTypes.Field:
                        FieldInfo f = (FieldInfo)mi;
                        PushVar(ptr, f.GetValue(self));
                        break;
                    case MemberTypes.Method:
                        LuaCSFunction ff = new MethodWrapper(self, mis).Invoke;
                        PushObject(ptr, ff);
                        break;
                    case MemberTypes.Event:
                        break;
                    default:
                        return 1;
                }

                return 2;
            }

            /// <summary>
            /// Collect Type Members, including base type.
            /// </summary>
            public static void CollectTypeMembers(Type type, ref Dictionary<string, List<MemberInfo>> membersMap)
            {
                MemberInfo[] mems = type.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly); // GetMembers can get basetType's members, but GetMember cannot
                for (int i = 0; i < mems.Length; i++)
                {
                    MemberInfo mem = mems[i];
                    List<MemberInfo> members;
                    if (!membersMap.TryGetValue(mem.Name, out members))
                    {
                        members = membersMap[mem.Name] = new List<MemberInfo>();
                    }

                    members.Add(mem);
                }

                if (type.BaseType != null)
                {
                    CollectTypeMembers(type.BaseType, ref membersMap);
                }
            }

            /// <summary>
            /// Get Member from Type, use reflection, use cache Dictionary.
            /// </summary>
            public static IList<MemberInfo> GetCacheMembers(Type type, string key)
            {
                Dictionary<string, List<MemberInfo>> cache;
                if (!cachedMemberInfos.TryGetValue(type, out cache))
                {
                    cachedMemberInfos[type] = cache = new Dictionary<string, List<MemberInfo>>();

                    // Get Member including all parent fields
                    CollectTypeMembers(type, ref cache);
                }

                return cache[key];
            }

            public static int NewIndexString(IntPtr ptr, object self, string key)
            {
                if (self is IDictionary)
                {
                    Type dictType = GetType(self);
                    Type valueType = dictType.GetGenericArguments()[1];
                    (self as IDictionary)[key] = LuaObject.CheckVar(ptr, 3, valueType);
                    return Ok(ptr);
                }

                Type t = GetType(self);

                IList<MemberInfo> mis = GetCacheMembers(t, key);
                if (mis == null || mis.Count == 0)
                {
                    return Error(ptr, "Can't find " + key);
                }

                MemberInfo mi = mis[0];
                object value;

                switch (mi.MemberType)
                {
                    case MemberTypes.Property:
                        PropertyInfo p = (PropertyInfo)mi;
                        MethodInfo set = p.GetSetMethod(true);
                        value = LuaObject.CheckVar(ptr, 3, p.PropertyType);
                        set.Invoke(self, new object[] { value });
                        break;
                    case MemberTypes.Field:
                        FieldInfo f = (FieldInfo)mi;
                        value = LuaObject.CheckVar(ptr, 3, f.FieldType);
                        f.SetValue(self, value);
                        break;
                    case MemberTypes.Method:
                        return Error(ptr, "Method can't set");
                    case MemberTypes.Event:
                        return Error(ptr, "Event can't set");
                }

                return Ok(ptr);
            }

            public static int IndexInt(IntPtr ptr, object self, int index)
            {
                Type type = GetType(self);
                if (self is IList)
                {
                    LuaObject.PushValue(ptr, true);
                    PushVar(ptr, (self as IList)[index]);
                    return 2;
                }
                else if (self is IDictionary)
                {
                    IDictionary dict = (IDictionary)self;

                    // support enumerate key
                    object dictKey = index;
                    if (type.IsGenericType)
                    {
                        Type keyType = type.GetGenericArguments()[0];

                        if (keyType.IsEnum)
                        {
                            LuaObject.PushValue(ptr, true);
                            PushVar(ptr, dict[Enum.Parse(keyType, dictKey.ToString())]);
                            return 2;
                        }

                        dictKey = ChangeType(dictKey, keyType); // if key is not int but ushort/uint,  IDictionary will cannot find the key and return null!
                    }

                    LuaObject.PushValue(ptr, true);
                    PushVar(ptr, dict[dictKey]);
                    return 2;
                }

                return 0;
            }

            public static int NewIndexInt(IntPtr ptr, object self, int index)
            {
                Type type = GetType(self);
                if (self is IList)
                {
                    if (type.IsGenericType)
                    {
                        Type t = type.GetGenericArguments()[0];
                        (self as IList)[index] = ChangeType(LuaObject.CheckVar(ptr, 3), t);
                    }
                    else
                    {
                        (self as IList)[index] = LuaObject.CheckVar(ptr, 3);
                    }
                }
                else if (self is IDictionary)
                {
                    Type keyType = type.GetGenericArguments()[0];
                    object dictKey = index;
                    dictKey = ChangeType(dictKey, keyType); // if key is not int but ushort/uint,  IDictionary will cannot find the key and return null!

                    if (type.IsGenericType)
                    {
                        Type t = type.GetGenericArguments()[1];
                        (self as IDictionary)[dictKey] = ChangeType(LuaObject.CheckVar(ptr, 3), t);
                    }
                    else
                    {
                        (self as IDictionary)[dictKey] = LuaObject.CheckVar(ptr, 3);
                    }
                }

                LuaObject.PushValue(ptr, true);
                return 1;
            }

            public static int NewIndexObject(IntPtr ptr, object self, object k, object v)
            {
                if (self is IDictionary)
                {
                    IDictionary dict = self as IDictionary;
                    Type dictType = GetType(self);
                    Type valueType = dictType.GetGenericArguments()[1];

                    object key = k;
                    object value = ChangeType(v, valueType);
                    dict[key] = value;
                }

                return Ok(ptr);
            }

            [MonoPInvokeCallback(typeof(LuaCSFunction))]
            public static int LUANewIndex(IntPtr ptr)
            {
                try
                {
                    ObjectCache oc = ObjectCache.Get(ptr);
                    object self = oc.Get(ptr, 1);

                    LuaTypes t = LuaNativeMethods.lua_type(ptr, 2);
                    switch (t)
                    {
                        case LuaTypes.TYPE_STRING:
                            return NewIndexString(ptr, self, LuaNativeMethods.lua_tostring(ptr, 2));
                        case LuaTypes.TYPE_NUMBER:
                            return NewIndexInt(ptr, self, LuaNativeMethods.lua_tointeger(ptr, 2));
                        default:
                            return NewIndexObject(ptr, self, LuaObject.CheckVar(ptr, 2), LuaObject.CheckVar(ptr, 3));
                    }
                }
                catch (Exception e)
                {
                    return Error(ptr, e);
                }
            }

            [MonoPInvokeCallback(typeof(LuaCSFunction))]
            public static int WrapMethod(IntPtr ptr)
            {
                try
                {
                    ObjectCache oc = ObjectCache.Get(ptr);
                    LuaCSFunction func = (LuaCSFunction)oc.Get(ptr, 1);
                    return func(ptr);
                }
                catch (Exception e)
                {
                    return Error(ptr, e);
                }
            }

            public static void Init(IntPtr ptr)
            {
                LuaNativeMethods.lua_createtable(ptr, 0, 3);
                LuaObject.PushValue(ptr, LUAIndex);
                LuaNativeMethods.lua_setfield(ptr, -2, "__index");
                LuaObject.PushValue(ptr, LUANewIndex);
                LuaNativeMethods.lua_setfield(ptr, -2, "__newindex");
                LuaNativeMethods.lua_pushcfunction(ptr, LuaGC);
                LuaNativeMethods.lua_setfield(ptr, -2, "__gc");
                LuaNativeMethods.lua_setfield(ptr, LuaIndexes.LUARegistryIndex, "LuaVarObject");

                LuaNativeMethods.lua_createtable(ptr, 0, 1);
                LuaObject.PushValue(ptr, WrapMethod);
                LuaNativeMethods.lua_setfield(ptr, -2, "__call");
                LuaNativeMethods.lua_setfield(ptr, LuaIndexes.LUARegistryIndex, ObjectCache.GetAQName(typeof(LuaCSFunction)));
            }

            public bool MatchType(IntPtr ptr, int p, LuaTypes lt, Type t)
            {
                if (t.IsPrimitive && t != typeof(bool))
                {
                    return lt == LuaTypes.TYPE_NUMBER;
                }

                if (t == typeof(bool))
                {
                    return lt == LuaTypes.TYPE_BOOLEAN;
                }

                if (t == typeof(string))
                {
                    return lt == LuaTypes.TYPE_STRING;
                }

                switch (lt)
                {
                    case LuaTypes.TYPE_FUNCTION:
                        return t == typeof(LuaFunction) || t.BaseType == typeof(MulticastDelegate);
                    case LuaTypes.TYPE_TABLE:
                        return t == typeof(LuaTable) || LuaObject.LuaTypeCheck(ptr, p, t.Name);
                    default:
                        return lt == LuaTypes.TYPE_USERDATA || t == typeof(object);
                }
            }

            public bool MatchType(IntPtr ptr, int from, ParameterInfo[] parameterInfo, bool isStatic)
            {
                int top = LuaNativeMethods.lua_gettop(ptr);
                from = isStatic ? from : from + 1;

                if (top - from + 1 != parameterInfo.Length)
                {
                    return false;
                }

                for (int n = 0; n < parameterInfo.Length; n++)
                {
                    int p = n + from;
                    LuaTypes t = LuaNativeMethods.lua_type(ptr, p);
                    if (!MatchType(ptr, p, t, parameterInfo[n].ParameterType))
                    {
                        return false;
                    }
                }

                return true;
            }

            public object CheckVar(IntPtr ptr, int p, Type t)
            {
                string tn = t.Name;

                switch (tn)
                {
                    case "String":
                        string str;
                        if (CheckType(ptr, p, out str))
                        {
                            return str;
                        }

                        break;
                    case "Decimal":
                        return (decimal)LuaNativeMethods.lua_tonumber(ptr, p);
                    case "Int64":
                        return (long)LuaNativeMethods.lua_tonumber(ptr, p);
                    case "UInt64":
                        return (ulong)LuaNativeMethods.lua_tonumber(ptr, p);
                    case "Int32":
                        return (int)LuaNativeMethods.lua_tointeger(ptr, p);
                    case "UInt32":
                        return (uint)LuaNativeMethods.lua_tointeger(ptr, p);
                    case "Single":
                        return (float)LuaNativeMethods.lua_tonumber(ptr, p);
                    case "Double":
                        return (double)LuaNativeMethods.lua_tonumber(ptr, p);
                    case "Boolean":
                        return (bool)LuaNativeMethods.lua_toboolean(ptr, p);
                    case "Byte":
                        return (byte)LuaNativeMethods.lua_tointeger(ptr, p);
                    case "UInt16":
                        return (ushort)LuaNativeMethods.lua_tointeger(ptr, p);
                    case "Int16":
                        return (short)LuaNativeMethods.lua_tointeger(ptr, p);
                    default:
                        // Enum convert
                        if (t.IsEnum)
                        {
                            int num = LuaNativeMethods.lua_tointeger(ptr, p);
                            return Enum.ToObject(t, num);
                        }

                        return LuaObject.CheckVar(ptr, p);
                }

                return null;
            }

            public int Invoke(IntPtr ptr)
            {
                for (int k = 0; k < mis.Count; k++)
                {
                    MethodInfo m = (MethodInfo)mis[k];
                    if (MatchType(ptr, 2, m.GetParameters(), m.IsStatic))
                    {
                        return ForceInvoke(ptr, m);
                    }
                }

                // cannot find best match function, try call first one
                // return LuaObject.Error(ptr, "Can't find valid overload function {0} to invoke or parameter type mis-matched.", mis[0].Name);
                return ForceInvoke(ptr, mis[0] as MethodInfo);
            }

            public void CheckArgs(IntPtr ptr, int from, MethodInfo m, out object[] args)
            {
                ParameterInfo[] ps = m.GetParameters();
                args = new object[ps.Length];
                int k = 0;
                from = m.IsStatic ? from + 1 : from + 2;

                for (int n = from; n <= LuaNativeMethods.lua_gettop(ptr); n++, k++)
                {
                    if (k + 1 > ps.Length)
                    {
                        break;
                    }

                    args[k] = CheckVar(ptr, n, ps[k].ParameterType);
                }
            }

            /// <summary>
            /// Invoke a C# method without match check.
            /// </summary>
            private int ForceInvoke(IntPtr ptr, MethodInfo m)
            {
                object[] args;
                CheckArgs(ptr, 1, m, out args);
                object ret = m.Invoke(m.IsStatic ? null : self, args);
                ParameterInfo[] pis = m.GetParameters();
                LuaObject.PushValue(ptr, true);
                if (ret != null)
                {
                    PushVar(ptr, ret);
                    int ct = 2;
                    for (int i = 0; i < pis.Length; ++i)
                    {
                        ParameterInfo pi = pis[i];
                        if (pi.ParameterType.IsByRef || pi.IsOut)
                        {
                            LuaObject.PushValue(ptr, args[i]);
                            ++ct;
                        }
                    }

                    return ct;
                }

                return 1;
            }
        }
    }

    public class LuaClassObject
    {
        public LuaClassObject(Type t)
        {
            CLS = t;
        }

        public Type CLS { get; private set; }

        public Type GetClsType()
        {
            return CLS;
        }
    }
}
