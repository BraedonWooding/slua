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

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SLua
{
    public class LuaConsole : EditorWindow
    {
        #region COMMON_DEFINE
        public const string CommonDefine = @"
local function prettyTabToStr(tab, level, path, visited)
    local result = ''
    if level == nil then
        visited = {}
        level = 0
        path = '(self)'
    end

    if visited[tab] then
        return string.format( '%s%s\n', string.rep('\t', level), visited[tab] )
    end
    visited[tab] = path

    result = result .. string.format('%s{\n', string.rep('\t', level))
    local ignore = {}
    for i,v in ipairs(tab)do
        ignore[i] = true
        if type(v) == 'table' then
            local newPath = path .. '.' .. tostring(k)
            if visited[v] then
                local existPath = visited[v]
                local _,count1 = string.gsub(existPath, '%.', function()end)
                local _,count2 = string.gsub(newPath, '%.', function()end)
                if count2 < count1 then
                    visited[v] = newPath
                end
                result = result .. string.format('%s%s\n', string.rep('\t', level+1), visited[v])
            else
                result = result .. string.format('%s\n', string.rep('\t', level+1))
                result = result .. prettyTabToStr(v, level+1, newPath, visited)
            end
        else
            result = result .. string.format('%s%s,\n', string.rep('\t', level+1), tostring(v))
        end
    end
    for k,v in pairs(tab)do
        if not ignore[k] then
            local typeOfKey = type(k)
            local kStr = k
            if typeOfKey == 'string' then
                if not k:match('^[_%a][_%w]*$') then
                    kStr = '[' .. k .. '] = '
                else
                    kStr = tostring(k) .. ' = '
                end
            else
                kStr = string.format('[%s] = ', tostring(k))
            end

            if type(v) == 'table' then
                local newPath = path .. '.' .. tostring(k)
                if visited[v] then
                    local existPath = visited[v]
                    local _,count1 = string.gsub(existPath, '%.', function()end)
                    local _,count2 = string.gsub(newPath, '%.', function()end)
                    if count2 < count1 then
                        visited[v] = newPath
                    end
                    result = result .. string.format('%s%s%s\n', string.rep('\t', level+1), tostring(kStr), visited[v])
                else
                    result = result .. string.format('%s%s\n', string.rep('\t', level+1), tostring(kStr))
                    result = result .. prettyTabToStr(v, level+1, newPath, visited)
                end
            else
                result = result .. string.format('%s%s%s,\n', string.rep('\t', level+1), tostring(kStr), tostring(v))
            end
        end
    end
    result = result .. string.format('%s}\n', string.rep('\t', level))
    return result
end
local setfenv = setfenv or function(f,env) debug.setupvalue(f,1,env) end
local env = setmetatable({}, {__index=_G, __newindex=function(t,k,v)
    print('set global', k, '=', v)
    _G[k] = v
end})
local function printVar(val)
    if type(val) == 'table' then
        print(prettyTabToStr(val))
    else
        print(val)
    end
end
local function eval(code)
    local func,err = loadstring('return ' .. code)
    if not func then
        LuaObject.Error(err)
    end
    setfenv(func, env)
    return func()
end
local function compile(code)
    local func,err = loadstring('do ' .. code .. ' end')
    if not func then
        LuaObject.Error(err)
    end
    setfenv(func, env)
    func()
end
local function printExpr(str)
    if str:match('^[_%a][_%w]*$') then
        printVar(env[str])
    else
        local result = {eval(str)}
        if #result > 1 then
            printVar(result)
        else
            printVar(result[1])
        end
    end
end
local function dir(val)
    if type(val) == 'table' then
        local t = {}
        for k,v in pairs(val)do
            table.insert(t, string.format('%s=%s', tostring(k), tostring(v)))
        end
        print(table.concat(t, '\n'))
    else
        print(val)
    end
end
local function dirExpr(str)
    if str:match('^[_%a][_%w]*$') then
        dir(env[str])
    else
        local result = {eval(str)}
        if #result > 1 then
            dir(result)
        else
            dir(result[1])
        end
    end
end
";
        #endregion

        private string inputText = string.Empty;
        private string filterText = string.Empty;

        private string outputText = "LuaConsole:\n";
        private StringBuilder outputBuffer = new StringBuilder();
        private List<OutputRecord> recordList = new List<OutputRecord>();

        private List<string> history = new List<string>();
        private int historyIndex = 0;

        private Vector2 scrollPosition = Vector2.zero;
        private GUIStyle textAreaStyle = new GUIStyle();
        private bool initedStyle = false;
        private bool toggleLog = true;
        private bool toggleErr = true;

        private float inputAreaPosY = 0f;
        private float inputAreaHeight = 50f;
        private bool inputAreaResizing;

        [MenuItem("SLua/LuaConsole")]
        public static void Open()
        {
            EditorWindow.GetWindow<LuaConsole>("LuaConsole");
        }

        [MenuItem("CONTEXT/Component/Push Component To Lua")]
        public static void PushComponentObjectToLua(MenuCommand cmd)
        {
            Component com = cmd.context as Component;
            if (com == null)
            {
                return;
            }

            LuaState luaState = LuaState.Main;
            if (luaState == null)
            {
                return;
            }

            LuaObject.PushObject(luaState.StatePointer, com);
            LuaNativeMethods.lua_setglobal(luaState.StatePointer, "_");
        }

        [MenuItem("CONTEXT/Component/Push GameObject To Lua")]
        public static void PushGameObjectToLua(MenuCommand cmd)
        {
            Component com = cmd.context as Component;
            if (com == null)
            {
                return;
            }

            LuaState luaState = LuaState.Main;
            if (luaState == null)
            {
                return;
            }

            SLua.LuaObject.PushObject(luaState.StatePointer, com.gameObject);
            LuaNativeMethods.lua_setglobal(luaState.StatePointer, "_");
        }

        public void AddLog(string str)
        {
            recordList.Add(new OutputRecord(str, OutputRecord.OutputType.Log));
            ConsoleFlush();
        }

        public void AddError(string str)
        {
            recordList.Add(new OutputRecord(str, OutputRecord.OutputType.Err));
            ConsoleFlush();
        }

        public void ConsoleFlush()
        {
            outputBuffer.Length = 0;

            string keyword = filterText.Trim();
            for (int i = 0; i < recordList.Count; ++i)
            {
                OutputRecord record = recordList[i];
                if (record.Type == OutputRecord.OutputType.Log && !toggleLog)
                {
                    continue;
                }
                else if (record.Type == OutputRecord.OutputType.Err && !toggleErr)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(keyword))
                {
                    if (record.Text.IndexOf(keyword) >= 0)
                    {
                        string highlightText = string.Format("<color=#ffff00ff>{0}</color>", keyword);
                        string displayText = record.Text.Replace(keyword, highlightText);
                        outputBuffer.AppendLine(displayText);
                    }
                }
                else
                {
                    outputBuffer.AppendLine(record.Text);
                }
            }

            outputText = outputBuffer.ToString();
            scrollPosition.y = float.MaxValue;
            Repaint();
        }

        public void OnEnable()
        {
            LuaState.LogEvent += AddLog;
            LuaState.ErrorEvent += AddError;
        }

        public void OnDisable()
        {
            LuaState.LogEvent -= AddLog;
            LuaState.ErrorEvent -= AddError;
        }

        public void OnDestroy()
        {
            LuaState.LogEvent -= AddLog;
            LuaState.ErrorEvent -= AddError;
        }

        public void OnGUI()
        {
            if (!initedStyle)
            {
                GUIStyle entryInfoTyle = "CN EntryInfo";
                textAreaStyle.richText = true;
                textAreaStyle.normal.textColor = entryInfoTyle.normal.textColor;
                initedStyle = true;
            }

            // Output Text Area
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(Screen.width), GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(outputText, textAreaStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            // Filter Option Toggles
            GUILayout.BeginHorizontal();
            bool oldToggleLog = toggleLog;
            bool oldToggleErr = toggleErr;
            toggleLog = GUILayout.Toggle(oldToggleLog, "log", GUILayout.ExpandWidth(false));
            toggleErr = GUILayout.Toggle(oldToggleErr, "error", GUILayout.ExpandWidth(false));

            // Filter Input Field
            GUILayout.Space(10f);
            GUILayout.Label("filter:", GUILayout.ExpandWidth(false));
            string oldFilterPattern = filterText;
            filterText = GUILayout.TextField(oldFilterPattern, GUILayout.Width(200f));

            // Menu Buttons
            if (GUILayout.Button("clear", GUILayout.ExpandWidth(false)))
            {
                recordList.Clear();
                ConsoleFlush();
            }

            GUILayout.EndHorizontal();

            if (toggleLog != oldToggleLog || toggleErr != oldToggleErr || filterText != oldFilterPattern)
            {
                ConsoleFlush();
            }

            if (Event.current.type == EventType.Repaint)
            {
                inputAreaPosY = GUILayoutUtility.GetLastRect().yMax;
            }

            // Drag Spliter
            ResizeScrollView();

            // Input Area
            GUI.SetNextControlName("Input");
            inputText = EditorGUILayout.TextField(inputText, GUILayout.Height(inputAreaHeight));

            if (Event.current.isKey && Event.current.type == EventType.KeyUp)
            {
                bool refresh = false;
                if (Event.current.keyCode == KeyCode.Return)
                {
                    if (inputText != string.Empty)
                    {
                        if (history.Count == 0 || history[history.Count - 1] != inputText)
                        {
                            history.Add(inputText);
                        }

                        AddLog(inputText);
                        DoCommand(inputText);
                        inputText = string.Empty;
                        refresh = true;
                        historyIndex = history.Count;
                    }
                }
                else if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    if (history.Count > 0)
                    {
                        historyIndex = historyIndex - 1;
                        if (historyIndex < 0)
                        {
                            historyIndex = 0;
                        }
                        else
                        {
                            inputText = history[historyIndex];
                            refresh = true;
                        }
                    }
                }
                else if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    if (history.Count > 0)
                    {
                        historyIndex = historyIndex + 1;
                        if (historyIndex > history.Count - 1)
                        {
                            historyIndex = history.Count - 1;
                        }
                        else
                        {
                            inputText = history[historyIndex];
                            refresh = true;
                        }
                    }
                }

                if (refresh)
                {
                    Repaint();
                    EditorGUIUtility.editingTextField = false;
                    GUI.FocusControl("Input");
                }
            }
        }

        public void ResizeScrollView()
        {
            Rect dragSpliterRect = new Rect(0f, inputAreaPosY + 2, Screen.width, 2);
            EditorGUI.DrawRect(dragSpliterRect, Color.black);
            EditorGUIUtility.AddCursorRect(dragSpliterRect, MouseCursor.ResizeVertical);
            GUILayout.Space(4);

            Event e = Event.current;
            if (e.type == EventType.mouseDown && dragSpliterRect.Contains(e.mousePosition))
            {
                e.Use();
                inputAreaResizing = true;
            }

            if (e.type == EventType.MouseDrag)
            {
                if (inputAreaResizing)
                {
                    e.Use();
                    inputAreaHeight -= Event.current.delta.y;
                    inputAreaHeight = Mathf.Max(inputAreaHeight, 20f);
                }
            }

            if (e.type == EventType.MouseUp)
            {
                inputAreaResizing = false;
            }
        }

        public void DoCommand(string str)
        {
            LuaState luaState = LuaState.Main;
            if (luaState == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            int index = str.IndexOf(" ");
            string cmd = str;
            string tail = string.Empty;
            if (index > 0)
            {
                cmd = str.Substring(0, index).Trim().ToLower();
                tail = str.Substring(index + 1);
            }

            if (cmd == "p")
            {
                if (tail == string.Empty)
                {
                    return;
                }

                LuaFunction f = luaState.DoString(CommonDefine + "return printExpr", "LuaConsole") as LuaFunction;
                f.Call(tail);
                f.Dispose();
            }
            else if (cmd == "dir")
            {
                if (tail == string.Empty)
                {
                    return;
                }

                LuaFunction f = luaState.DoString(CommonDefine + "return dirExpr", "LuaConsole") as LuaFunction;
                f.Call(tail);
                f.Dispose();
            }
            else
            {
                LuaFunction f = luaState.DoString(CommonDefine + "return compile", "LuaConsole") as LuaFunction;
                f.Call(str);
                f.Dispose();
            }
        }

        public struct OutputRecord
        {
            public OutputRecord(string text, OutputType type)
            {
                this.Type = type;
                if (type == OutputType.Err)
                {
                    this.Text = "<color=#a52a2aff>" + text + "</color>";
                }
                else
                {
                    this.Text = text;
                }
            }

            public enum OutputType
            {
                Log = 0,
                Err = 1,
            }

            public string Text { get; private set; }

            public OutputType Type { get; private set; }
        }
    }
}