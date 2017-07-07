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

namespace SLua
{
    public class LuaTimer : LuaObject
    {
        public const int JiffiesMsec = 20;
        public const float JiffiesSec = JiffiesMsec * .001f;

        public static int NextSn { get; private set; }

        public static Wheel[] Wheels { get; private set; }

        public static float PileSecs { get; private set; }

        public static float NowTime { get; private set; }

        public static Dictionary<int, Timer> MapSnTimer { get; private set; }

        public static LinkedList<Timer> ExecuteTimers { get; private set; }

        public static int IntPow(int n, int m)
        {
            int ret = 1;
            for (int i = 0; i < m; ++i)
            {
                ret *= n;
            }

            return ret;
        }

        public static void InnerAdd(int deadline, Timer tm)
        {
            tm.Deadline = deadline;
            int delay = Math.Max(0, deadline - Now());
            Wheel suitableWheel = Wheels[Wheels.Length - 1];
            for (int i = 0; i < Wheels.Length; ++i)
            {
                Wheel wheel = Wheels[i];
                if (delay < wheel.TimeRange)
                {
                    suitableWheel = wheel;
                    break;
                }
            }

            suitableWheel.Add(delay, tm);
        }

        public static void InnerDelete(Timer tm)
        {
            InnerDelete(tm, true);
        }

        public static void InnerDelete(Timer tm, bool removeFromMap)
        {
            tm.Delete = true;
            if (tm.Container != null)
            {
                tm.Container.Remove(tm);
                tm.Container = null;
            }

            if (removeFromMap)
            {
                MapSnTimer.Remove(tm.SN);
            }
        }

        public static int Now()
        {
            return (int)(NowTime * 1000);
        }

        public static void Tick(float deltaTime)
        {
            NowTime += deltaTime;
            PileSecs += deltaTime;
            int cycle = 0;
            while (PileSecs >= JiffiesSec)
            {
                PileSecs -= JiffiesSec;
                cycle++;
            }

            for (int i = 0; i < cycle; ++i)
            {
                LinkedList<Timer> timers = Wheels[0].NextDial();
                LinkedListNode<Timer> node = timers.First;
                for (int j = 0; j < timers.Count; ++j)
                {
                    Timer tm = node.Value;
                    ExecuteTimers.AddLast(tm);
                    node = node.Next;
                }

                timers.Clear();

                for (int j = 0; j < Wheels.Length; ++j)
                {
                    Wheel wheel = Wheels[j];
                    if (wheel.Head == Wheel.DialScale)
                    {
                        wheel.Head = 0;
                        if (wheel.NextWheel != null)
                        {
                            LinkedList<Timer> tms = wheel.NextWheel.NextDial();
                            LinkedListNode<Timer> tmsNode = tms.First;
                            for (int k = 0; k < tms.Count; ++k)
                            {
                                Timer tm = tmsNode.Value;
                                if (tm.Delete)
                                {
                                    MapSnTimer.Remove(tm.SN);
                                }
                                else
                                {
                                    InnerAdd(tm.Deadline, tm);
                                }

                                tmsNode = tmsNode.Next;
                            }

                            tms.Clear();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            while (ExecuteTimers.Count > 0)
            {
                Timer tm = ExecuteTimers.First.Value;
                ExecuteTimers.Remove(tm);

                if (!tm.Delete && tm.Handler(tm.SN) && tm.Cycle > 0)
                {
                    InnerAdd(Now() + tm.Cycle, tm);
                }
                else
                {
                    MapSnTimer.Remove(tm.SN);
                }
            }
        }

        public static void Init()
        {
            Wheels = new Wheel[4];
            for (int i = 0; i < 4; ++i)
            {
                Wheels[i] = new Wheel(JiffiesMsec * IntPow(Wheel.DialScale, i));
                if (i > 0)
                {
                    Wheels[i - 1].NextWheel = Wheels[i];
                }
            }

            MapSnTimer = new Dictionary<int, Timer>();
            ExecuteTimers = new LinkedList<Timer>();
        }

        public static int FetchSn()
        {
            return ++NextSn;
        }

        public static int Add(int delay, Action<int> handler)
        {
            return Add(delay, 0, (int sn) =>
                       {
                           handler(sn);
                           return false;
                       });
        }

        public static int Add(int delay, int cycle, Func<int, bool> handler)
        {
            Timer tm = new Timer()
            {
                SN = FetchSn(),
                Cycle = cycle,
                Handler = handler
            };
            MapSnTimer[tm.SN] = tm;
            InnerAdd(Now() + delay, tm);
            return tm.SN;
        }

        public static void Delete(int sn)
        {
            Timer tm;
            if (MapSnTimer.TryGetValue(sn, out tm))
            {
                InnerDelete(tm);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Delete(IntPtr ptr)
        {
            try
            {
                int id;
                CheckType(ptr, 1, out id);
                Delete(id);
                return Ok(ptr);
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int Add(IntPtr ptr)
        {
            try
            {
                int top = LuaNativeMethods.lua_gettop(ptr);
                if (top == 2)
                {
                    int delay;
                    CheckType(ptr, 1, out delay);
                    LuaDelegate ld;
                    CheckType(ptr, 2, out ld);
                    Action<int> ua;
                    if (ld.D != null)
                    {
                        ua = (Action<int>)ld.D;
                    }
                    else
                    {
                        IntPtr ml = LuaState.Get(ptr).StatePointer;
                        ua = (int id) =>
                        {
                            int error = PushTry(ml);
                            LuaObject.PushValue(ml, id);
                            ld.ProtectedCall(1, error);
                            LuaNativeMethods.lua_settop(ml, error - 1);
                        };
                    }

                    ld.D = ua;
                    LuaObject.PushValue(ptr, true);
                    LuaObject.PushValue(ptr, Add(delay, ua));
                    return 2;
                }
                else if (top == 3)
                {
                    int delay, cycle;
                    CheckType(ptr, 1, out delay);
                    CheckType(ptr, 2, out cycle);
                    LuaDelegate ld;
                    CheckType(ptr, 3, out ld);
                    Func<int, bool> ua;

                    if (ld.D != null)
                    {
                        ua = (Func<int, bool>)ld.D;
                    }
                    else
                    {
                        IntPtr ml = LuaState.Get(ptr).StatePointer;
                        ua = (int id) =>
                        {
                            int error = PushTry(ml);
                            LuaObject.PushValue(ml, id);
                            ld.ProtectedCall(1, error);
                            bool ret = LuaNativeMethods.lua_toboolean(ml, -1);
                            LuaNativeMethods.lua_settop(ml, error - 1);
                            return ret;
                        };
                    }

                    ld.D = ua;
                    LuaObject.PushValue(ptr, true);
                    LuaObject.PushValue(ptr, Add(delay, cycle, ua));
                    return 2;
                }

                return LuaObject.Error(ptr, "Argument error");
            }
            catch (Exception e)
            {
                return LuaObject.Error(ptr, e);
            }
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        public static int DeleteAll(IntPtr ptr)
        {
            if (MapSnTimer == null)
            {
                return 0;
            }

            try
            {
                foreach (KeyValuePair<int, Timer> t in MapSnTimer)
                {
                    InnerDelete(t.Value, false);
                }

                MapSnTimer.Clear();

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
            Init();
            GetTypeTable(ptr, "LuaTimer");
            AddMember(ptr, Add, false);
            AddMember(ptr, Delete, false);
            AddMember(ptr, DeleteAll, false);
            CreateTypeMetatable(ptr, typeof(LuaTimer));
        }

        public class Timer
        {
            public int SN { get; set; }

            public int Cycle { get; set; }

            public int Deadline { get; set; }

            public Func<int, bool> Handler { get; set; }

            public bool Delete { get; set; }

            public LinkedList<Timer> Container { get; set; }
        }

        public class Wheel
        {
            public const int DialScale = 256;

            public Wheel(int dialSize)
            {
                this.DialSize = dialSize;
                this.TimeRange = dialSize * DialScale;
                this.Head = 0;
                this.VecDial = new LinkedList<Timer>[DialScale];
                for (int i = 0; i < DialScale; ++i)
                {
                    this.VecDial[i] = new LinkedList<Timer>();
                }
            }

            public int Head { get; set; }

            public Wheel NextWheel { get; set; }

            public LinkedList<Timer>[] VecDial { get; set; }

            public int DialSize { get; private set; }

            public int TimeRange { get; private set; }

            public LinkedList<Timer> NextDial()
            {
                return VecDial[Head++];
            }

            public void Add(int delay, Timer tm)
            {
                LinkedList<Timer> container = VecDial[((Head + (delay - (DialSize - JiffiesMsec))) / DialSize) % DialScale];
                container.AddLast(tm);
                tm.Container = container;
            }
        }
    }
}