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

public class VarObj : MonoBehaviour
{
    private LuaSvr l;

    // Use this for initialization
    private void Start()
    {
        l = new LuaSvr();
        l.Init(null, () =>
        {
            l.Start("varobj");
        });
    }
}
