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

using SLua;
using UnityEngine;

public class ValueType : MonoBehaviour
{
    private LuaSvr l;
    // Use this for initialization
    private void Start()
    {
        l = new LuaSvr();
        l.Init(null, () =>
        {
            l.Start("valuetype");
        });

        using (LuaState newState = new LuaState())
        {
            LuaTable table = new LuaTable(newState);

            Vector2 v2 = new Vector2(1, 2);
            Vector3 v3 = new Vector3(1, 2, 3);
            Vector4 v4 = new Vector4(1, 2, 3, 4);
            Color col = new Color(.1f, .2f, .3f);
            Foo foo = new Foo();

            table["v2"] = v2;
            table["v3"] = v3;
            table["v4"] = v4;
            table["col"] = col;
            table["foo"] = foo;

            Assert.IsTrue((Vector2)table["v2"] == v2);
            Assert.IsTrue((Vector3)table["v3"] == v3);
            Assert.IsTrue((Vector4)table["v4"] == v4);
            Assert.IsTrue((Color)table["col"] == col);
            Assert.IsTrue(table["foo"] == foo);
        }
    }

    private class Foo
    {
    }
}
