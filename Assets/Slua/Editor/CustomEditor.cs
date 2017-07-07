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

using UnityEditor;
using UnityEngine;

namespace SLua
{
    [CustomEditor(typeof(LuaSvrGameObject))]
    public class LuaSvrEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            LuaSvrGameObject myTarget = (LuaSvrGameObject)target;
            int bytes = LuaNativeMethods.lua_gc(myTarget.State.StatePointer, LuaGCOptions.LUA_GCCOUNT, 0);
            EditorGUILayout.LabelField("Memory(Kb)", bytes.ToString());
            if (GUILayout.Button("Lua GC"))
            {
                LuaNativeMethods.lua_gc(myTarget.State.StatePointer, LuaGCOptions.LUA_GCCOLLECT, 0);
                System.GC.Collect();
            }
        }
    }
}
