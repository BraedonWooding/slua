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
    public class LuaSvrGameObject : MonoBehaviour
    {
        public LuaState State { get; set; }

        public Action OnUpdate { get; set; }

        public void OnDestroy()
        {
            if (this.State != null)
            {
                // state is disposed by editorapplication if in the Editor
                // state isn't disposed in App because that all resources will be disposed by app on process exit.
            }
        }

        public void Init()
        {
        }

        public void Update()
        {
            if (this.OnUpdate != null)
            {
                this.OnUpdate();
            }
        }
    }
}