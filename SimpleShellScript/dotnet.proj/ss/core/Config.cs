﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SScript
{
    public class Config
    {
        public const string MAGIC_THIS = "this";
        public static string def_shell = "bash";

        // 稍微优化下性能，(/ □ \)
        public static readonly List<object> EmptyResults = new List<object>();
    }
}
