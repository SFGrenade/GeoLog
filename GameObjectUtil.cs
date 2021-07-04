using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GeoLog
{
    public static class GameObjectUtil
    {
        public static string GetGoPath(this GameObject self)
        {
            StringBuilder ret = new StringBuilder();
            Transform p = self.transform;
            while (p != null)
            {
                ret.Insert(0, $"/{p.gameObject.name}");
                p = p.parent;
            }
            return ret.ToString();
        }
    }
}
