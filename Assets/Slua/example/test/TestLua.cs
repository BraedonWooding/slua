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

namespace NLuaTest.Mock
{
    using System;
    using System.Threading;
    using SLua;
    using Debug = UnityEngine.Debug;

    public class Parameter
    {
        private string field1 = "parameter-field1";

        public string Field1
        {
            get
            {
                return field1;
            }

            set
            {
                field1 = value;
            }
        }
    }

    public class UnicodeClass
    {
        public static readonly char UnicodeChar = '\uE007';

        public static string UnicodeString
        {
            get
            {
                return Convert.ToString(UnicodeChar);
            }
        }
    }

#if MONOTOUCH
    [Preserve (AllMembers = true)]
#endif
    public class Master
    {
        public static string read()
        {
            return "test-master";
        }

        public static string read(Parameter test)
        {
            return test.Field1;
        }
    }

#if MONOTOUCH
    [Preserve (AllMembers = true)]
#endif
    public class TestClass3 : Master
    {
        private String strData;

        private int intData;

        public string StrData
        {
            get
            {
                return strData;
            }

            set
            {
                strData = value;
            }
        }

        public int IntData
        {
            get
            {
                return intData;
            }

            set
            {
                intData = value;
            }
        }

        public static string Read2()
        {
            return "test";
        }

        public static string Read(int test)
        {
            return "int-test";
        }
    }

#if MONOTOUCH
    [Preserve (AllMembers = true)]
#endif
    public class TestCaseName
    {
        public string name = "name";

        public string Name
        {
            get
            {
                return "**" + name + "**";
            }
        }
    }

#if MONOTOUCH
        [Preserve (AllMembers = true)]
#endif
    public class Vector
    {
        public double X;

        public double Y;

        public static Vector operator *(float k, Vector v)
        {
            Vector r = new Vector()
            {
                X = v.X * k,
                Y = v.Y * k
            };
            return r;
        }

        public static Vector operator *(Vector v, float k)
        {
            Vector r = new Vector()
            {
                X = v.X * k,
                Y = v.Y * k
            };
            return r;
        }

        public void Func()
        {
            Debug.Log("Func");
        }
    }

    public static class VectorExtension
    {
        public static double Length(this Vector v)
        {
            return (v.X * v.X) + (v.Y * v.Y);
        }
    }

    public class DefaultElementModel
    {
        public Action<double> DrawMe { get; set; }
    }

    /*
     * Delegates used for testing Lua function -> delegate translation
     */
    public delegate int TestDelegate1(int a, int b);

    public delegate int TestDelegate2(int a, out int b);

    public delegate void TestDelegate3(int a, ref int b);

    public delegate TestClass TestDelegate4(int a, int b);

    public delegate int TestDelegate5(TestClass a, TestClass b);

    public delegate int TestDelegate6(int a, out TestClass b);

    public delegate void TestDelegate7(int a, ref TestClass b);

    /* Delegate Lua-handlers */

    //	class LuaTestDelegate1Handler : NLua.Method.LuaDelegate
    //	{
    //		int CallFunction (int a, int b)
    //		{
    //			object [] args = new object [] { a, b };
    //			object [] inArgs = new object [] { a, b };
    //			int [] outArgs = new int [] { };
    //			
    //			object ret = base.CallFunction (args, inArgs, outArgs);
    //			
    //			return (int)ret;
    //		}
    //	}
    //	
    //	class LuaTestDelegate2Handler : NLua.Method.LuaDelegate
    //	{
    //		int CallFunction (int a, out int b)
    //		{
    //			object [] args = new object [] { a, 0 };
    //			object [] inArgs = new object [] { a };
    //			int [] outArgs = new int [] { 1 };
    //			
    //			object ret = base.CallFunction (args, inArgs, outArgs);
    //			
    //			b = (int)args [1];
    //			return (int)ret;
    //		}
    //	}
    //	
    //	class LuaTestDelegate3Handler : NLua.Method.LuaDelegate
    //	{
    //		void CallFunction (int a, ref int b)
    //		{
    //			object [] args = new object [] { a, b };
    //			object [] inArgs = new object [] { a, b };
    //			int [] outArgs = new int [] { 1 };
    //			
    //			base.CallFunction (args, inArgs, outArgs);
    //			
    //			b = (int)args [1];
    //		}
    //	}
    //	
    //	class LuaTestDelegate4Handler : NLua.Method.LuaDelegate
    //	{
    //		TestClass CallFunction (int a, int b)
    //		{
    //			object [] args = new object [] { a, b };
    //			object [] inArgs = new object [] { a, b };
    //			int [] outArgs = new int [] { };
    //			
    //			object ret = base.CallFunction (args, inArgs, outArgs);
    //			
    //			return (TestClass)ret;
    //		}
    //	}
    //	
    //	class LuaTestDelegate5Handler : NLua.Method.LuaDelegate
    //	{	
    //		int CallFunction (TestClass a, TestClass b)
    //		{
    //			object [] args = new object [] { a, b };
    //			object [] inArgs = new object [] { a, b };
    //			int [] outArgs = new int [] {  };
    //			
    //			object ret = base.CallFunction (args, inArgs, outArgs);
    //			
    //			return (int)ret;
    //		}
    //	}
    //	
    //	class LuaTestDelegate6Handler : NLua.Method.LuaDelegate
    //	{
    //		int CallFunction (int a, ref TestClass b)
    //		{
    //			object [] args = new object [] { a, b };
    //			object [] inArgs = new object [] { a };
    //			int [] outArgs = new int [] { 1 };
    //			
    //			object ret = base.CallFunction (args, inArgs, outArgs);
    //			
    //			b = (TestClass)args [1];
    //			return (int)ret;
    //		}
    //	}
    //	
    //	class LuaTestDelegate7Handler : NLua.Method.LuaDelegate
    //	{
    //		void CallFunction (int a, ref TestClass b)
    //		{
    //			object [] args = new object [] { a, b };
    //			object [] inArgs = new object [] { a , b};
    //			int [] outArgs = new int [] { 1 };
    //			
    //			base.CallFunction (args, inArgs, outArgs);
    //			
    //			b = (TestClass)args [1];
    //		}
    //	}
    /*
     * Interface used for testing Lua table -> interface translation
     */
    public interface ITest
    {
        int IntProp
        {
            get;
            set;
        }

        TestClass RefProp
        {
            get;
            set;
        }

        int Test1(int a, int b);

        int Test2(int a, out int b);

        void Test3(int a, ref int b);

        TestClass Test4(int a, int b);

        int Test5(TestClass a, TestClass b);

        int Test6(int a, out TestClass b);

        void Test7(int a, ref TestClass b);
    }

    public interface IFoo1
    {
        int foo();
    }

    public interface IFoo2
    {
        int foo();
    }

    public class MyClass
    {
        public int Func1()
        {
            return 1;
        }
    }

    /// <summary>
    /// Use to test threading.
    /// </summary>
    public class DoWorkClass
    {
        public void DoWork()
        {
            //simulate work by sleeping
            //Debug.Log("Started to do work on thread: " + Thread.CurrentThread.ManagedThreadId);
            Thread.Sleep(new Random().Next(0, 1000));
            //Debug.Log("Finished work on thread: " + Thread.CurrentThread.ManagedThreadId);
        }
    }

    /// <summary>
    /// Test structure passing.
    /// </summary>
    public struct TestStruct
    {
        public TestStruct(float val)
        {
            v = val;
        }

        public float v;

        public float val
        {
            get { return v; }
            set { v = value; }
        }
    }

    /// <summary>
    /// Test enum.
    /// </summary>
    public enum TestEnum
    {
        ValueA,
        ValueB
    }

    /// <summary>
    /// Generic class with generic and non-generic methods.
    /// </summary>
    public class TestClassGeneric<T>
    {
        private object _PassedValue;

        private bool _RegularMethodSuccess;

        public bool RegularMethodSuccess
        {
            get { return _RegularMethodSuccess; }
        }

        private bool _GenericMethodSuccess;

        public bool GenericMethodSuccess
        {
            get { return _GenericMethodSuccess; }
        }

        public void GenericMethod(T value)
        {
            _PassedValue = value;
            _GenericMethodSuccess = true;
        }

        public void RegularMethod()
        {
            _RegularMethodSuccess = true;
        }

        /// <summary>
        /// Returns true if the generic method was successfully passed a matching value.
        /// </summary>
        public bool Validate(T value)
        {
            return value.Equals(_PassedValue);
        }
    }

    /// <summary>
    /// Normal class containing a generic method.
    /// </summary>
    public class TestClassWithGenericMethod
    {
        private object _PassedValue;

        public object PassedValue
        {
            get { return _PassedValue; }
        }

        private bool _GenericMethodSuccess;

        public bool GenericMethodSuccess
        {
            get { return _GenericMethodSuccess; }
        }

        public void GenericMethod<T>(T value)
        {
            _PassedValue = value;
            _GenericMethodSuccess = true;
        }

        public bool Validate<T>(T value)
        {
            return value.Equals(_PassedValue);
        }
    }

    public class TestClass2
    {
        public static int func(int x, int y)
        {
            return x + y;
        }

        public int funcInstance(int x, int y)
        {
            return x + y;
        }
    }

    /*
     * Sample class used in several test cases to check if
     * Lua scripts are accessing objects correctly
     */
    public class TestClass : IFoo1, IFoo2
    {
        public int val;
        private string strVal;

        public TestClass()
        {
            val = 0;
        }

        public TestClass(int val)
        {
            this.val = val;
        }

        public TestClass(string val)
        {
            this.strVal = val;
        }

        public static TestClass makeFromString(String str)
        {
            return new TestClass(str);
        }

        public TestStruct s = new TestStruct();

        public TestStruct Struct
        {
            get { return s; }
            set { s = (TestStruct)value; }
        }

        public int TestVal
        {
            get
            {
                return this.val;
            }

            set
            {
                this.val = value;
            }
        }

        public string TestStrval
        {
            get
            {
                return this.strVal;
            }

            set
            {
                this.strVal = value;
            }
        }

        public int this[int index]
        {
            get
            {
                return 1;
            }
        }

        public int this[string index]
        {
            get
            {
                return 1;
            }
        }

        public object TestLuaFunction(LuaFunction func)
        {
            if (func != null)
            {
                return func.Call(1, 2);
            }

            return null;
        }

        public int sum(int x, int y)
        {
            return x + y;
        }

        public void setVal(int newVal)
        {
            val = newVal;
        }

        public void setVal(string newVal)
        {
            strVal = newVal;
        }

        public int getVal()
        {
            return val;
        }

        public string getStrVal()
        {
            return strVal;
        }

        public int outVal(out int val)
        {
            val = 5;
            return 3;
        }

        public int outVal(out int val, int val2)
        {
            val = 5;
            return val2;
        }

        public int outVal(int val, ref int val2)
        {
            val2 = val + val2;
            return val;
        }

        public int outValMutiple(int arg, out string arg2, out string arg3)
        {
            arg2 = Guid.NewGuid().ToString();
            arg3 = Guid.NewGuid().ToString();

            return arg;
        }

        public int callDelegate1(TestDelegate1 del)
        {
            return del(2, 3);
        }

        public int callDelegate2(TestDelegate2 del)
        {
            int a = 3;
            int b = del(2, out a);
            return a + b;
        }

        public int callDelegate3(TestDelegate3 del)
        {
            int a = 3;
            del(2, ref a);
            //Debug.Log(a);
            return a;
        }

        public int callDelegate4(TestDelegate4 del)
        {
            return del(2, 3).TestVal;
        }

        public int callDelegate5(TestDelegate5 del)
        {
            return del(new TestClass(2), new TestClass(3));
        }

        public int callDelegate6(TestDelegate6 del)
        {
            TestClass test = new TestClass();
            int a = del(2, out test);
            return a + test.TestVal;
        }

        public int callDelegate7(TestDelegate7 del)
        {
            TestClass test = new TestClass(3);
            del(2, ref test);
            return test.TestVal;
        }

        public int callInterface1(ITest itest)
        {
            return itest.Test1(2, 3);
        }

        public int callInterface2(ITest itest)
        {
            int a = 3;
            int b = itest.Test2(2, out a);
            return a + b;
        }

        public int callInterface3(ITest itest)
        {
            int a = 3;
            itest.Test3(2, ref a);
            //Debug.Log(a);
            return a;
        }

        public int callInterface4(ITest itest)
        {
            return itest.Test4(2, 3).TestVal;
        }

        public int callInterface5(ITest itest)
        {
            return itest.Test5(new TestClass(2), new TestClass(3));
        }

        public int callInterface6(ITest itest)
        {
            TestClass test = new TestClass();
            int a = itest.Test6(2, out test);
            return a + test.TestVal;
        }

        public int callInterface7(ITest itest)
        {
            TestClass test = new TestClass(3);
            itest.Test7(2, ref test);
            return test.TestVal;
        }

        public int callInterface8(ITest itest)
        {
            itest.IntProp = 3;
            return itest.IntProp;
        }

        public int callInterface9(ITest itest)
        {
            itest.RefProp = new TestClass(3);
            return itest.RefProp.TestVal;
        }

        public void exceptionMethod()
        {
            throw new Exception("exception test");
        }

        public virtual int overridableMethod(int x, int y)
        {
            return x + y;
        }

        public static int callOverridable(TestClass test, int x, int y)
        {
            return test.overridableMethod(x, y);
        }

        int IFoo1.foo()
        {
            return 3;
        }

        public int foo()
        {
            return 5;
        }

        private void _PrivateMethod()
        {
            Debug.Log("Private method called");
        }

        public int MethodOverload()
        {
            Debug.Log("Method with no params");
            return 1;
        }

        public int MethodOverload(TestClass testClass)
        {
            Debug.Log("Method with testclass param");
            return 2;
        }

        public int MethodOverload(Type type)
        {
            Debug.Log("Method with testclass param");
            return 3;
        }

        public int MethodOverload(int i, int j, int k)
        {
            Debug.Log("Overload without out param: " + i + ", " + j + ", " + k);
            return 4;
        }

        public int MethodOverload(int i, int j, out int k)
        {
            k = 5;
            Debug.Log("Overload with out param" + i + ", " + j);
            return 5;
        }

        public void Print(object format, params object[] args)
        {
            //just for test,this is not printf implements
            string output = format.ToString() + "\t";
            foreach (object msg in args)
            {
                output += msg.ToString() + "\t";
            }

            Debug.Log(output);
        }

        public static int MethodWithParams(int a, params int[] others)
        {
            Debug.Log(a);
            int i = 0;
            foreach (int val in others)
            {
                Debug.Log(val);
                i++;
            }

            return i;
        }

        public bool TestType(Type t)
        {
            return this.GetType() == t;
        }
    }

    public class TestClassWithOverloadedMethod
    {
        public int CallsToStringFunc { get; set; }

        public int CallsToIntFunc { get; set; }

        public void Func(string param)
        {
            CallsToStringFunc++;
        }

        public void Func(int param)
        {
            CallsToIntFunc++;
        }
    }
}
