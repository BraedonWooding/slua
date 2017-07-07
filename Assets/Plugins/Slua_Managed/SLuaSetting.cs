// The MIT License (MIT)

// Copyright 2015 Siney/Pangweiwei siney@yeah.net
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#if UNITY_EDITOR
using UnityEditor;
#endif
#if !SLUA_STANDALONE
using UnityEngine;
#endif

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