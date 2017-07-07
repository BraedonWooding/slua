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
    public class WeakDictionary<K, V>
    {
        private Dictionary<K, WeakReference> dict = new Dictionary<K, WeakReference>();

        public ICollection<K> Keys
        {
            get
            {
                return this.dict.Keys;
            }
        }

        public ICollection<V> Values
        {
            get
            {
                List<V> l = new List<V>();
                foreach (K key in dict.Keys)
                {
                    l.Add((V)dict[key].Target);
                }

                return l;
            }
        }

        public V this[K key]
        {
            get
            {
                WeakReference w = dict[key];
                return w.IsAlive ? (V)w.Target : default(V);
            }

            set
            {
                this.Add(key, value);
            }
        }

        public void Add(K key, V value)
        {
            if (dict.ContainsKey(key))
            {
                if (dict[key].IsAlive)
                {
                    throw new ArgumentException("Key Exists");
                }

                dict[key].Target = value;
            }
            else
            {
                WeakReference w = new WeakReference(value);
                dict.Add(key, w);
            }
        }

        public bool ContainsKey(K key)
        {
            return dict.ContainsKey(key);
        }

        public bool Remove(K key)
        {
            return dict.Remove(key);
        }

        public bool TryGetValue(K key, out V value)
        {
            WeakReference w;
            if (dict.TryGetValue(key, out w))
            {
                value = (V)w.Target;
                return true;
            }

            value = default(V);
            return false;
        }
    }
}