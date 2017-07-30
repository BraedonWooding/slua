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
    public class ByteArray
    {
        private byte[] data;
        private int pos;

        public ByteArray()
        {
            this.data = new byte[32];
            this.pos = 0;
        }

        public ByteArray(byte[] data)
        {
            this.SetData(data);
        }

        public int Length
        {
            get
            {
                return this.data.Length;
            }
        }

        public int Position
        {
            get
            {
                return this.pos;
            }

            set
            {
                this.pos = value;
            }
        }

        public static void ReAlloc(ref byte[] ba, int pos, int size)
        {
            if (ba.Length < (pos + size))
            {
                Array.Resize<byte>(ref ba, (int)(ba.Length + size + 1024));
            }
        }

        public void SetData(byte[] data)
        {
            this.data = data;
            this.pos = 0;
        }

        public void SetData(byte[] data, int len, int pos)
        {
            this.data = data;
            this.pos = pos;
        }

        public void Clear()
        {
            this.pos = 0;
        }

        public byte[] GetData()
        {
            return this.data;
        }

        public void Read(ref byte[] arr)
        {
            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = this.data[this.pos++];
            }
        }

        public bool ReadBool()
        {
            return this.ReadByte() == 1 ? true : false;
        }

        public sbyte ReadSignedChar()
        {
            return this.ReadSignedByte();
        }

        public byte ReadUnsignedChar()
        {
            return this.ReadByte();
        }

        public byte ReadByte()
        {
            return this.data[this.pos++];
        }

        public sbyte ReadSignedByte()
        {
            if (this.data[this.pos] > 127)
            {
                return (sbyte)(this.data[this.pos++] - 256);
            }
            else
            {
                return (sbyte)this.data[this.pos++];
            }
        }

        public short ReadShortInt()
        {
            int oldPos = this.pos;
            this.pos += 2;
            return BitConverter.ToInt16(this.data, oldPos);
        }

        public ushort ReadUnsignedShortInt()
        {
            int oldPos = this.pos;
            this.pos += 2;
            return BitConverter.ToUInt16(this.data, oldPos);
        }

        public int ReadInt()
        {
            int oldPos = this.pos;
            this.pos += 4;
            return BitConverter.ToInt32(this.data, oldPos);
        }

        public uint ReadUnsignedInt()
        {
            int oldPos = this.pos;
            this.pos += 4;
            return BitConverter.ToUInt32(this.data, oldPos);
        }

        public long ReadInt48()
        {
            uint low = this.ReadUnsignedInt();
            short high = this.ReadShortInt();
            long int48 = (long)((ulong)high << 32 | low);
            return int48;
        }

        public long ReadInt48L()
        {
            long low = (long)this.ReadUnsignedInt();
            long high = (long)this.ReadShortInt();
            long v = (long)(low | (high << 32));
            return v;
        }

        public long ReadLongInt()
        {
            int oldPos = this.pos;
            this.pos += 8;
            return BitConverter.ToInt64(this.data, oldPos);
        }

        public ulong ReadUnsignedLongInt()
        {
            int oldPos = this.pos;
            this.pos += 8;
            return BitConverter.ToUInt64(this.data, oldPos);
        }

        public float ReadFloat()
        {
            int oldPos = this.pos;
            this.pos += 4;
            return BitConverter.ToSingle(this.data, oldPos);
        }

        public double ReadDouble()
        {
            int oldPos = this.pos;
            this.pos += 8;
            return BitConverter.ToDouble(this.data, oldPos);
        }

        public string ReadString()
        {
            int len = (int)this.ReadVarInt();
            int oldPos = this.pos;
            this.pos += len;
            return System.Text.UTF8Encoding.UTF8.GetString(this.data, oldPos, len);
        }

        public long ReadVarInt()
        {
            byte ch = this.ReadByte();
            long v = ch & 0x7f;
            int shift = 7;
            while ((ch & 0x80) > 0 && this.pos < this.data.Length - 1)
            {
                ch = this.ReadByte();
                v |= ((long)(ch & 0x7f)) << shift;
                shift += 7;
            }

            return v;
        }

        public ByteArray ReadByteArray()
        {
            int len = this.data.Length - this.pos;

            ByteArray ba = new ByteArray();
            if (len == 0)
            {
                return ba;
            }

            byte[] data = new byte[len];
            for (int i = 0; i < len; ++i)
            {
                data[i] = this.data[this.pos++];
            }

            ba.SetData(data);

            return ba;
        }

        public byte[] ReadBytes()
        {
            ushort len = this.ReadUnsignedShortInt();

            int oldPos = this.pos;
            this.pos += len;

            byte[] bytes = new byte[len];
            for (int i = 0; i < len; ++i)
            {
                bytes[i] = this.data[oldPos + i];
            }

            return bytes;
        }

        public void WriteByteArray(ByteArray v)
        {
            if (v != null)
            {
                ReAlloc(ref this.data, this.pos, v.Position);
                byte[] arr = v.GetData();
                Array.Copy(arr, 0, this.data, this.pos, v.Position);
                this.pos += v.Position;
            }
        }

        public void WriteByteArray(byte[] arr)
        {
            ReAlloc(ref this.data, this.pos, arr.Length);
            foreach (byte v in arr)
            {
                this.data[this.pos++] = v;
            }
        }

        public void WriteBytes(byte[] v)
        {
            ushort len = (ushort)v.Length;
            this.WriteUnsignedShortInt(len);
            ReAlloc(ref this.data, this.pos, len);
            v.CopyTo(this.data, this.pos);
            this.pos += len;
        }

        public void WriteBool(bool v)
        {
            this.WriteByte(v ? (byte)1 : (byte)0);
        }

        public void WriteSByte(sbyte v)
        {
            ReAlloc(ref this.data, this.pos, 1);
            BytesHelper.MoveToBytes(this.data, this.pos, v);
            this.pos++;
        }

        public void WriteByte(byte v)
        {
            ReAlloc(ref this.data, this.pos, 1);
            this.data[this.pos] = v;
            this.pos++;
        }

        public void WriteUnsignedChar(byte v)
        {
            this.WriteByte(v);
        }

        public void WriteChar(char v)
        {
            ReAlloc(ref this.data, this.pos, 1);
            this.data[this.pos] = (byte)v;
            this.pos++;
        }

        public void WriteShortInt(short v)
        {
            ReAlloc(ref this.data, this.pos, 2);
            BytesHelper.MoveToBytes(this.data, this.pos, v);
            this.pos += 2;
        }

        public void WriteUnsignedShortInt(ushort v)
        {
            ReAlloc(ref this.data, this.pos, 2);
            BytesHelper.MoveToBytes(this.data, this.pos, v);
            this.pos += 2;
        }

        public void WriteInt(int v)
        {
            ReAlloc(ref this.data, this.pos, sizeof(int));
            BytesHelper.MoveToBytes(this.data, this.pos, v);
            this.pos += sizeof(int);
        }

        public void WriteUnsignedInt(uint v)
        {
            ReAlloc(ref this.data, this.pos, sizeof(uint));
            BytesHelper.MoveToBytes(this.data, this.pos, v);
            this.pos += sizeof(uint);
        }

        public void WriteUnsignedInt(uint v, int pos)
        {
            BytesHelper.MoveToBytes(this.data, pos, v);
            this.pos += sizeof(uint);
        }

        public void WriteInt48(long v)
        {
            this.WriteUnsignedInt(Convert.ToUInt32(v & 0x00000000ffffffff));
            this.WriteShortInt(Convert.ToInt16(v & 0x0000ffff00000000));
        }

        public void WriteLongInt(long v)
        {
            BytesHelper.MoveToBytes(this.data, this.pos, v);
            this.pos += sizeof(long);
        }

        public void WriteFloat(float v)
        {
            ReAlloc(ref this.data, this.pos, sizeof(float));
            BytesHelper.MoveToBytes(this.data, this.pos, v);
            this.pos += sizeof(float);
        }

        public void WriteDouble(double v)
        {
            ReAlloc(ref this.data, this.pos, sizeof(double));
            BytesHelper.MoveToBytes(this.data, this.pos, v);
            this.pos += sizeof(double);
        }

        public void WriteString(string v)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(v);
            int len = bytes.Length;
            this.WriteVarInt(bytes.Length);
            ReAlloc(ref this.data, this.pos, len);
            bytes.CopyTo(this.data, this.pos);
            this.pos += len;
        }

        public void WriteVarInt(long v)
        {
            ulong uv = (ulong)v;
            while (uv >= 0x80)
            {
                this.WriteByte((byte)(uv | 0x80));
                uv >>= 7;
            }

            this.WriteByte((byte)uv);
        }
    }
}