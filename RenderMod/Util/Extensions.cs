using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RenderMod.Util
{
    static class Extensions
    {
        public static string GetObjectPath(this Transform current, int depth)
        {
            StringBuilder path = new StringBuilder();
            Transform temp = current;
            int currentDepth = 0;
            while (temp != null && currentDepth < depth)
            {
                path.Insert(0, "/" + temp.name);
                temp = temp.parent;
                currentDepth++;
            }
            return path.ToString();
        }
    }
}
