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

public class Circle : MonoBehaviour
{
    private LuaSvr svr;
    private LuaTable self;
    private LuaFunction update;

    public void Start()
    {
        svr = new LuaSvr();
        svr.Init(null, () =>
        {
            self = (LuaTable)svr.Start("circle/circle");
            update = (LuaFunction)self["update"];
        });
    }

    public void Update()
    {
        if (update != null)
        {
            update.Call(self);
        }
    }
}
