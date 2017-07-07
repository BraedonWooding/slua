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
using UnityEngine;

namespace SLua
{
    public partial class LuaObject
    {
        public static bool CheckType(IntPtr ptr, int p, out Vector4 v)
        {
            float x, y, z, w;
            if (LuaNativeMethods.luaS_checkVector4(ptr, p, out x, out y, out z, out w) != 0)
            {
                throw new Exception(string.Format("Invalid vector4 argument at {0}", p));
            }

            v = new Vector4(x, y, z, w);
            return true;
        }

        public static bool CheckType(IntPtr ptr, int p, out Vector3 v)
        {
            float x, y, z;
            if (LuaNativeMethods.luaS_checkVector3(ptr, p, out x, out y, out z) != 0)
            {
                throw new Exception(string.Format("Invalid vector3 argument at {0}", p));
            }

            v = new Vector3(x, y, z);
            return true;
        }

        public static bool CheckType(IntPtr ptr, int p, out Vector2 v)
        {
            float x, y;
            if (LuaNativeMethods.luaS_checkVector2(ptr, p, out x, out y) != 0)
            {
                throw new Exception(string.Format("Invalid vector2 argument at {0}", p));
            }

            v = new Vector2(x, y);
            return true;
        }

        public static bool CheckType(IntPtr ptr, int p, out Quaternion q)
        {
            float x, y, z, w;
            if (LuaNativeMethods.luaS_checkQuaternion(ptr, p, out x, out y, out z, out w) != 0)
            {
                throw new Exception(string.Format("Invalid quaternion argument at {0}", p));
            }

            q = new Quaternion(x, y, z, w);
            return true;
        }

        public static bool CheckType(IntPtr ptr, int p, out Color c)
        {
            float x, y, z, w;
            if (LuaNativeMethods.lua_type(ptr, p) == LuaTypes.LUA_TUSERDATA)
            {
                object o = CheckObj(ptr, p);
                if (o is Color32)
                {
                    c = (Color32)o;
                    return true;
                }

                throw new Exception(string.Format("Invalid color argument at {0}", p));
            }

            if (LuaNativeMethods.luaS_checkColor(ptr, p, out x, out y, out z, out w) != 0)
            {
                throw new Exception(string.Format("Invalid color argument at {0}", p));
            }

            c = new Color(x, y, z, w);
            return true;
        }

        public static bool CheckType(IntPtr ptr, int p, out LayerMask lm)
        {
            int v;
            CheckType(ptr, p, out v);
            lm = v;
            return true;
        }

        public static bool CheckParams(IntPtr ptr, int p, out Vector2[] pars)
        {
            int top = LuaNativeMethods.lua_gettop(ptr);
            if (top - p >= 0)
            {
                pars = new Vector2[top - p + 1];
                for (int n = p, k = 0; n <= top; n++, k++)
                {
                    CheckType(ptr, n, out pars[k]);
                }

                return true;
            }

            pars = new Vector2[0];
            return true;
        }

        public static void PushValue(IntPtr ptr, RaycastHit2D r)
        {
            PushObject(ptr, r);
        }

        public static void PushValue(IntPtr ptr, RaycastHit r)
        {
            PushObject(ptr, r);
        }

        public static void PushValue(IntPtr ptr, UnityEngine.AnimationState o)
        {
            if (o == null)
            {
                LuaNativeMethods.lua_pushnil(ptr);
            }
            else
            {
                PushObject(ptr, o);
            }
        }

        public static void PushValue(IntPtr ptr, UnityEngine.Object o)
        {
            if (o == null)
            {
                LuaNativeMethods.lua_pushnil(ptr);
            }
            else
            {
                PushObject(ptr, o);
            }
        }

        public static void PushValue(IntPtr ptr, Quaternion o)
        {
            LuaNativeMethods.luaS_pushQuaternion(ptr, o.x, o.y, o.z, o.w);
        }

        public static void PushValue(IntPtr ptr, Vector2 o)
        {
            LuaNativeMethods.luaS_pushVector2(ptr, o.x, o.y);
        }

        public static void PushValue(IntPtr ptr, Vector3 o)
        {
            LuaNativeMethods.luaS_pushVector3(ptr, o.x, o.y, o.z);
        }

        public static void PushValue(IntPtr ptr, Vector4 o)
        {
            LuaNativeMethods.luaS_pushVector4(ptr, o.x, o.y, o.z, o.w);
        }

        public static void PushValue(IntPtr ptr, Color o)
        {
            LuaNativeMethods.luaS_pushColor(ptr, o.r, o.g, o.b, o.a);
        }

        public static void PushValue(IntPtr ptr, Color32 c32)
        {
            PushObject(ptr, c32);
        }
    }
}
