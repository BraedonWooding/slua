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

using System.Collections;
using UnityEngine;

namespace SLua
{
    public static class UnityExtension
    {
        public static void StartCoroutine(this MonoBehaviour mb, LuaFunction func)
        {
            mb.StartCoroutine(LuaCoroutine(func));
        }

        public static IEnumerator LuaCoroutine(LuaFunction func)
        {
            LuaThreadWrapper thread = new LuaThreadWrapper(func);
            while (true)
            {
                object obj;
                if (!thread.Resume(out obj))
                {
                    yield break;
                }

                yield return obj;
            }
        }
    }
}