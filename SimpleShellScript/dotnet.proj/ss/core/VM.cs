﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SScript
{
    /// <summary>
    /// 管理各个Module
    /// 管理全局表
    /// 
    /// 维护一些用户接口
    /// </summary>
    public class VM
    {
        public readonly Dictionary<string,object> global_table = new Dictionary<string, object>();
        public readonly Dictionary<string, Dictionary<string, object>> modules = new Dictionary<string, Dictionary<string, object>>();
        public readonly Lex lex = new Lex();
        public readonly Parser parser = new Parser();

        public Dictionary<string, object> DoString(string str)
        {
            var f = Parse(str);
            var ret = InitModule(f);
            return ret;
        }

        public Dictionary<string, object> DoFile(string file)
        {
            return DoString(File.ReadAllText(file));
        }

        public Table Import(string module_name)
        {
            return null;
        }

        public FunctionBody Parse(string str)
        {
            lex.Init(str);
            return parser.Parse(lex);
        }

        public Dictionary<string, object> InitModule(FunctionBody tree)
        {
            Function func = new Function();
            func.vm = this;
            func.module_table = new Dictionary<string, object>();
            func.code = tree;
            func.upvalues = new Dictionary<string, LocalValue>();
            func.Call();
            return func.module_table;
        }
    }
}
