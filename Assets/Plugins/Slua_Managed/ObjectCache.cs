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
using System.Runtime.CompilerServices;

namespace SLua
{
    public class ObjectCache
    {
        private static Dictionary<Type, string> nameMap = new Dictionary<Type, string>();
        private static Dictionary<IntPtr, ObjectCache> multiState = new Dictionary<IntPtr, ObjectCache>();
        private static IntPtr oldPtr = IntPtr.Zero;

        private FreeList cache = new FreeList();

        private Dictionary<object, int> objMap = new Dictionary<object, int>(new ObjEqualityComparer());

        private int cacheRef = 0;

        public ObjectCache(IntPtr ptr)
        {
            LuaNativeMethods.lua_newtable(ptr);
            LuaNativeMethods.lua_newtable(ptr);
            LuaNativeMethods.lua_pushstring(ptr, "v");
            LuaNativeMethods.lua_setfield(ptr, -2, "__mode");
            LuaNativeMethods.lua_setmetatable(ptr, -2);
            cacheRef = LuaNativeMethods.luaL_ref(ptr, LuaIndexes.LUARegistryIndex);
        }

        public static ObjectCache Oldoc { get; private set; }

        public Dictionary<object, int>.KeyCollection Objs
        {
            get
            {
                return objMap.Keys;
            }
        }

        public static ObjectCache Get(IntPtr ptr)
        {
            if (oldPtr == ptr)
            {
                return Oldoc;
            }

            ObjectCache oc;
            if (multiState.TryGetValue(ptr, out oc))
            {
                oldPtr = ptr;
                Oldoc = oc;
                return oc;
            }

            LuaNativeMethods.lua_getglobal(ptr, "__main_state");
            if (LuaNativeMethods.lua_isnil(ptr, -1))
            {
                LuaNativeMethods.lua_pop(ptr, 1);
                return null;
            }

            IntPtr nl = LuaNativeMethods.lua_touserdata(ptr, -1);
            LuaNativeMethods.lua_pop(ptr, 1);

            if (nl != ptr)
            {
                return Get(nl);
            }

            return null;
        }

        public static void Clear()
        {
            oldPtr = IntPtr.Zero;
            Oldoc = null;
        }

        public static string GetAQName(object o)
        {
            Type t = o.GetType();
            return GetAQName(t);
        }

        public static string GetAQName(Type t)
        {
            string name;
            if (nameMap.TryGetValue(t, out name))
            {
                return name;
            }

            name = t.AssemblyQualifiedName;
            nameMap[t] = name;
            return name;
        }

        public static void Delete(IntPtr ptr)
        {
            multiState.Remove(ptr);
        }

        public static void Make(IntPtr ptr)
        {
            ObjectCache oc = new ObjectCache(ptr);
            multiState[ptr] = oc;
            oldPtr = ptr;
            Oldoc = oc;
        }

        public int Size()
        {
            return objMap.Count;
        }

        public void GC(int index)
        {
            object o;
            if (cache.Get(index, out o))
            {
                int oldindex;
                if (IsGcObject(o) && objMap.TryGetValue(o, out oldindex) && oldindex == index)
                {
                    objMap.Remove(o);
                }

                cache.Delete(index);
            }
        }

        public void GC(UnityEngine.Object o)
        {
            int index;
            if (objMap.TryGetValue(o, out index))
            {
                objMap.Remove(o);
                cache.Delete(index);
            }
        }

        public int Add(object o)
        {
            int objIndex = cache.Add(o);
            if (IsGcObject(o))
            {
                objMap[o] = objIndex;
            }

            return objIndex;
        }

        public object Get(IntPtr ptr, int p)
        {
            int index = LuaNativeMethods.luaS_rawnetobj(ptr, p);
            object o;
            if (index != -1 && cache.Get(index, out o))
            {
                return o;
            }

            return null;
        }

        public void SetBack(IntPtr ptr, int p, object o)
        {
            int index = LuaNativeMethods.luaS_rawnetobj(ptr, p);
            if (index != -1)
            {
                cache.Set(index, o);
            }
        }

        public void Push(IntPtr ptr, object o)
        {
            Push(ptr, o, true);
        }

        public void Push(IntPtr ptr, Array o)
        {
            int index = AllocID(ptr, o);
            if (index < 0)
            {
                return;
            }

            LuaNativeMethods.luaS_pushobject(ptr, index, "LuaArray", true, cacheRef);
        }

        public int AllocID(IntPtr ptr, object o)
        {
            int index = -1;

            if (o == null)
            {
                LuaNativeMethods.lua_pushnil(ptr);
                return index;
            }

            bool gco = IsGcObject(o);
            bool found = gco && objMap.TryGetValue(o, out index);

            if (found)
            {
                if (LuaNativeMethods.luaS_getcacheud(ptr, index, cacheRef) == 1)
                {
                    return -1;
                }
            }

            index = Add(o);
            return index;
        }

        public void Push(IntPtr ptr, object o, bool checkReflect)
        {
            int index = AllocID(ptr, o);
            if (index < 0)
            {
                return;
            }

            bool gco = IsGcObject(o);

#if SLUA_CHECK_REFLECTION
            int isReflect = LuaDLL.luaS_pushobject(ptr, index, getAQName(o), gco, udCacheRef);
            if (isReflect != 0 && checkReflect)
            {
                Logger.LogWarning(string.Format("{0} not exported, using reflection instead", o.ToString()));
            }
#else
            LuaNativeMethods.luaS_pushobject(ptr, index, GetAQName(o), gco, cacheRef);
#endif
        }

        public bool IsGcObject(object obj)
        {
            return obj.GetType().IsValueType == false;
        }

        public bool IsObjInLua(object obj)
        {
            return objMap.ContainsKey(obj);
        }

#if SPEED_FREELIST
        class FreeList : List<ObjSlot>
        {
            public FreeList()
            {
                this.Add(new ObjSlot(0, null));
            }

            public int add(object o)
            {
                ObjSlot free = this[0];
                if (free.freeslot == 0)
                {
                    Add(new ObjSlot(this.Count, o));
                    return this.Count - 1;
                }
                else
                {
                    int slot = free.freeslot;
                    free.freeslot = this[slot].freeslot;
                    this[slot].v = o;
                    this[slot].freeslot = slot;
                    return slot;
                }
            }

            public void del(int i)
            {
                ObjSlot free = this[0];
                this[i].freeslot = free.freeslot;
                this[i].v = null;
                free.freeslot = i;
            }

            public bool get(int i, out object o)
            {
                if (i < 1 || i > this.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                ObjSlot slot = this[i];
                o = slot.v;
                return o != null;
            }

            public object get(int i)
            {
                object o;
                if (get(i, out o))
                    return o;
                return null;
            }

            public void set(int i, object o)
            {
                this[i].v = o;
            }
        }
#else

        public class FreeList : Dictionary<int, object>
        {
            private int id = 1;

            public int Add(object o)
            {
                Add(id, o);
                return id++;
            }

            public void Delete(int i)
            {
                this.Remove(i);
            }

            public bool Get(int i, out object o)
            {
                return TryGetValue(i, out o);
            }

            public object Get(int i)
            {
                object o;
                if (TryGetValue(i, out o))
                {
                    return o;
                }

                return null;
            }

            public void Set(int i, object o)
            {
                this[i] = o;
            }
        }
#endif

        public class ObjEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        public class ObjSlot
        {
            public ObjSlot(int slot, object o)
            {
                Freeslot = slot;
                Value = o;
            }

            public int Freeslot { get; private set; }

            public object Value { get; private set; }
        }
    }
}
