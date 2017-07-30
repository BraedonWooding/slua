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

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SLua
{
    public enum EOL
    {
        Native,
        CRLF,
        CR,
        LF,
    }

    public enum JITBUILDTYPE : int
    {
        none,
        X86,
        X64,
        GC64,
    }

    public class SLuaSetting : ScriptableObject
    {
        public EOL Eol = EOL.Native;
        public bool ExportExtensionMethod = true;
        public string UnityEngineGeneratePath = "Assets/Slua/LuaObject/";

        public JITBUILDTYPE JitType = JITBUILDTYPE.none;

        // public int debugPort=10240;
        // public string debugIP="0.0.0.0"; // no longer debugger built-in
        private static SLuaSetting instance = null;

        public static SLuaSetting Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<SLuaSetting>("setting");
#if UNITY_EDITOR
                    if (instance == null)
                    {
                        instance = SLuaSetting.CreateInstance<SLuaSetting>();
                        AssetDatabase.CreateAsset(instance, "Assets/Slua/Resources/setting.asset");
                    }
#endif
                }

                return instance;
            }
        }

#if UNITY_EDITOR
        [MenuItem("SLua/Setting")]
        public static void Open()
        {
            Selection.activeObject = Instance;
        }
#endif
    }
}