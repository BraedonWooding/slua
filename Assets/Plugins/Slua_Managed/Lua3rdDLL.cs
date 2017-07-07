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
using System.Reflection;

namespace SLua
{
    public static class Lua3rdDLL
    {
        static Lua3rdDLL()
        {
            // LuaSocketDLL.Reg(DLLRegFuncs);
            DLLRegFuncs = new Dictionary<string, LuaCSFunction>();
        }

        public static Dictionary<string, LuaCSFunction> DLLRegFuncs { get; private set; }

        public static void Open(IntPtr ptr)
        {
            List<string> typenames = Lua3rdMeta.Instance.TypesWithAttributes;
            Assembly[] assemblys = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly = null;
            foreach (Assembly ass in assemblys)
            {
                if (ass.GetName().Name == "Assembly-CSharp")
                {
                    assembly = ass;
                    break;
                }
            }

            if (assembly != null)
            {
                foreach (string typename in typenames)
                {
                    Type type = assembly.GetType(typename);
                    MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
                    foreach (MethodInfo method in methods)
                    {
                        LualibRegAttribute attr = System.Attribute.GetCustomAttribute(method, typeof(LualibRegAttribute)) as LualibRegAttribute;
                        if (attr != null)
                        {
                            LuaCSFunction csfunc = Delegate.CreateDelegate(typeof(LuaCSFunction), method) as LuaCSFunction;
                            DLLRegFuncs.Add(attr.LuaName, csfunc);
                        }
                    }
                }
            }

            if (DLLRegFuncs.Count == 0)
            {
                return;
            }

            LuaNativeMethods.lua_getglobal(ptr, "package");
            LuaNativeMethods.lua_getfield(ptr, -1, "preload");
            foreach (KeyValuePair<string, LuaCSFunction> pair in DLLRegFuncs)
            {
                LuaNativeMethods.lua_pushcfunction(ptr, pair.Value);
                LuaNativeMethods.lua_setfield(ptr, -2, pair.Key);
            }

            LuaNativeMethods.lua_settop(ptr, 0);
        }

        [AttributeUsage(AttributeTargets.Method)]
        public class LualibRegAttribute : System.Attribute
        {
            public LualibRegAttribute(string luaName)
            {
                this.LuaName = luaName;
            }

            public string LuaName { get; private set; }
        }
    }
}
