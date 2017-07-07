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

namespace SLua
{
    public class Lua_SLua_ByteArray : LuaObject
    {
        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Constructor(IntPtr ptr)
        {
            try
            {
                int argc = LuaNativeMethods.lua_gettop(ptr);
                SLua.ByteArray o;
                if (argc == 1)
                {
                    o = new SLua.ByteArray();
                    LuaObject.PushValue(ptr, true);
                    LuaObject.PushValue(ptr, o);
                    return 2;
                }
                else if (argc == 2)
                {
                    byte[] a1;
                    LuaObject.CheckArray(ptr, 2, out a1);
                    o = new SLua.ByteArray(a1);
                    LuaObject.PushValue(ptr, true);
                    LuaObject.PushValue(ptr, o);
                    return 2;
                }

                return LuaObject.Error(ptr, "New object failed.");
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int SetData(IntPtr ptr)
        {
            try
            {
                int argc = LuaNativeMethods.lua_gettop(ptr);
                if (argc == 2)
                {
                    SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                    byte[] a1;
                    LuaObject.CheckArray(ptr, 2, out a1);
                    self.SetData(a1);
                    LuaObject.PushValue(ptr, true);
                    return 1;
                }
                else if (argc == 4)
                {
                    SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                    byte[] a1;
                    LuaObject.CheckArray(ptr, 2, out a1);
                    int a2;
                    LuaObject.CheckType(ptr, 3, out a2);
                    int a3;
                    LuaObject.CheckType(ptr, 4, out a3);
                    self.SetData(a1, a2, a3);
                    LuaObject.PushValue(ptr, true);
                    return 1;
                }

                LuaObject.PushValue(ptr, false);
                LuaNativeMethods.lua_pushstring(ptr, "No matched override function SetData to call");
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Clear(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                self.Clear();
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int GetData(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                byte[] ret = self.GetData();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadBool(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                bool ret = self.ReadBool();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadInt(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                int ret = self.ReadInt();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadUInt(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                uint ret = self.ReadUnsignedInt();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadChar(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                sbyte ret = self.ReadSignedChar();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadUChar(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                byte ret = self.ReadUnsignedChar();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadByte(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                byte ret = self.ReadByte();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Read(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                byte[] a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.Read(ref a1);
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, a1);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadSByte(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                sbyte ret = self.ReadSignedByte();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadShort(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                short ret = self.ReadShortInt();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadUShort(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                ushort ret = self.ReadUnsignedShortInt();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadInt16(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                short ret = self.ReadShortInt();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadUInt16(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                ushort ret = self.ReadUnsignedShortInt();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadInt64(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                long ret = self.ReadLongInt();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadFloat(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                float ret = self.ReadFloat();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadDouble(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                double ret = self.ReadDouble();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadString(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                string ret = self.ReadString();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteByteArray(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                SLua.ByteArray a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteByteArray(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteBool(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                bool a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteBool(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteInt(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                int a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteInt(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteUInt(IntPtr ptr)
        {
            try
            {
                int argc = LuaNativeMethods.lua_gettop(ptr);
                if (argc == 2)
                {
                    SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                    uint a1;
                    LuaObject.CheckType(ptr, 2, out a1);
                    self.WriteUnsignedInt(a1);
                    LuaObject.PushValue(ptr, true);
                    return 1;
                }
                else if (argc == 3)
                {
                    SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                    uint a1;
                    LuaObject.CheckType(ptr, 2, out a1);
                    int a2;
                    LuaObject.CheckType(ptr, 3, out a2);
                    self.WriteUnsignedInt(a1, a2);
                    LuaObject.PushValue(ptr, true);
                    return 1;
                }

                LuaObject.PushValue(ptr, false);
                LuaNativeMethods.lua_pushstring(ptr, "No matched override functionthis.WriteUInt to call");
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteChar(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                sbyte a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteSByte(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteByte(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                byte a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteByte(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteUChar(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                byte a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteByte(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteSByte(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                sbyte a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteSByte(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteUShort(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                ushort a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteUnsignedShortInt(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteShort(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                short a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteShortInt(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteFloat(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                float a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteFloat(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteNum(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                double a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteDouble(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteString(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                string a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteString(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteInt64(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                long a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteLongInt(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadVarInt(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                long ret = self.ReadVarInt();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteVarInt(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                long a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteVarInt(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadInt48(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                long ret = self.ReadInt48();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadInt48L(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                long ret = self.ReadInt48L();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteInt48(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                long a1;
                LuaObject.CheckType(ptr, 2, out a1);
                self.WriteInt48(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadByteArray(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                ByteArray ret = self.ReadByteArray();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadUInt64(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                ulong ret = self.ReadUnsignedLongInt();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int ReadBytes(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                byte[] ret = self.ReadBytes();
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, ret);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int WriteBytes(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                byte[] a1;
                LuaObject.CheckArray(ptr, 2, out a1);
                self.WriteBytes(a1);
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int GetLength(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, self.Length);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int GetPosition(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                LuaObject.PushValue(ptr, true);
                LuaObject.PushValue(ptr, self.Position);
                return 2;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int SetPosition(IntPtr ptr)
        {
            try
            {
                SLua.ByteArray self = (SLua.ByteArray)LuaObject.CheckSelf(ptr);
                int v;
                LuaObject.CheckType(ptr, 2, out v);
                self.Position = v;
                LuaObject.PushValue(ptr, true);
                return 1;
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        public static void Register(IntPtr ptr)
        {
            LuaObject.GetTypeTable(ptr, "Slua.ByteArray");
            LuaObject.AddMember(ptr, SetData);
            LuaObject.AddMember(ptr, Clear);
            LuaObject.AddMember(ptr, GetData);
            LuaObject.AddMember(ptr, ReadBool);
            LuaObject.AddMember(ptr, ReadInt);
            LuaObject.AddMember(ptr, ReadUInt);
            LuaObject.AddMember(ptr, ReadChar);
            LuaObject.AddMember(ptr, ReadUChar);
            LuaObject.AddMember(ptr, ReadByte);
            LuaObject.AddMember(ptr, Read);
            LuaObject.AddMember(ptr, ReadSByte);
            LuaObject.AddMember(ptr, ReadShort);
            LuaObject.AddMember(ptr, ReadUShort);
            LuaObject.AddMember(ptr, ReadInt16);
            LuaObject.AddMember(ptr, ReadUInt16);
            LuaObject.AddMember(ptr, ReadInt64);
            LuaObject.AddMember(ptr, ReadFloat);
            LuaObject.AddMember(ptr, ReadDouble);
            LuaObject.AddMember(ptr, ReadString);
            LuaObject.AddMember(ptr, WriteByteArray);
            LuaObject.AddMember(ptr, WriteBool);
            LuaObject.AddMember(ptr, WriteInt);
            LuaObject.AddMember(ptr, WriteUInt);
            LuaObject.AddMember(ptr, WriteChar);
            LuaObject.AddMember(ptr, WriteByte);
            LuaObject.AddMember(ptr, WriteUChar);
            LuaObject.AddMember(ptr, WriteSByte);
            LuaObject.AddMember(ptr, WriteUShort);
            LuaObject.AddMember(ptr, WriteShort);
            LuaObject.AddMember(ptr, WriteFloat);
            LuaObject.AddMember(ptr, WriteNum);
            LuaObject.AddMember(ptr, WriteString);
            LuaObject.AddMember(ptr, WriteInt64);
            LuaObject.AddMember(ptr, ReadVarInt);
            LuaObject.AddMember(ptr, WriteVarInt);
            LuaObject.AddMember(ptr, ReadInt48);
            LuaObject.AddMember(ptr, ReadInt48L);
            LuaObject.AddMember(ptr, WriteInt48);
            LuaObject.AddMember(ptr, ReadByteArray);
            LuaObject.AddMember(ptr, ReadUInt64);
            LuaObject.AddMember(ptr, ReadBytes);
            LuaObject.AddMember(ptr, WriteBytes);
            LuaObject.AddMember(ptr, "Length", GetLength, null, true);
            LuaObject.AddMember(ptr, "Position", GetPosition, SetPosition, true);
            LuaObject.CreateTypeMetatable(ptr, Constructor, typeof(SLua.ByteArray));
        }
    }
}
