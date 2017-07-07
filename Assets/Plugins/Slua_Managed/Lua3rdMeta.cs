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
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SLua
{
    public class Lua3rdMeta : ScriptableObject
    {
        private static Lua3rdMeta instance = null;

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static Lua3rdMeta Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<Lua3rdMeta>("lua3rdmeta");
                }
#if UNITY_EDITOR
                if (instance == null)
                {
                    instance = ScriptableObject.CreateInstance<Lua3rdMeta>();
                    string path = "Assets/Slua/Resources";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    UnityEditor.AssetDatabase.CreateAsset(instance, Path.Combine(path, "lua3rdmeta.asset"));
                }
#endif
                return instance;
            }
        }

        /// <summary>
        /// Cache class types here those contain 3rd dll attribute.
        /// </summary>
        public List<string> TypesWithAttributes { get; private set; }

        public void OnEnable()
        {
            this.hideFlags = HideFlags.NotEditable;
        }

#if UNITY_EDITOR
        public void ReBuildTypes()
        {
            this.TypesWithAttributes = new List<string>();
            Assembly assembly = null;
            foreach (Assembly assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assem.GetName().Name == "Assembly-CSharp")
                {
                    assembly = assem;
                    break;
                }
            }

            if (assembly != null)
            {
                Type[] types = assembly.GetExportedTypes();
                foreach (Type type in types)
                {
                    MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    foreach (MethodInfo method in methods)
                    {
                        if (method.IsDefined(typeof(Lua3rdDLL.LualibRegAttribute), false))
                        {
                            this.TypesWithAttributes.Add(type.FullName);
                            break;
                        }
                    }
                }
            }
        }
#endif
    }
}
