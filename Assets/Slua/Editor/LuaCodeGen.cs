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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SLua
{
    public interface ICustomExportPost
    {
    }

    public class PropPair
    {
        private string get = "null";
        private string set = "null";
        private bool isInstance = true;

        public string Get
        {
            get
            {
                return get;
            }

            set
            {
                get = value;
            }
        }

        public string Set
        {
            get
            {
                return set;
            }

            set
            {
                set = value;
            }
        }

        public bool IsInstance
        {
            get
            {
                return isInstance;
            }

            set
            {
                isInstance = value;
            }
        }
    }

    public class LuaCodeGen : MonoBehaviour
    {
        public static string GenPath = SLuaSetting.Instance.UnityEngineGeneratePath;
        public static string WrapperName = "sluaWrapper.dll";
        public static bool AutoRefresh = true;

        public delegate void ExportGenericDelegate(Type t, string ns);

        public static bool IsCompiling
        {
            get
            {
                if (EditorApplication.isCompiling)
                {
                    Debug.Log("Unity Editor is compiling, please wait.");
                }

                return EditorApplication.isCompiling;
            }
        }

        [MenuItem("SLua/All/Make")]
        public static void GenerateAll()
        {
            AutoRefresh = false;
            Generate();
            GenerateUI();
            GenerateAds();
            Custom();
            Generate3rdDll();
            AutoRefresh = true;
            AssetDatabase.Refresh();
        }

        public static bool FilterType(Type t, List<string> noUseList, List<string> uselist)
        {
            if (t.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                Debug.Log(t.Name + " is filtered out");
                return false;
            }

            // check type in uselist
            string fullName = t.FullName;
            if (uselist != null && uselist.Count > 0)
            {
                return uselist.Contains(fullName);
            }
            else
            {
                // check type not in nouselist
                foreach (string str in noUseList)
                {
                    if (fullName.Contains(str))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [MenuItem("SLua/Unity/Make UnityEngine")]
        public static void Generate()
        {
            GenerateFor("UnityEngine", "Unity/", 0, "BindUnity");
        }

        [MenuItem("SLua/Unity/Make UnityEngine.UI")]
        public static void GenerateUI()
        {
            GenerateFor("UnityEngine.UI", "Unity/", 1, "BindUnityUI");
        }

        [MenuItem("SLua/Unity/Make UnityEngine.Advertisements")]
        public static void GenerateAds()
        {
            GenerateFor("UnityEngine.Advertisements", "Unity/", 2, "BindUnityAds");
        }

        public static void GenerateFor(string asemblyName, string genAtPath, int genOrder, string bindMethod)
        {
            if (IsCompiling)
            {
                return;
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.Load(asemblyName);
            }
            catch (Exception)
            {
                return;
            }

            Type[] types = assembly.GetExportedTypes();

            List<string> uselist;
            List<string> noUseList;

            CustomExport.OnGetNoUseList(out noUseList);
            CustomExport.OnGetUseList(out uselist);

            // Get use and nouse list from custom export.
            object[] customExport = new object[1];
            InvokeEditorMethod<ICustomExportPost>("OnGetUseList", ref customExport);
            if (customExport[0] != null)
            {
                if (uselist != null)
                {
                    uselist.AddRange((List<string>)customExport[0]);
                }
                else
                {
                    uselist = (List<string>)customExport[0];
                }
            }

            customExport[0] = null;
            InvokeEditorMethod<ICustomExportPost>("OnGetNoUseList", ref customExport);
            if (customExport[0] != null)
            {
                if (noUseList != null)
                {
                    noUseList.AddRange((List<string>)customExport[0]);
                }
                else
                {
                    noUseList = (List<string>)customExport[0];
                }
            }

            List<Type> exports = new List<Type>();
            string path = GenPath + genAtPath;
            foreach (Type t in types)
            {
                if (FilterType(t, noUseList, uselist) && Generate(t, path))
                {
                    exports.Add(t);
                }
            }

            GenerateBind(exports, bindMethod, genOrder, path);
            if (AutoRefresh)
            {
                AssetDatabase.Refresh();
            }

            Debug.Log("Generate interface finished: " + asemblyName);
        }

        public static string FixPathName(string path)
        {
            if (path.EndsWith("\\") || path.EndsWith("/"))
            {
                return path.Substring(0, path.Length - 1);
            }

            return path;
        }

        [MenuItem("SLua/Unity/Clear Unity and UI")]
        public static void ClearUnity()
        {
            Clear(new string[] { GenPath + "Unity" });
            Debug.Log("Clear Unity & UI complete.");
        }

        [MenuItem("SLua/Custom/Make")]
        public static void Custom()
        {
            if (IsCompiling)
            {
                return;
            }

            List<Type> exports = new List<Type>();
            string path = GenPath + "Custom/";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            ExportGenericDelegate fun = (Type t, string ns) =>
            {
                if (Generate(t, ns, path))
                {
                    exports.Add(t);
                }
            };

            HashSet<string> namespaces = CustomExport.OnAddCustomNamespace();

            // Add custom namespaces.
            object[] customExport = null;
            List<object> customNs = LuaCodeGen.InvokeEditorMethod<ICustomExportPost>("OnAddCustomNamespace", ref customExport);
            foreach (object set in customNs)
            {
                foreach (string strNs in (HashSet<string>)set)
                {
                    namespaces.Add(strNs);
                }
            }

            Assembly assembly;
            Type[] types;

            try
            {
                // export plugin-dll
                assembly = Assembly.Load("Assembly-CSharp-firstpass");
                types = assembly.GetExportedTypes();
                foreach (Type t in types)
                {
                    if (t.IsDefined(typeof(CustomLuaClassAttribute), false) || namespaces.Contains(t.Namespace))
                    {
                        fun(t, null);
                    }
                }
            }
            catch (Exception)
            {
            }

            // export self-dll
            assembly = Assembly.Load("Assembly-CSharp");
            types = assembly.GetExportedTypes();
            foreach (Type t in types)
            {
                if (t.IsDefined(typeof(CustomLuaClassAttribute), false) || namespaces.Contains(t.Namespace))
                {
                    fun(t, null);
                }
            }

            CustomExport.OnAddCustomClass(fun);

            // detect interface ICustomExportPost,and call OnAddCustomClass
            customExport = new object[] { fun };
            InvokeEditorMethod<ICustomExportPost>("OnAddCustomClass", ref customExport);

            GenerateBind(exports, "BindCustom", 3, path);
            if (AutoRefresh)
            {
                AssetDatabase.Refresh();
            }

            Debug.Log("Generate custom interface finished");
        }

        public static List<object> InvokeEditorMethod<T>(string methodName, ref object[] parameters)
        {
            List<object> returnList = new List<object>();
            System.Reflection.Assembly editorAssembly = System.Reflection.Assembly.Load("Assembly-CSharp-Editor");
            Type[] editorTypes = editorAssembly.GetExportedTypes();
            foreach (Type t in editorTypes)
            {
                if (typeof(T).IsAssignableFrom(t))
                {
                    System.Reflection.MethodInfo method = t.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (method != null)
                    {
                        object res = method.Invoke(null, parameters);
                        if (res != null)
                        {
                            returnList.Add(res);
                        }
                    }
                }
            }

            return returnList;
        }

        public static List<object> GetEditorField<T>(string strFieldName)
        {
            List<object> returnList = new List<object>();
            System.Reflection.Assembly editorAssembly = System.Reflection.Assembly.Load("Assembly-CSharp-Editor");
            Type[] editorTypes = editorAssembly.GetExportedTypes();
            foreach (Type t in editorTypes)
            {
                if (typeof(T).IsAssignableFrom(t))
                {
                    FieldInfo fieldInfo = t.GetField(strFieldName, BindingFlags.Static | BindingFlags.Public);
                    if (fieldInfo != null)
                    {
                        object value = fieldInfo.GetValue(t);
                        if (value != null)
                        {
                            returnList.Add(value);
                        }
                    }
                }
            }

            return returnList;
        }

        [MenuItem("SLua/3rdDll/Make")]
        public static void Generate3rdDll()
        {
            if (IsCompiling)
            {
                return;
            }

            List<Type> cust = new List<Type>();
            List<string> assemblyList = new List<string>();
            CustomExport.OnAddCustomAssembly(ref assemblyList);

            // detect interface ICustomExportPost,and call OnAddCustomAssembly
            object[] customExport = new object[] { assemblyList };
            InvokeEditorMethod<ICustomExportPost>("OnAddCustomAssembly", ref customExport);

            foreach (string assemblyItem in assemblyList)
            {
                Assembly assembly = Assembly.Load(assemblyItem);
                Type[] types = assembly.GetExportedTypes();
                foreach (Type t in types)
                {
                    cust.Add(t);
                }
            }

            if (cust.Count > 0)
            {
                List<Type> exports = new List<Type>();
                string path = GenPath + "Dll/";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                foreach (Type t in cust)
                {
                    if (Generate(t, path))
                    {
                        exports.Add(t);
                    }
                }

                GenerateBind(exports, "BindDll", 2, path);
                if (AutoRefresh)
                {
                    AssetDatabase.Refresh();
                }

                Debug.Log("Generate 3rdDll interface finished");
            }
        }

        [MenuItem("SLua/3rdDll/Clear")]
        public static void Clear3rdDll()
        {
            Clear(new string[] { GenPath + "Dll" });
            Debug.Log("Clear AssemblyDll complete.");
        }

        [MenuItem("SLua/Custom/Clear")]
        public static void ClearCustom()
        {
            Clear(new string[] { GenPath + "Custom" });
            Debug.Log("Clear custom complete.");
        }

        [MenuItem("SLua/All/Clear")]
        public static void ClearALL()
        {
            Clear(new string[] { Path.GetDirectoryName(GenPath) });
            Debug.Log("Clear all complete.");
        }

        [MenuItem("SLua/Compile LuaObject To DLL")]
        public static void CompileDLL()
        {
            #region scripts
            List<string> scripts = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Script", new string[1] { Path.GetDirectoryName(GenPath) }).Distinct().ToArray();
            int guidCount = guids.Length;
            for (int i = 0; i < guidCount; i++)
            {
                // path may contains space
                string path = "\"" + AssetDatabase.GUIDToAssetPath(guids[i]) + "\"";
                if (!scripts.Contains(path))
                {
                    scripts.Add(path);
                }
            }

            if (scripts.Count == 0)
            {
                Debug.LogError("No Scripts");
                return;
            }
            #endregion

            #region libraries
            List<string> libraries = new List<string>();
            string[] referenced = new string[] { "UnityEngine", "UnityEngine.UI" };
            string projectPath = Path.GetFullPath(Application.dataPath + "/..").Replace("\\", "/");
            // http://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in
            foreach (Assembly assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                UriBuilder uri = new UriBuilder(assem.CodeBase);
                string path = Uri.UnescapeDataString(uri.Path).Replace("\\", "/");
                string name = Path.GetFileNameWithoutExtension(path);
                // ignore dll for Editor
                if ((path.StartsWith(projectPath) && !path.Contains("/Editor/") && !path.Contains("CSharp-Editor"))
                    || referenced.Contains(name))
                {
                    libraries.Add(path);
                }
            }
            #endregion

            // generate AssemblyInfo
            File.WriteAllText(Application.dataPath + "/AssemblyInfo.cs", string.Format("[assembly: UnityEngine.UnityAPICompatibilityVersionAttribute(\"{0}\")]", Application.unityVersion));

            #region mono compile            
            string editorData = EditorApplication.applicationContentsPath;
#if UNITY_EDITOR_OSX && !UNITY_5_4_OR_NEWER
            editorData += "/Frameworks";
#endif
            List<string> arg = new List<string>();
            arg.Add("/target:library");
            arg.Add("/sdk:2");
            arg.Add(string.Format("/out:\"{0}\"", WrapperName));
            arg.Add(string.Format("/r:\"{0}\"", string.Join("; ", libraries.ToArray())));
            arg.AddRange(scripts);
            arg.Add(Application.dataPath + "/AssemblyInfo.cs");

            const string ArgumentFile = "LuaCodeGen.txt";
            File.WriteAllLines(ArgumentFile, arg.ToArray());

            string mcs = editorData + "/MonoBleedingEdge/bin/mcs";
            // wrapping since we may have space
#if UNITY_EDITOR_WIN
            mcs += ".bat";
#endif
            #endregion

            #region execute bash
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            bool success = false;
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = mcs;
                process.StartInfo.Arguments = "@" + ArgumentFile;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                using (System.Threading.AutoResetEvent outputWaitHandle = new System.Threading.AutoResetEvent(false))
                using (System.Threading.AutoResetEvent errorWaitHandle = new System.Threading.AutoResetEvent(false))
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            output.AppendLine(e.Data);
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            error.AppendLine(e.Data);
                        }
                    };
                    // http://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    int timeout = 300;
                    if (process.WaitForExit(timeout * 1000) &&
                        outputWaitHandle.WaitOne(timeout * 1000) &&
                        errorWaitHandle.WaitOne(timeout * 1000))
                    {
                        success = process.ExitCode == 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
            #endregion

            Debug.Log(output.ToString());
            if (success)
            {
                Directory.Delete(GenPath, true);
                Directory.CreateDirectory(GenPath);
                File.Move(WrapperName, GenPath + WrapperName);
                // AssetDatabase.Refresh();
                File.Delete(ArgumentFile);
                File.Delete(Application.dataPath + "/AssemblyInfo.cs");
            }
            else
            {
                Debug.LogError(error.ToString());
            }
        }

        public static void Clear(string[] paths)
        {
            try
            {
                foreach (string path in paths)
                {
                    System.IO.Directory.Delete(path, true);
                    AssetDatabase.DeleteAsset(path);
                }
            }
            catch
            {
            }

            AssetDatabase.Refresh();
        }

        public static bool Generate(Type t, string path)
        {
            return Generate(t, null, path);
        }

        public static bool Generate(Type t, string ns, string path)
        {
            if (t.IsInterface)
            {
                return false;
            }

            CodeGenerator cg = new CodeGenerator()
            {
                GivenNamespace = ns,
                Path = path
            };
            return cg.Generate(t);
        }

        public static void GenerateBind(List<Type> list, string name, int order, string path)
        {
            // delete wrapper dll
            System.IO.File.Delete(GenPath + WrapperName);

            CodeGenerator cg = new CodeGenerator()
            {
                Path = path
            };
            cg.GenerateBind(list, name, order);
        }

        public struct ArgMode
        {
            public ArgMode(int index, int mode)
            {
                this.Index = index;
                this.Mode = mode;
            }

            public int Index { get; set; }

            public int Mode { get; set; }
        }

        [InitializeOnLoad]
        public class Startup
        {
            static Startup()
            {
                EditorApplication.update += Update;
                // use this delegation to ensure dispose luavm at last
                EditorApplication.playmodeStateChanged += () =>
                {
                    if (IsPlaying == true && EditorApplication.isPlaying == false)
                    {
                        if (LuaState.Main != null)
                        {
                            LuaState.Main.Dispose();
                        }
                    }

                    IsPlaying = EditorApplication.isPlaying;
                };
            }

            public static bool IsPlaying { get; private set; }

            public static void Update()
            {
                EditorApplication.update -= Update;
                Lua3rdMeta.Instance.ReBuildTypes();

                // Remind user to generate lua interface code
                bool remindGenerate = !EditorPrefs.HasKey("SLUA_REMIND_GENERTE_LUA_INTERFACE") || EditorPrefs.GetBool("SLUA_REMIND_GENERTE_LUA_INTERFACE");
                bool ok = System.IO.Directory.Exists(GenPath + "Unity") || System.IO.File.Exists(GenPath + WrapperName);
                if (!ok && remindGenerate)
                {
                    if (EditorUtility.DisplayDialog("Slua", "Not found lua interface for Unity, generate it now?", "Generate", "No"))
                    {
                        GenerateAll();
                    }
                    else
                    {
                        if (!EditorUtility.DisplayDialog("Slua", "Remind you next time when no lua interface found for Unity?", "OK",
                            "Don't remind me next time!"))
                        {
                            EditorPrefs.SetBool("SLUA_REMIND_GENERTE_LUA_INTERFACE", false);
                        }
                        else
                        {
                            EditorPrefs.SetBool("SLUA_REMIND_GENERTE_LUA_INTERFACE", true);
                        }
                    }
                }
            }
        }
    }

    public class CodeGenerator
    {
        private static readonly string[] KeyWords = { "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while" };

        private static Dictionary<System.Type, List<MethodInfo>> extensionMethods;
        private static Dictionary<Type, Type> overloadedClass;

        private static List<string> memberFilter = new List<string>
        {
            "AnimationClip.averageDuration",
            "AnimationClip.averageAngularSpeed",
            "AnimationClip.averageSpeed",
            "AnimationClip.apparentSpeed",
            "AnimationClip.isLooping",
            "AnimationClip.isAnimatorMotion",
            "AnimationClip.isHumanMotion",
            "AnimatorOverrideController.PerformOverrideClipListCleanup",
            "Caching.SetNoBackupFlag",
            "Caching.ResetNoBackupFlag",
            "Light.areaSize",
            "Security.GetChainOfTrustValue",
            "Texture2D.alphaIsTransparency",
            "WWW.movie",
            "WebCamTexture.MarkNonReadable",
            "WebCamTexture.isReadable",
            // i don't know why below 2 functions missed in iOS platform
            "*.OnRebuildRequested",
            // il2cpp not exixts
            "Application.ExternalEval",
            "GameObject.networkView",
            "Component.networkView",
            // unity5
            "AnimatorControllerParameter.name",
            "Input.IsJoystickPreconfigured",
            "Resources.LoadAssetAtPath",
#if UNITY_4_6
            "Motion.ValidateIfRetargetable",
            "Motion.averageDuration",
            "Motion.averageAngularSpeed",
            "Motion.averageSpeed",
            "Motion.apparentSpeed",
            "Motion.isLooping",
            "Motion.isAnimatorMotion",
            "Motion.isHumanMotion",
#endif

            "Light.lightmappingMode",
            "Light.lightmapBakeType",
            "MonoBehaviour.runInEditMode",
            "MonoBehaviour.useGUILayout",
            "PlayableGraph.CreateScriptPlayable",
        };

        private string[] prefix = new string[] { "System.Collections.Generic" };
        private HashSet<string> funcname = new HashSet<string>();
        private Dictionary<string, bool> directfunc = new Dictionary<string, bool>();
        private bool includeExtension = SLuaSetting.Instance.ExportExtensionMethod;
        private EOL eol = SLuaSetting.Instance.Eol;
        private Dictionary<string, PropPair> propname = new Dictionary<string, PropPair>();
        private int indent = 0;

        static CodeGenerator()
        {
            FilterSpecMethods(out extensionMethods, out overloadedClass);
        }

        public string GivenNamespace { get; set; }

        public string Path { get; set; }

        public string NewLine
        {
            get
            {
                switch (eol)
                {
                    case EOL.Native:
                        return System.Environment.NewLine;
                    case EOL.CRLF:
                        return "\r\n";
                    case EOL.CR:
                        return "\r";
                    case EOL.LF:
                        return "\n";
                    default:
                        return string.Empty;
                }
            }
        }

        public static string NormalName(string name)
        {
            if (Array.BinarySearch<string>(KeyWords, name) >= 0)
            {
                return "@" + name;
            }

            return name;
        }

        // try filling generic parameters
        public static MethodInfo TryFixGenericMethod(MethodInfo method)
        {
            if (!method.ContainsGenericParameters)
            {
                return method;
            }

            try
            {
                Type[] genericTypes = method.GetGenericArguments();
                for (int j = 0; j < genericTypes.Length; j++)
                {
                    Type[] contraints = genericTypes[j].GetGenericParameterConstraints();
                    if (contraints != null && contraints.Length == 1 && contraints[0] != typeof(ValueType))
                    {
                        genericTypes[j] = contraints[0];
                    }
                    else
                    {
                        return method;
                    }
                }
                // only fixed here
                return method.MakeGenericMethod(genericTypes);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return method;
        }

        public static void FilterSpecMethods(out Dictionary<Type, List<MethodInfo>> dic, out Dictionary<Type, Type> overloadedClass)
        {
            dic = new Dictionary<Type, List<MethodInfo>>();
            overloadedClass = new Dictionary<Type, Type>();
            List<string> asems;
            CustomExport.OnGetAssemblyToGenerateExtensionMethod(out asems);

            // Get list from custom export.
            object[] customExport = new object[1];
            LuaCodeGen.InvokeEditorMethod<ICustomExportPost>("OnGetAssemblyToGenerateExtensionMethod", ref customExport);
            if (customExport[0] != null)
            {
                if (asems != null)
                {
                    asems.AddRange((List<string>)customExport[0]);
                }
                else
                {
                    asems = (List<string>)customExport[0];
                }
            }

            foreach (string assstr in asems)
            {
                Assembly assembly = Assembly.Load(assstr);
                foreach (Type type in assembly.GetExportedTypes())
                {
                    if (type.IsSealed && !type.IsGenericType && !type.IsNested)
                    {
                        MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
                        foreach (MethodInfo methodInfo in methods)
                        {
                            MethodInfo method = TryFixGenericMethod(methodInfo);
                            if (IsExtensionMethod(method))
                            {
                                Type extendedType = method.GetParameters()[0].ParameterType;
                                if (!dic.ContainsKey(extendedType))
                                {
                                    dic.Add(extendedType, new List<MethodInfo>());
                                }

                                dic[extendedType].Add(method);
                            }
                        }
                    }

                    if (type.IsDefined(typeof(OverloadLuaClassAttribute), false))
                    {
                        OverloadLuaClassAttribute olc = type.GetCustomAttributes(typeof(OverloadLuaClassAttribute), false)[0] as OverloadLuaClassAttribute;
                        if (olc != null)
                        {
                            if (overloadedClass.ContainsKey(olc.TargetType))
                            {
                                throw new Exception("Can't overload class more than once");
                            }

                            overloadedClass.Add(olc.TargetType, type);
                        }
                    }
                }
            }
        }

        public static bool IsExtensionMethod(MethodBase method)
        {
            return method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);
        }

        public void GenerateBind(List<Type> list, string name, int order)
        {
            HashSet<Type> exported = new HashSet<Type>();
            string f = System.IO.Path.Combine(Path, name + ".cs");
            StreamWriter file = new StreamWriter(f, false, Encoding.UTF8)
            {
                NewLine = NewLine
            };
            this.Write(file, "using System;");
            this.Write(file, "using System.Collections.Generic;");
            this.Write(file, "namespace SLua {");
            this.Write(file, "[LuaBinder({0})]", order);
            this.Write(file, "public class {0} {{", name);
            this.Write(file, "public static Action<IntPtr>[] GetBindList() {");
            this.Write(file, "Action<IntPtr>[] list= {");

            foreach (Type t in list)
            {
                this.WriteBindType(file, t, list, exported);
            }

            this.Write(file, "};");
            this.Write(file, "return list;");
            this.Write(file, "}");
            this.Write(file, "}");
            this.Write(file, "}");
            file.Close();
        }

        public void WriteBindType(StreamWriter file, Type t, List<Type> exported, HashSet<Type> binded)
        {
            if (t == null || binded.Contains(t) || !exported.Contains(t))
            {
                return;
            }

            this.WriteBindType(file, t.BaseType, exported, binded);
            this.Write(file, "{0}.Register,", ExportName(t), binded);
            binded.Add(t);
        }

        public string DelegateExportFilename(string path, Type t)
        {
            string f;
            if (t.IsGenericType)
            {
                f = path + string.Format("Lua{0}_{1}.cs", _Name(GenericBaseName(t)), _Name(GenericName(t)));
            }
            else
            {
                f = path + "LuaDelegate_" + _Name(t.FullName) + ".cs";
            }

            return f;
        }

        public bool Generate(Type t)
        {
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }

            if ((!t.IsGenericTypeDefinition && !IsObsolete(t) && t != typeof(YieldInstruction) && t != typeof(Coroutine))
                || (t.BaseType != null && t.BaseType == typeof(System.MulticastDelegate)))
            {
                if (t.IsNested && t.DeclaringType.IsPublic == false)
                {
                    return false;
                }

                if (t.IsEnum)
                {
                    StreamWriter file = Begin(t);
                    this.WriteHead(t, file);
                    RegisterEnumFunction(t, file);
                    End(file);
                }
                else if (t.BaseType == typeof(System.MulticastDelegate))
                {
                    if (t.ContainsGenericParameters)
                    {
                        return false;
                    }

                    string f = DelegateExportFilename(Path, t);

                    StreamWriter file = new StreamWriter(f, false, Encoding.UTF8)
                    {
                        NewLine = NewLine
                    };
                    this.WriteDelegate(t, file);
                    file.Close();
                    return false;
                }
                else
                {
                    funcname.Clear();
                    propname.Clear();
                    directfunc.Clear();

                    StreamWriter file = Begin(t);
                    this.WriteHead(t, file);
                    this.WriteConstructor(t, file);
                    this.WriteFunction(t, file, false);
                    this.WriteFunction(t, file, true);
                    this.WriteField(t, file);
                    RegisterFunction(t, file);
                    End(file);

                    if (t.BaseType != null && t.BaseType.Name.Contains("UnityEvent`"))
                    {
                        string basename = "LuaUnityEvent_" + _Name(GenericName(t.BaseType)) + ".cs";
                        string f = Path + basename;
                        string checkf = LuaCodeGen.GenPath + "Unity/" + basename;
                        if (!File.Exists(checkf))
                        {
                            // if had exported
                            file = new StreamWriter(f, false, Encoding.UTF8)
                            {
                                NewLine = NewLine
                            };
                            this.WriteEvent(t, file);
                            file.Close();
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public void WriteDelegate(Type t, StreamWriter file)
        {
            string temp = @"
using System;
using System.Collections.Generic;

namespace SLua
{
    public partial class LuaDelegation : LuaObject
    {
        public static int CheckDelegate(IntPtr ptr, int p, out $FN ua) {
            int op = ExtractFunction(ptr,p);
            if(LuaNativeMethods.lua_isnil(ptr,p)) {
                ua=null;
                return op;
            }
            else if (LuaNativeMethods.lua_isuserdata(ptr, p)==1)
            {
                ua = ($FN)CheckObj(ptr, p);
                return op;
            }

            LuaDelegate luaDelegate;
            CheckType(ptr, -1, out luaDelegate);
            LuaNativeMethods.lua_pop(ptr, 1);
            if(luaDelegate.Delegate != null)
            {
                ua = ($FN)luaDelegate.Delegate;
                return op;
            }
            
            ptr = LuaState.Get(ptr).StatePointer;
            ua = ($ARGS) =>
            {
                int error = PushTry(ptr);
";

            temp = temp.Replace("$TN", t.Name);
            temp = temp.Replace("$FN", SimpleType(t));
            MethodInfo mi = t.GetMethod("Invoke");

            temp = temp.Replace("$ARGS", ArgsList(mi));
            this.Write(file, temp);
            this.indent = 4;

            ParameterInfo[] pis = mi.GetParameters();

            for (int n = 0; n < pis.Length; n++)
            {
                if (!pis[n].IsOut)
                {
                    this.Write(file, "PushValue(ptr,a{0});", n + 1);
                }
            }

            int outcount = pis.Count((ParameterInfo p) =>
            {
                return p.ParameterType.IsByRef && p.IsOut;
            });

            this.Write(file, "luaDelegate.ProtectedCall({0}, error);", pis.Length - outcount);

            int offset = 0;
            if (mi.ReturnType != typeof(void))
            {
                offset = 1;
                this.WriteValueCheck(file, mi.ReturnType, offset, "ret", "error+");
            }

            for (int n = 0; n < pis.Length; n++)
            {
                if (pis[n].ParameterType.IsByRef)
                {
                    string a = string.Format("a{0}", n + 1);
                    this.WriteCheckType(file, pis[n].ParameterType, ++offset, a, "error+");
                }
            }

            this.Write(file, "LuaNativeMethods.lua_settop(ptr, error-1);");
            if (mi.ReturnType != typeof(void))
            {
                this.Write(file, "return ret;");
            }

            this.Write(file, "};");
            this.Write(file, "luaDelegate.Delegate = ua;");
            this.Write(file, "return op;");
            this.Write(file, "}");
            this.Write(file, "}");
            this.Write(file, "}");
        }

        public string ArgsList(MethodInfo m)
        {
            string str = string.Empty;
            ParameterInfo[] pars = m.GetParameters();
            for (int n = 0; n < pars.Length; n++)
            {
                string t = SimpleType(pars[n].ParameterType);

                ParameterInfo p = pars[n];
                if (p.ParameterType.IsByRef && p.IsOut)
                {
                    str += string.Format("out {0} a{1}", t, n + 1);
                }
                else if (p.ParameterType.IsByRef)
                {
                    str += string.Format("ref {0} a{1}", t, n + 1);
                }
                else
                {
                    str += string.Format("{0} a{1}", t, n + 1);
                }

                if (n < pars.Length - 1)
                {
                    str += ",";
                }
            }

            return str;
        }

        public void TryMake(Type t)
        {
            if (t.BaseType == typeof(System.MulticastDelegate))
            {
                CodeGenerator cg = new CodeGenerator();
                if (File.Exists(cg.DelegateExportFilename(LuaCodeGen.GenPath + "Unity/", t)))
                {
                    return;
                }

                cg.Path = this.Path;
                cg.Generate(t);
            }
        }

        public void WriteEvent(Type t, StreamWriter file)
        {
            string temp = @"
using System;
using System.Collections.Generic;

namespace SLua
{
    public class LuaUnityEvent_$CLS : LuaObject
    {
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        public static int AddListener(IntPtr ptr)
        {
            try
            {
                UnityEngine.Events.UnityEvent<$GN> self = CheckSelf<UnityEngine.Events.UnityEvent<$GN>>(ptr);
                UnityEngine.Events.UnityAction<$GN> a1;
                CheckType(ptr, 2, out a1);
                self.AddListener(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return Error(ptr,e);
            }
        }

        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        public static int RemoveListener(IntPtr ptr)
        {
            try
            {
                UnityEngine.Events.UnityEvent<$GN> self = CheckSelf<UnityEngine.Events.UnityEvent<$GN>>(ptr);
                UnityEngine.Events.UnityAction<$GN> a1;
                CheckType(ptr, 2, out a1);
                self.RemoveListener(a1);
                LuaObject.PushValue(ptr,true);
                return 1;
            }
            catch (Exception e)
            {
                return Error(ptr,e);
            }
        }

        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        public static int Invoke(IntPtr ptr)
        {
            try
            {
                UnityEngine.Events.UnityEvent<$GN> self = CheckSelf<UnityEngine.Events.UnityEvent<$GN>>(ptr);
" +
                GenericCallDecl(t.BaseType)
+ @"
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return Error(ptr,e);
            }
        }

        public static void Register(IntPtr ptr)
        {
            GetTypeTable(ptr, typeof(LuaUnityEvent_$CLS).FullName);
            AddMember(ptr, AddListener);
            AddMember(ptr, RemoveListener);
            AddMember(ptr, Invoke);
            CreateTypeMetatable(ptr, null, typeof(LuaUnityEvent_$CLS), typeof(UnityEngine.Events.UnityEventBase));
        }

        public static bool CheckType(IntPtr ptr, int p, out UnityEngine.Events.UnityAction<$GN> ua) {
            LuaNativeMethods.luaL_checktype(ptr, p, LuaTypes.TYPE_FUNCTION);
            LuaDelegate luaDelegate;
            CheckType(ptr, p, out luaDelegate);
            if (luaDelegate.Delegate != null)
            {
                ua = (UnityEngine.Events.UnityAction<$GN>)luaDelegate.Delegate;
                return true;
            }

            ptr = LuaState.Get(ptr).StatePointer;
            ua = ($ARGS) =>
            {
                int error = PushTry(ptr);
                $PushValueS
                luaDelegate.ProtectedCall($GENERICCOUNT, error);
                LuaNativeMethods.lua_settop(ptr, error - 1);
            };

            luaDelegate.Delegate = ua;
            return true;
        }
    }
}";

            temp = temp.Replace("$CLS", _Name(GenericName(t.BaseType)));
            temp = temp.Replace("$FNAME", FullName(t));
            temp = temp.Replace("$GN", GenericName(t.BaseType, ","));
            temp = temp.Replace("$ARGS", ArgsDecl(t.BaseType));
            temp = temp.Replace("$PushValueS", PushValues(t.BaseType));
            temp = temp.Replace("$GENERICCOUNT", t.BaseType.GetGenericArguments().Length.ToString());
            this.Write(file, temp);
        }

        public string GenericCallDecl(Type t)
        {
            try
            {
                Type[] tt = t.GetGenericArguments();
                string ret = string.Empty;
                string args = string.Empty;
                for (int n = 0; n < tt.Length; n++)
                {
                    string dt = SimpleType(tt[n]);
                    ret += string.Format("				{0} a{1};", dt, n) + NewLine;
                    // ret+=string.Format("CheckType(l,{0},out a{1});",n+2,n)+NewLine;
                    ret += "				" + GetCheckType(tt[n], n + 2, "a" + n) + NewLine;
                    args += "a" + n;
                    if (n < tt.Length - 1)
                    {
                        args += ",";
                    }
                }

                ret += string.Format("				self.Invoke({0});", args) + NewLine;
                return ret;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                return string.Empty;
            }
        }

        public string GetCheckType(Type t, int n, string v = "v", string prefix = "")
        {
            if (t.IsEnum)
            {
                return string.Format("CheckEnum(ptr, {2}{0}, out {1});", n, v, prefix);
            }
            else if (t.BaseType == typeof(System.MulticastDelegate))
            {
                return string.Format("int op=LuaDelegation.CheckDelegate(ptr, {2}{0}, out {1});", n, v, prefix);
            }
            else if (IsValueType(t))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    return string.Format("CheckNullable(ptr, {2}{0}, out {1});", n, v, prefix);
                }
                else
                {
                    return string.Format("CheckValueType(ptr, {2}{0}, out {1});", n, v, prefix);
                }
            }
            else if (t.IsArray)
            {
                return string.Format("CheckArray(ptr, {2}{0}, out {1});", n, v, prefix);
            }
            else
            {
                return string.Format("CheckType(ptr, {2}{0}, out {1});", n, v, prefix);
            }
        }

        public string PushValues(Type t)
        {
            try
            {
                Type[] tt = t.GetGenericArguments();
                string ret = string.Empty;
                for (int n = 0; n < tt.Length; n++)
                {
                    ret += "PushValue(ptr,v" + n + ");";
                }

                return ret;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                return string.Empty;
            }
        }

        public string ArgsDecl(Type t)
        {
            try
            {
                Type[] tt = t.GetGenericArguments();
                string ret = string.Empty;
                for (int n = 0; n < tt.Length; n++)
                {
                    string dt = SimpleType(tt[n]);
                    dt += " v" + n;
                    ret += dt;
                    if (n < tt.Length - 1)
                    {
                        ret += ",";
                    }
                }

                return ret;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                return string.Empty;
            }
        }

        public void RegisterEnumFunction(Type t, StreamWriter file)
        {
            // this.Write export function
            this.Write(file, "public static void Register(IntPtr ptr) {");
            this.Write(file, "GetEnumTable(ptr, \"{0}\");", string.IsNullOrEmpty(GivenNamespace) ? FullName(t) : GivenNamespace);

            foreach (string name in Enum.GetNames(t))
            {
                this.Write(file, "AddMember(ptr, {0}, \"{1}\");", Convert.ToInt32(Enum.Parse(t, name)), name);
            }

            this.Write(file, "LuaNativeMethods.lua_pop(ptr, 1);");
            this.Write(file, "}");
        }

        public StreamWriter Begin(Type t)
        {
            string clsname = ExportName(t);
            string f = Path + clsname + ".cs";
            StreamWriter file = new StreamWriter(f, false, Encoding.UTF8)
            {
                NewLine = NewLine
            };
            return file;
        }

        public bool IsPInvoke(MethodInfo mi, out bool instanceFunc)
        {
            if (mi.IsDefined(typeof(MonoPInvokeCallbackAttribute), false))
            {
                instanceFunc = !mi.IsDefined(typeof(StaticExportAttribute), false);
                return true;
            }

            instanceFunc = true;
            return false;
        }

        public string StaticName(string name)
        {
            if (name.StartsWith("op_"))
            {
                return name;
            }

            return name + "_s";
        }

        public bool MemberInFilter(Type t, MemberInfo mi)
        {
            return memberFilter.Contains(t.Name + "." + mi.Name) || memberFilter.Contains("*." + mi.Name);
        }

        public bool IsObsolete(MemberInfo t)
        {
            return t.IsDefined(typeof(ObsoleteAttribute), false);
        }

        public bool HasOverloadedVersion(Type t, ref string f)
        {
            Type ot;
            if (overloadedClass.TryGetValue(t, out ot))
            {
                MethodInfo mi = ot.GetMethod(f, BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (mi != null && mi.IsDefined(typeof(MonoPInvokeCallbackAttribute), false))
                {
                    f = FullName(ot) + "." + f;
                    return true;
                }
            }

            return false;
        }

        public void RegisterFunction(Type t, StreamWriter file)
        {
#if UNITY_5_3_OR_NEWER
            this.Write(file, "[UnityEngine.Scripting.Preserve]");
#endif
            // this.Write export function
            this.Write(file, "public static void Register(IntPtr ptr) {");

            if (t.BaseType != null && t.BaseType.Name.Contains("UnityEvent`"))
            {
                this.Write(file, "LuaUnityEvent_{1}.Register(ptr);", FullName(t), _Name(GenericName(t.BaseType)));
            }

            this.Write(file, "GetTypeTable(ptr, \"{0}\");", string.IsNullOrEmpty(GivenNamespace) ? FullName(t) : GivenNamespace);
            foreach (string i in funcname)
            {
                string f = i;
                if (HasOverloadedVersion(t, ref f))
                {
                    this.Write(file, "AddMember(ptr, {0});", f);
                }
                else
                {
                    this.Write(file, "AddMember(ptr, {0});", f);
                }
            }

            foreach (string f in directfunc.Keys)
            {
                bool instance = directfunc[f];
                this.Write(file, "AddMember(ptr, {0}, {1});", f, instance ? "true" : "false");
            }

            foreach (string f in propname.Keys)
            {
                PropPair pp = propname[f];
                this.Write(file, "AddMember(ptr, \"{0}\", {1}, {2}, {3});", f, pp.Get, pp.Set, pp.IsInstance ? "true" : "false");
            }

            if (t.BaseType != null && !CutBase(t.BaseType))
            {
                if (t.BaseType.Name.Contains("UnityEvent`"))
                {
                    this.Write(file, "CreateTypeMetatable(ptr, {2}, typeof({0}), typeof(LuaUnityEvent_{1}));", TypeDecl(t), _Name(GenericName(t.BaseType)), ConstructorOrNot(t));
                }
                else
                {
                    this.Write(file, "CreateTypeMetatable(ptr, {2}, typeof({0}), typeof({1}));", TypeDecl(t), TypeDecl(t.BaseType), ConstructorOrNot(t));
                }
            }
            else
            {
                this.Write(file, "CreateTypeMetatable(ptr, {1}, typeof({0}));", TypeDecl(t), ConstructorOrNot(t));
            }

            this.Write(file, "}");
        }

        public string ConstructorOrNot(Type t)
        {
            ConstructorInfo[] cons = GetValidConstructor(t);
            if (cons.Length > 0 || t.IsValueType)
            {
                return "Constructor";
            }

            return "null";
        }

        public bool CutBase(Type t)
        {
            if (t.FullName.StartsWith("System.Object"))
            {
                return true;
            }

            return false;
        }

        public void WriteSet(StreamWriter file, Type t, string cls, string fn, bool isstatic = false, bool canread = true)
        {
            if (t.BaseType == typeof(MulticastDelegate))
            {
                if (isstatic)
                {
                    this.Write(file, "if(op==0) {0}.{1}=v;", cls, fn);
                    if (canread)
                    {
                        this.Write(file, "else if(op==1) {0}.{1}+=v;", cls, fn);
                        this.Write(file, "else if(op==2) {0}.{1}-=v;", cls, fn);
                    }
                }
                else
                {
                    this.Write(file, "if(op==0) self.{0}=v;", fn);
                    if (canread)
                    {
                        this.Write(file, "else if(op==1) self.{0}+=v;", fn);
                        this.Write(file, "else if(op==2) self.{0}-=v;", fn);
                    }
                }
            }
            else
            {
                if (isstatic)
                {
                    this.Write(file, "{0}.{1}=v;", cls, fn);
                }
                else
                {
                    this.Write(file, "self.{0}=v;", fn);
                }
            }
        }

        // add namespace for extension method
        public void WriteExtraNamespace(StreamWriter file, Type t, HashSet<string> nsset)
        {
            List<MethodInfo> lstMI;
            if (extensionMethods.TryGetValue(t, out lstMI))
            {
                foreach (MethodInfo m in lstMI)
                {
                    // if notthis.Writed
                    if (!string.IsNullOrEmpty(m.ReflectedType.Namespace) && !nsset.Contains(m.ReflectedType.Namespace))
                    {
                        this.Write(file, "using {0};", m.ReflectedType.Namespace);
                        nsset.Add(m.ReflectedType.Namespace);
                    }
                }
            }
        }

        public void WriteItemFunc(Type t, StreamWriter file, List<PropertyInfo> getter, List<PropertyInfo> setter)
        {
            // Write property this[] set/get
            if (getter.Count > 0)
            {
                // get
                bool first_get = true;
                this.WriteFunctionAttr(file);
                this.Write(file, "public static int GetItem(IntPtr ptr) {");
                this.WriteTry(file);
                this.WriteCheckSelf(file, t);
                if (getter.Count == 1)
                {
                    PropertyInfo get = getter[0];
                    ParameterInfo[] infos = get.GetIndexParameters();
                    this.WriteValueCheck(file, infos[0].ParameterType, 2, "v");
                    this.Write(file, "var ret = self[v];");
                    this.WriteOk(file);
                    this.WritePushValue(get.PropertyType, file, "ret");
                    this.Write(file, "return 2;");
                }
                else
                {
                    this.Write(file, "LuaTypes t = LuaNativeMethods.lua_type(ptr, 2);");
                    for (int i = 0; i < getter.Count; i++)
                    {
                        PropertyInfo fii = getter[i];
                        ParameterInfo[] infos = fii.GetIndexParameters();
                        this.Write(file, "{0}(MatchType(ptr, 2, t, typeof({1}))){{", first_get ? "if" : "else if", infos[0].ParameterType);
                        this.WriteValueCheck(file, infos[0].ParameterType, 2, "v");
                        this.Write(file, "var ret = self[v];");
                        this.WriteOk(file);
                        this.WritePushValue(fii.PropertyType, file, "ret");
                        this.Write(file, "return 2;");
                        this.Write(file, "}");
                        first_get = false;
                    }

                    this.WriteNotMatch(file, "GetItem");
                }

                this.WriteCatchExecption(file);
                this.Write(file, "}");
                funcname.Add("GetItem");
            }

            if (setter.Count > 0)
            {
                bool first_set = true;
                this.WriteFunctionAttr(file);
                this.Write(file, "public static int SetItem(IntPtr ptr) {");
                this.WriteTry(file);
                this.WriteCheckSelf(file, t);
                if (setter.Count == 1)
                {
                    PropertyInfo set = setter[0];
                    ParameterInfo[] infos = set.GetIndexParameters();
                    this.WriteValueCheck(file, infos[0].ParameterType, 2);
                    this.WriteValueCheck(file, set.PropertyType, 3, "c");
                    this.Write(file, "self[v]=c;");
                    this.WriteOk(file);
                    this.Write(file, "return 1;");
                }
                else
                {
                    this.Write(file, "LuaTypes t = LuaNativeMethods.lua_type(ptr, 2);");
                    for (int i = 0; i < setter.Count; i++)
                    {
                        PropertyInfo fii = setter[i];
                        if (t.BaseType != typeof(MulticastDelegate))
                        {
                            ParameterInfo[] infos = fii.GetIndexParameters();
                            this.Write(file, "{0}(MatchType(ptr, 2, t, typeof({1}))){{", first_set ? "if" : "else if", infos[0].ParameterType);
                            this.WriteValueCheck(file, infos[0].ParameterType, 2, "v");
                            this.WriteValueCheck(file, fii.PropertyType, 3, "c");
                            this.Write(file, "self[v]=c;");
                            this.WriteOk(file);
                            this.Write(file, "return 1;");
                            this.Write(file, "}");
                            first_set = false;
                        }

                        if (t.IsValueType)
                        {
                            this.Write(file, "SetBack(ptr,self);");
                        }
                    }

                    this.WriteNotMatch(file, "SetItem");
                }

                this.WriteCatchExecption(file);
                this.Write(file, "}");
                funcname.Add("SetItem");
            }
        }

        public void WriteTry(StreamWriter file)
        {
            this.Write(file, "try {");
        }

        public void WriteCatchExecption(StreamWriter file)
        {
            this.Write(file, "}");
            this.Write(file, "catch(Exception e) {");
            this.Write(file, "return Error(ptr, e);");
            this.Write(file, "}");
        }

        public void WriteCheckType(StreamWriter file, Type t, int n, string v = "v", string nprefix = "")
        {
            if (t.IsEnum)
            {
                this.Write(file, "CheckEnum(ptr, {2}{0}, out {1});", n, v, nprefix);
            }
            else if (t.BaseType == typeof(System.MulticastDelegate))
            {
                this.Write(file, "int op = LuaDelegation.CheckDelegate(ptr, {2}{0}, out {1});", n, v, nprefix);
            }
            else if (IsValueType(t))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    this.Write(file, "CheckNullable(ptr, {2}{0}, out {1});", n, v, nprefix);
                }
                else
                {
                    this.Write(file, "CheckValueType(ptr, {2}{0}, out {1});", n, v, nprefix);
                }
            }
            else if (t.IsArray)
            {
                this.Write(file, "CheckArray(ptr, {2}{0}, out {1});", n, v, nprefix);
            }
            else
            {
                this.Write(file, "CheckType(ptr, {2}{0}, out {1});", n, v, nprefix);
            }
        }

        public void WriteValueCheck(StreamWriter file, Type t, int n, string v = "v", string nprefix = "")
        {
            this.Write(file, "{0} {1};", SimpleType(t), v);
            this.WriteCheckType(file, t, n, v, nprefix);
        }

        public void WriteOk(StreamWriter file)
        {
            this.Write(file, "PushValue(ptr, true);");
        }

        public void WriteBad(StreamWriter file)
        {
            this.Write(file, "PushValue(ptr, false);");
        }

        public void WriteError(StreamWriter file, string err)
        {
            this.WriteBad(file);
            this.Write(file, "LuaNativeMethods.lua_pushstring(ptr, \"{0}\");", err);
            this.Write(file, "return 2;");
        }

        public void WriteReturn(StreamWriter file, string val)
        {
            this.Write(file, "PushValue(ptr, true);");
            this.Write(file, "PushValue(ptr, {0});", val);
            this.Write(file, "return 2;");
        }

        public bool IsNotSupport(Type t)
        {
            if (t.IsSubclassOf(typeof(Delegate)))
            {
                return true;
            }

            return false;
        }

        public string RemoveRef(string s, bool removearray = true)
        {
            if (s.EndsWith("&"))
            {
                s = s.Substring(0, s.Length - 1);
            }

            if (s.EndsWith("[]") && removearray)
            {
                s = s.Substring(0, s.Length - 2);
            }

            if (s.StartsWith(prefix[0]))
            {
                s = s.Substring(prefix[0].Length + 1, s.Length - prefix[0].Length - 1);
            }

            s = s.Replace("+", ".");
            if (s.Contains("`"))
            {
                string regstr = @"`\d";
                Regex r = new Regex(regstr, RegexOptions.None);
                s = r.Replace(s, string.Empty);
                s = s.Replace("[", "<");
                s = s.Replace("]", ">");
            }

            return s;
        }

        public string GenericBaseName(Type t)
        {
            string n = t.FullName;
            if (n.IndexOf('[') > 0)
            {
                n = n.Substring(0, n.IndexOf('['));
            }

            return n.Replace("+", ".");
        }

        public string GenericName(Type t, string sep = "_")
        {
            try
            {
                Type[] tt = t.GetGenericArguments();
                string ret = string.Empty;
                for (int n = 0; n < tt.Length; n++)
                {
                    string dt = SimpleType(tt[n]);
                    ret += dt;
                    if (n < tt.Length - 1)
                    {
                        ret += sep;
                    }
                }

                return ret;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                return string.Empty;
            }
        }

        public string _Name(string n)
        {
            string ret = string.Empty;
            for (int i = 0; i < n.Length; i++)
            {
                if (char.IsLetterOrDigit(n[i]))
                {
                    ret += n[i];
                }
                else
                {
                    ret += "_";
                }
            }

            return ret;
        }

        public string TypeDecl(ParameterInfo[] pars, int paraOffset = 0)
        {
            string ret = string.Empty;
            for (int n = paraOffset; n < pars.Length; n++)
            {
                ret += ", typeof(";
                if (pars[n].IsOut)
                {
                    ret += "LuaOut";
                }
                else
                {
                    ret += SimpleType(pars[n].ParameterType);
                }

                ret += ")";
            }

            return ret;
        }

        // fill Generic Parameters if needed
        public string MethodDecl(MethodInfo m)
        {
            if (m.IsGenericMethod)
            {
                string parameters = string.Empty;
                bool first = true;
                foreach (Type genericType in m.GetGenericArguments())
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        parameters += ", ";
                    }

                    parameters += genericType.ToString();
                }

                return string.Format("{0}<{1}>", m.Name, parameters);
            }
            else
            {
                return m.Name;
            }
        }

        public bool IsUsefulMethod(MethodInfo method)
        {
            if (method.Name != "GetType" && method.Name != "GetHashCode" && method.Name != "Equals" &&
                method.Name != "ToString" && method.Name != "Clone" &&
                method.Name != "GetEnumerator" && method.Name != "CopyTo" &&
                method.Name != "op_Implicit" && method.Name != "op_Explicit" &&
                !method.Name.StartsWith("get_", StringComparison.Ordinal) &&
                !method.Name.StartsWith("set_", StringComparison.Ordinal) &&
                !method.Name.StartsWith("add_", StringComparison.Ordinal) &&
                !IsObsolete(method) && !method.ContainsGenericParameters && !method.IsGenericMethod &&
                method.ToString() != "Int32 Clamp(Int32, Int32, Int32)" &&
                !method.Name.StartsWith("remove_", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        public void WriteFunctionDec(StreamWriter file, string name)
        {
            this.WriteFunctionAttr(file);
            this.Write(file, "public static int {0}(IntPtr ptr) {{", name);
        }

        public MethodBase[] GetMethods(Type t, string name, BindingFlags bf)
        {
            List<MethodBase> methods = new List<MethodBase>();

            if (this.includeExtension && ((bf & BindingFlags.Instance) == BindingFlags.Instance))
            {
                if (extensionMethods.ContainsKey(t))
                {
                    foreach (MethodInfo m in extensionMethods[t])
                    {
                        if (m.Name == name
                           && !IsObsolete(m)
                           && !DontExport(m)
                           && IsUsefulMethod(m))
                        {
                            methods.Add(m);
                        }
                    }
                }
            }

            MemberInfo[] cons = t.GetMember(name, bf);
            foreach (MemberInfo memberInfo in cons)
            {
                MemberInfo member = memberInfo;
                if (member.MemberType == MemberTypes.Method)
                {
                    member = TryFixGenericMethod((MethodInfo)member);
                }

                if (member.MemberType == MemberTypes.Method
                    && !IsObsolete(member)
                    && !DontExport(member)
                    && IsUsefulMethod((MethodInfo)member))
                {
                    methods.Add((MethodBase)member);
                }
            }

            methods.Sort((a, b) =>
            {
                return a.GetParameters().Length - b.GetParameters().Length;
            });

            return methods.ToArray();
        }

        public void WriteNotMatch(StreamWriter file, string fn)
        {
            this.WriteError(file, string.Format("No matched override function {0} to call", fn));
        }

        public void WriteFunctionImpl(StreamWriter file, MethodInfo m, Type t, BindingFlags bf)
        {
            this.WriteTry(file);
            MethodBase[] cons = GetMethods(t, m.Name, bf);

            Dictionary<string, MethodInfo> overridedMethods = null;

            if (cons.Length == 1)
            {
                // no override function
                if (IsUsefulMethod(m) && !m.ReturnType.ContainsGenericParameters && !m.ContainsGenericParameters)
                {
                    // don't support generic method
                    this.WriteFunctionCall(m, file, t, bf);
                }
                else
                {
                    this.WriteNotMatch(file, m.Name);
                }
            }
            else
            {
                // 2 or more override function
                this.Write(file, "int argc = LuaNativeMethods.lua_gettop(ptr);");

                bool first = true;
                for (int n = 0; n < cons.Length; n++)
                {
                    if (cons[n].MemberType == MemberTypes.Method)
                    {
                        MethodInfo mi = cons[n] as MethodInfo;

                        if (mi.IsDefined(typeof(LuaOverrideAttribute), false))
                        {
                            if (overridedMethods == null)
                            {
                                overridedMethods = new Dictionary<string, MethodInfo>();
                            }

                            LuaOverrideAttribute attr = mi.GetCustomAttributes(typeof(LuaOverrideAttribute), false)[0] as LuaOverrideAttribute;
                            string fn = attr.FunctionName;
                            if (overridedMethods.ContainsKey(fn))
                            {
                                throw new Exception(string.Format("Found function with same name {0}", fn));
                            }

                            overridedMethods.Add(fn, mi);
                            continue;
                        }

                        ParameterInfo[] pars = mi.GetParameters();
                        if (IsUsefulMethod(mi)
                            && !mi.ReturnType.ContainsGenericParameters)
                        {
                            /*&& !ContainGeneric(pars)*/ // don't support generic method
                            bool isExtension = IsExtensionMethod(mi) && (bf & BindingFlags.Instance) == BindingFlags.Instance;
                            if (IsUniqueArgsCount(cons, mi))
                            {
                                this.Write(file, "{0}(argc=={1}){{", first ? "if" : "else if", mi.IsStatic ? mi.GetParameters().Length : mi.GetParameters().Length + 1);
                            }
                            else
                            {
                                this.Write(file, "{0}(MatchType(ptr,argc,{1}{2})){{", first ? "if" : "else if", mi.IsStatic && !isExtension ? 1 : 2, TypeDecl(pars, isExtension ? 1 : 0));
                            }

                            this.WriteFunctionCall(mi, file, t, bf);
                            this.Write(file, "}");
                            first = false;
                        }
                    }
                }

                this.WriteNotMatch(file, m.Name);
            }

            this.WriteCatchExecption(file);
            this.Write(file, "}");
            //this.WriteOverridedMethod(file, overridedMethods, t, bf);
        }

        public void WriteOverridedMethod(StreamWriter file, Dictionary<string, MethodInfo> methods, Type t, BindingFlags bf)
        {
            if (methods == null)
            {
                return;
            }

            foreach (KeyValuePair<string, MethodInfo> pair in methods)
            {
                string fn = pair.Value.IsStatic ? StaticName(pair.Key) : pair.Key;
                this.WriteSimpleFunction(file, fn, pair.Value, t, bf);
                funcname.Add(fn);
            }
        }

        public void WriteSimpleFunction(StreamWriter file, string fn, MethodInfo mi, Type t, BindingFlags bf)
        {
            this.WriteFunctionDec(file, fn);
            this.WriteTry(file);
            this.WriteFunctionCall(mi, file, t, bf);
            this.WriteCatchExecption(file);
            this.Write(file, "}");
        }

        public int GetMethodArgc(MethodBase mi)
        {
            bool isExtension = IsExtensionMethod(mi);
            if (isExtension)
            {
                return mi.GetParameters().Length - 1;
            }

            return mi.GetParameters().Length;
        }

        public bool IsUniqueArgsCount(MethodBase[] cons, MethodBase mi)
        {
            int argcLength = GetMethodArgc(mi);
            foreach (MethodBase member in cons)
            {
                MethodBase m = (MethodBase)member;
                if (m == mi)
                {
                    continue;
                }

                if (argcLength == GetMethodArgc(m))
                {
                    return false;
                }
            }

            return true;
        }

        public void WriteCheckSelf(StreamWriter file, Type t)
        {
            if (t.IsValueType)
            {
                this.Write(file, "{0} self;", TypeDecl(t));
                if (IsBaseType(t))
                {
                    this.Write(file, "CheckType(ptr, 1, out self);");
                }
                else
                {
                    this.Write(file, "CheckValueType(ptr, 1, out self);");
                }
            }
            else
            {
                this.Write(file, "{0} self=({0})CheckSelf(ptr);", TypeDecl(t));
            }
        }

        public void WritePushValue(Type t, StreamWriter file)
        {
            if (t.IsEnum)
            {
                this.Write(file, "PushEnum(ptr, (int)ret);");
            }
            else
            {
                this.Write(file, "PushValue(ptr, ret);");
            }
        }

        public void WritePushValue(Type t, StreamWriter file, string ret)
        {
            if (t.IsEnum)
            {
                this.Write(file, "PushEnum(ptr, (int){0});", ret);
            }
            else
            {
                this.Write(file, "PushValue(ptr, {0});", ret);
            }
        }

        public bool IsValueType(Type t)
        {
            if (t.IsByRef)
            {
                t = t.GetElementType();
            }

            return t.IsValueType || (t.BaseType == typeof(ValueType) && !IsBaseType(t));
            // return (t.IsSubclassOf(typeof(ValueType)) && !IsBaseType(t));
        }

        public bool IsBaseType(Type t)
        {
            return t.IsPrimitive || LuaObject.IsImplByLua(t);
        }

        public string FullName(string str)
        {
            if (str == null)
            {
                throw new NullReferenceException();
            }

            return RemoveRef(str.Replace("+", "."));
        }

        public string TypeDecl(Type t)
        {
            if (t.IsGenericType)
            {
                string ret = GenericBaseName(t);

                string gs = string.Empty;
                gs += "<";
                Type[] types = t.GetGenericArguments();
                for (int n = 0; n < types.Length; n++)
                {
                    gs += TypeDecl(types[n]);
                    if (n < types.Length - 1)
                    {
                        gs += ",";
                    }
                }

                gs += ">";

                ret = Regex.Replace(ret, @"`\d", gs);

                return ret;
            }

            if (t.IsArray)
            {
                return TypeDecl(t.GetElementType()) + "[]";
            }
            else
            {
                return RemoveRef(t.ToString(), false);
            }
        }

        public string ExportName(Type t)
        {
            if (t.IsGenericType)
            {
                return string.Format("Lua_{0}_{1}", _Name(GenericBaseName(t)), _Name(GenericName(t)));
            }
            else
            {
                string name = RemoveRef(t.FullName, true);
                name = "Lua_" + name;
                return name.Replace(".", "_");
            }
        }

        public string FullName(Type t)
        {
            if (t.FullName == null)
            {
                Debug.Log(t.Name);
                return t.Name;
            }

            return FullName(t.FullName);
        }

        public string FuncCall(MethodBase m, int parOffset = 0)
        {
            string str = string.Empty;
            ParameterInfo[] pars = m.GetParameters();
            for (int n = parOffset; n < pars.Length; n++)
            {
                ParameterInfo p = pars[n];
                if (p.ParameterType.IsByRef && p.IsOut)
                {
                    str += string.Format("out a{0}", n + 1);
                }
                else if (p.ParameterType.IsByRef)
                {
                    str += string.Format("ref a{0}", n + 1);
                }
                else
                {
                    str += string.Format("a{0}", n + 1);
                }

                if (n < pars.Length - 1)
                {
                    str += ",";
                }
            }

            return str;
        }

        public void Write(StreamWriter file, string fmt, params object[] args)
        {
            fmt = System.Text.RegularExpressions.Regex.Replace(fmt, @"\r\n?|\n|\r", NewLine);

            if (fmt.StartsWith("}"))
            {
                indent--;
            }

            for (int n = 0; n < indent; n++)
            {
                file.Write("\t");
            }

            if (args.Length == 0)
            {
                file.WriteLine(fmt);
            }
            else
            {
                string line = string.Format(fmt, args);
                file.WriteLine(line);
            }

            if (fmt.EndsWith("{"))
            {
                indent++;
            }
        }

        private void End(StreamWriter file)
        {
            this.Write(file, "}");
            file.Flush();
            file.Close();
        }

        private void WriteHead(Type t, StreamWriter file)
        {
            HashSet<string> nsset = new HashSet<string>();
            this.Write(file, "using System;");
            this.Write(file, "using SLua;");
            this.Write(file, "using System.Collections.Generic;");
            nsset.Add("System");
            nsset.Add("SLua");
            nsset.Add("System.Collections.Generic");
            this.WriteExtraNamespace(file, t, nsset);
#if UNITY_5_3_OR_NEWER
            this.Write(file, "[UnityEngine.Scripting.Preserve]");
#endif
            this.Write(file, "public class {0} : LuaObject {{", ExportName(t));
        }

        private void WriteFunction(Type t, StreamWriter file, bool writeStatic = false)
        {
            BindingFlags bf = BindingFlags.Public | BindingFlags.DeclaredOnly;
            if (writeStatic)
            {
                bf |= BindingFlags.Static;
            }
            else
            {
                bf |= BindingFlags.Instance;
            }

            MethodInfo[] members = t.GetMethods(bf);
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MethodInfo mi in members)
            {
                methods.Add(TryFixGenericMethod(mi));
            }

            if (!writeStatic && this.includeExtension)
            {
                if (extensionMethods.ContainsKey(t))
                {
                    methods.AddRange(extensionMethods[t]);
                }
            }

            foreach (MethodInfo mi in methods)
            {
                bool instanceFunc;
                if (writeStatic && IsPInvoke(mi, out instanceFunc))
                {
                    directfunc.Add(t.FullName + "." + mi.Name, instanceFunc);
                    continue;
                }

                string fn = writeStatic ? StaticName(mi.Name) : mi.Name;
                if (mi.MemberType == MemberTypes.Method
                    && !IsObsolete(mi)
                    && !DontExport(mi)
                    && !funcname.Contains(fn)
                    && IsUsefulMethod(mi)
                    && !MemberInFilter(t, mi))
                {
                    this.WriteFunctionDec(file, fn);
                    this.WriteFunctionImpl(file, mi, t, bf);
                    funcname.Add(fn);
                }
            }
        }

        private void WriteField(Type t, StreamWriter file)
        {
            // this.Write field set/get

            FieldInfo[] fields = t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (FieldInfo fi in fields)
            {
                if (DontExport(fi) || IsObsolete(fi))
                {
                    continue;
                }

                PropPair pp = new PropPair()
                {
                    IsInstance = !fi.IsStatic
                };
                if (fi.FieldType.BaseType != typeof(MulticastDelegate))
                {
                    this.WriteFunctionAttr(file);
                    this.Write(file, "public static int Get_{0}(IntPtr ptr) {{", fi.Name);
                    this.WriteTry(file);

                    if (fi.IsStatic)
                    {
                        this.WriteOk(file);
                        this.WritePushValue(fi.FieldType, file, string.Format("{0}.{1}", TypeDecl(t), NormalName(fi.Name)));
                    }
                    else
                    {
                        this.WriteCheckSelf(file, t);
                        this.WriteOk(file);
                        this.WritePushValue(fi.FieldType, file, string.Format("self.{0}", NormalName(fi.Name)));
                    }

                    this.Write(file, "return 2;");
                    this.WriteCatchExecption(file);
                    this.Write(file, "}");

                    pp.Get = "Get_" + fi.Name;
                }

                if (!fi.IsLiteral && !fi.IsInitOnly)
                {
                    this.WriteFunctionAttr(file);
                    this.Write(file, "public static int Set_{0}(IntPtr ptr) {{", fi.Name);
                    this.WriteTry(file);
                    if (fi.IsStatic)
                    {
                        this.Write(file, "{0} v;", TypeDecl(fi.FieldType));
                        this.WriteCheckType(file, fi.FieldType, 2);
                        this.WriteSet(file, fi.FieldType, TypeDecl(t), NormalName(fi.Name), true);
                    }
                    else
                    {
                        this.WriteCheckSelf(file, t);
                        this.Write(file, "{0} v;", TypeDecl(fi.FieldType));
                        this.WriteCheckType(file, fi.FieldType, 2);
                        this.WriteSet(file, fi.FieldType, t.FullName, NormalName(fi.Name));
                    }

                    if (t.IsValueType && !fi.IsStatic)
                    {
                        this.Write(file, "SetBack(ptr,self);");
                    }

                    this.WriteOk(file);
                    this.Write(file, "return 1;");
                    this.WriteCatchExecption(file);
                    this.Write(file, "}");

                    pp.Set = "Set_" + fi.Name;
                }

                propname.Add(fi.Name, pp);
                TryMake(fi.FieldType);
            }

            // for this[]
            List<PropertyInfo> getter = new List<PropertyInfo>();
            List<PropertyInfo> setter = new List<PropertyInfo>();

            // this.Write property set/get
            PropertyInfo[] props = t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (PropertyInfo fi in props)
            {
                // if (fi.Name == "Item" || IsObsolete(fi) || MemberInFilter(t,fi) || DontExport(fi))
                if (IsObsolete(fi) || MemberInFilter(t, fi) || DontExport(fi))
                {
                    continue;
                }

                if (fi.Name == "Item" || (t.Name == "String" && fi.Name == "Chars"))
                {
                    // for string[]
                    if (!fi.GetGetMethod().IsStatic && fi.GetIndexParameters().Length == 1)
                    {
                        if (fi.CanRead && !IsNotSupport(fi.PropertyType))
                        {
                            getter.Add(fi);
                        }

                        if (fi.CanWrite && fi.GetSetMethod() != null)
                        {
                            setter.Add(fi);
                        }
                    }

                    continue;
                }

                PropPair pp = new PropPair();
                bool isInstance = true;

                if (fi.CanRead && fi.GetGetMethod() != null)
                {
                    if (!IsNotSupport(fi.PropertyType))
                    {
                        this.WriteFunctionAttr(file);
                        this.Write(file, "public static int Get_{0}(IntPtr ptr) {{", fi.Name);
                        this.WriteTry(file);

                        if (fi.GetGetMethod().IsStatic)
                        {
                            isInstance = false;
                            this.WriteOk(file);
                            this.WritePushValue(fi.PropertyType, file, string.Format("{0}.{1}", TypeDecl(t), NormalName(fi.Name)));
                        }
                        else
                        {
                            this.WriteCheckSelf(file, t);
                            this.WriteOk(file);
                            this.WritePushValue(fi.PropertyType, file, string.Format("self.{0}", NormalName(fi.Name)));
                        }

                        this.Write(file, "return 2;");
                        this.WriteCatchExecption(file);
                        this.Write(file, "}");
                        pp.Get = "Get_" + fi.Name;
                    }
                }

                if (fi.CanWrite && fi.GetSetMethod() != null)
                {
                    this.WriteFunctionAttr(file);
                    this.Write(file, "public static int Set_{0}(IntPtr ptr) {{", fi.Name);
                    this.WriteTry(file);

                    if (fi.GetSetMethod().IsStatic)
                    {
                        this.WriteValueCheck(file, fi.PropertyType, 2);
                        this.WriteSet(file, fi.PropertyType, TypeDecl(t), NormalName(fi.Name), true, fi.CanRead);
                        isInstance = false;
                    }
                    else
                    {
                        this.WriteCheckSelf(file, t);
                        this.WriteValueCheck(file, fi.PropertyType, 2);
                        this.WriteSet(file, fi.PropertyType, TypeDecl(t), NormalName(fi.Name), false, fi.CanRead);
                    }

                    if (t.IsValueType)
                    {
                        this.Write(file, "SetBack(ptr,self);");
                    }

                    this.WriteOk(file);
                    this.Write(file, "return 1;");
                    this.WriteCatchExecption(file);
                    this.Write(file, "}");
                    pp.Set = "Set_" + fi.Name;
                }

                pp.IsInstance = isInstance;

                propname.Add(fi.Name, pp);
                TryMake(fi.PropertyType);
            }

            // for this[]
            this.WriteItemFunc(t, file, getter, setter);
        }

        private void WriteFunctionAttr(StreamWriter file)
        {
            this.Write(file, "[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]");
#if UNITY_5_3_OR_NEWER
            this.Write(file, "[UnityEngine.Scripting.Preserve]");
#endif
        }

        private ConstructorInfo[] GetValidConstructor(Type t)
        {
            List<ConstructorInfo> ret = new List<ConstructorInfo>();
            if (t.GetConstructor(Type.EmptyTypes) == null && t.IsAbstract && t.IsSealed)
            {
                return ret.ToArray();
            }

            if (t.IsAbstract)
            {
                return ret.ToArray();
            }

            if (t.BaseType != null && t.BaseType.Name == "MonoBehaviour")
            {
                return ret.ToArray();
            }

            ConstructorInfo[] cons = t.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            foreach (ConstructorInfo ci in cons)
            {
                if (!IsObsolete(ci) && !DontExport(ci) && !ContainUnsafe(ci))
                {
                    ret.Add(ci);
                }
            }

            return ret.ToArray();
        }

        private bool ContainUnsafe(MethodBase mi)
        {
            foreach (ParameterInfo p in mi.GetParameters())
            {
                if (p.ParameterType.FullName.Contains("*"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool DontExport(MemberInfo mi)
        {
            string methodString = string.Format("{0}.{1}", mi.DeclaringType, mi.Name);
            if (CustomExport.FunctionFilterList.Contains(methodString))
            {
                return true;
            }

            // directly ignore any components .ctor
            if (mi.DeclaringType.IsSubclassOf(typeof(UnityEngine.Component)))
            {
                if (mi.MemberType == MemberTypes.Constructor)
                {
                    return true;
                }
            }

            // Check in custom export function filter list.
            List<object> funcFilterList = LuaCodeGen.GetEditorField<ICustomExportPost>("FunctionFilterList");
            foreach (object filterList in funcFilterList)
            {
                if (((List<string>)filterList).Contains(methodString))
                {
                    return true;
                }
            }

            return mi.IsDefined(typeof(DoNotToLuaAttribute), false);
        }

        private void WriteConstructor(Type t, StreamWriter file)
        {
            ConstructorInfo[] cons = GetValidConstructor(t);
            if (cons.Length > 0)
            {
                this.WriteFunctionAttr(file);
                this.Write(file, "public static int Constructor(IntPtr ptr) {");
                this.WriteTry(file);
                if (cons.Length > 1)
                {
                    this.Write(file, "int argc = LuaNativeMethods.lua_gettop(ptr);");
                }

                this.Write(file, "{0} o;", TypeDecl(t));
                bool first = true;
                for (int n = 0; n < cons.Length; n++)
                {
                    ConstructorInfo ci = cons[n];
                    ParameterInfo[] pars = ci.GetParameters();

                    if (cons.Length > 1)
                    {
                        if (IsUniqueArgsCount(cons, ci))
                        {
                            this.Write(file, "{0}(argc=={1}){{", first ? "if" : "else if", ci.GetParameters().Length + 1);
                        }
                        else
                        {
                            this.Write(file, "{0}(MatchType(ptr,argc,2{1})){{", first ? "if" : "else if", TypeDecl(pars));
                        }
                    }

                    for (int k = 0; k < pars.Length; k++)
                    {
                        ParameterInfo p = pars[k];
                        bool hasParams = p.IsDefined(typeof(ParamArrayAttribute), false);
                        CheckArgument(file, p.ParameterType, k, 2, p.IsOut, hasParams);
                    }

                    this.Write(file, "o=new {0}({1});", TypeDecl(t), FuncCall(ci));
                    this.WriteOk(file);

                    if (t.Name == "String")
                    {
                        // if export string, push string as ud not lua string
                        this.Write(file, "PushObject(ptr, o);");
                    }
                    else
                    {
                        this.Write(file, "PushValue(ptr, o);");
                    }

                    this.Write(file, "return 2;");
                    if (cons.Length == 1)
                    {
                        this.WriteCatchExecption(file);
                    }

                    this.Write(file, "}");
                    first = false;
                }

                if (cons.Length > 1)
                {
                    if (t.IsValueType)
                    {
                        this.Write(file, "{0}(argc=={1}){{", first ? "if" : "else if", 0);
                        this.Write(file, "o=new {0}();", FullName(t));
                        this.Write(file, "PushValue(ptr, true);");
                        this.Write(file, "PushObject(ptr, o);");
                        this.Write(file, "return 2;");
                        this.Write(file, "}");
                    }

                    this.Write(file, "return Error(ptr, \"New object failed.\");");
                    this.WriteCatchExecption(file);
                    this.Write(file, "}");
                }
            }
            else if (t.IsValueType)
            {
                // default constructor
                this.WriteFunctionAttr(file);
                this.Write(file, "public static int Constructor(IntPtr ptr) {");
                this.WriteTry(file);
                this.Write(file, "{0} o;", FullName(t));
                this.Write(file, "o=new {0}();", FullName(t));
                this.WriteReturn(file, "o");
                this.WriteCatchExecption(file);
                this.Write(file, "}");
            }
        }

        private void WriteFunctionCall(MethodInfo m, StreamWriter file, Type t, BindingFlags bf)
        {
            bool isExtension = IsExtensionMethod(m) && (bf & BindingFlags.Instance) == BindingFlags.Instance;
            bool hasref = false;
            ParameterInfo[] pars = m.GetParameters();

            int argIndex = 1;
            int parOffset = 0;
            if (!m.IsStatic)
            {
                this.WriteCheckSelf(file, t);
                argIndex++;
            }
            else if (isExtension)
            {
                this.WriteCheckSelf(file, t);
                parOffset++;
            }

            for (int n = parOffset; n < pars.Length; n++)
            {
                ParameterInfo p = pars[n];
                string pn = p.ParameterType.Name;
                if (pn.EndsWith("&"))
                {
                    hasref = true;
                }

                bool hasParams = p.IsDefined(typeof(ParamArrayAttribute), false);
                CheckArgument(file, p.ParameterType, n, argIndex, p.IsOut, hasParams);
            }

            string ret = string.Empty;
            if (m.ReturnType != typeof(void))
            {
                ret = "var ret=";
            }

            if (m.IsStatic && !isExtension)
            {
                if (m.Name == "op_Multiply")
                {
                    this.Write(file, "{0}a1*a2;", ret);
                }
                else if (m.Name == "op_Subtraction")
                {
                    this.Write(file, "{0}a1-a2;", ret);
                }
                else if (m.Name == "op_Addition")
                {
                    this.Write(file, "{0}a1+a2;", ret);
                }
                else if (m.Name == "op_Division")
                {
                    this.Write(file, "{0}a1/a2;", ret);
                }
                else if (m.Name == "op_UnaryNegation")
                {
                    this.Write(file, "{0}-a1;", ret);
                }
                else if (m.Name == "op_UnaryPlus")
                {
                    this.Write(file, "{0}+a1;", ret);
                }
                else if (m.Name == "op_Equality")
                {
                    this.Write(file, "{0}(a1==a2);", ret);
                }
                else if (m.Name == "op_Inequality")
                {
                    this.Write(file, "{0}(a1!=a2);", ret);
                }
                else if (m.Name == "op_LessThan")
                {
                    this.Write(file, "{0}(a1<a2);", ret);
                }
                else if (m.Name == "op_GreaterThan")
                {
                    this.Write(file, "{0}(a2<a1);", ret);
                }
                else if (m.Name == "op_LessThanOrEqual")
                {
                    this.Write(file, "{0}(a1<=a2);", ret);
                }
                else if (m.Name == "op_GreaterThanOrEqual")
                {
                    this.Write(file, "{0}(a2<=a1);", ret);
                }
                else
                {
                    this.Write(file, "{3}{2}.{0}({1});", MethodDecl(m), FuncCall(m), TypeDecl(t), ret);
                }
            }
            else
            {
                this.Write(file, "{2}self.{0}({1});", MethodDecl(m), FuncCall(m, parOffset), ret);
            }

            this.WriteOk(file);
            int retcount = 1;
            if (m.ReturnType != typeof(void))
            {
                this.WritePushValue(m.ReturnType, file);
                retcount = 2;
            }

            // push out/ref value for return value
            if (hasref)
            {
                for (int n = 0; n < pars.Length; n++)
                {
                    ParameterInfo p = pars[n];

                    if (p.ParameterType.IsByRef)
                    {
                        this.WritePushValue(p.ParameterType, file, string.Format("a{0}", n + 1));
                        retcount++;
                    }
                }
            }

            if (t.IsValueType && m.ReturnType == typeof(void) && !m.IsStatic)
            {
                this.Write(file, "SetBack(ptr,self);");
            }

            this.Write(file, "return {0};", retcount);
        }

        private string SimpleType(Type t)
        {
            string tn = t.Name;
            switch (tn)
            {
                case "Single":
                    return "float";
                case "String":
                    return "string";
                case "Double":
                    return "double";
                case "Boolean":
                    return "bool";
                case "Int32":
                    return "int";
                case "Object":
                    return FullName(t);
                default:
                    tn = TypeDecl(t);
                    tn = tn.Replace("System.Collections.Generic.", string.Empty);
                    tn = tn.Replace("System.Object", "object");
                    return tn;
            }
        }

        private void CheckArgument(StreamWriter file, Type t, int n, int argstart, bool isout, bool isparams)
        {
            this.Write(file, "{0} a{1};", TypeDecl(t), n + 1);

            if (!isout)
            {
                if (t.IsEnum)
                {
                    this.Write(file, "CheckEnum(ptr, {0}, out a{1});", n + argstart, n + 1);
                }
                else if (t.BaseType == typeof(System.MulticastDelegate))
                {
                    TryMake(t);
                    this.Write(file, "LuaDelegation.CheckDelegate(ptr, {0}, out a{1});", n + argstart, n + 1);
                }
                else if (isparams)
                {
                    if (t.GetElementType().IsValueType && !IsBaseType(t.GetElementType()))
                    {
                        this.Write(file, "CheckValueParams(ptr, {0}, out a{1});", n + argstart, n + 1);
                    }
                    else
                    {
                        this.Write(file, "CheckParams(ptr, {0}, out a{1});", n + argstart, n + 1);
                    }
                }
                else if (t.IsArray)
                {
                    this.Write(file, "CheckArray(ptr, {0}, out a{1});", n + argstart, n + 1);
                }
                else if (IsValueType(t))
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        this.Write(file, "CheckNullable(ptr, {0}, out a{1});", n + argstart, n + 1);
                    }
                    else
                    {
                        this.Write(file, "CheckValueType(ptr, {0}, out a{1});", n + argstart, n + 1);
                    }
                }
                else
                {
                    this.Write(file, "CheckType(ptr, {0}, out a{1});", n + argstart, n + 1);
                }
            }
        }
    }
}
