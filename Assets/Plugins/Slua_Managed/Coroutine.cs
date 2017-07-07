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
using UnityEngine;

namespace SLua
{
    public class LuaCoroutine : LuaObject
    {
        public static MonoBehaviour Behaviour { get; private set; }

        public static void Register(IntPtr ptr, MonoBehaviour behaviour)
        {
            LuaCoroutine.Behaviour = behaviour;
            Register(ptr, YieldValue, "UnityEngine");

            string yield =
@"
local Yield = UnityEngine.Yieldk

uCoroutine = uCoroutine or {}

uCoroutine.create = function(x)

    local co = coroutine.create(x)
    coroutine.resume(co)
    return co

end

uCoroutine.yield = function(x)

    local co, ismain = coroutine.running()
    if ismain then LuaObject.Error('Can not yield in main thread') end

    if type(x) == 'thread' and coroutine.status(x) ~= 'dead' then
        repeat
            Yield(nil, function() coroutine.resume(co) end)
            coroutine.yield()
        until coroutine.status(x) == 'dead'
    else
        Yield(x, function() coroutine.resume(co) end)
        coroutine.yield()
    end

end

-- backward compatibility of older versions
UnityEngine.Yield = uCoroutine.yield
";
            LuaState.Get(ptr).DoString(yield);
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int YieldValue(IntPtr ptr)
        {
            try
            {
                if (LuaNativeMethods.lua_pushthread(ptr) == 1)
                {
                    return LuaObject.Error(ptr, "should put Yield call into lua coroutine.");
                }

                object y = CheckObj(ptr, 1);
                LuaFunction f;
                LuaObject.CheckType(ptr, 2, out f);
                Behaviour.StartCoroutine(YieldReturn(y, f));
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        public static IEnumerator YieldReturn(object yieldObject, LuaFunction function)
        {
            yield return yieldObject is IEnumerator ? Behaviour.StartCoroutine((IEnumerator)yieldObject) : yieldObject;
            function.Call();
        }
    }
}
