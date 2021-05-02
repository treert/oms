﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;

/// <summary>
/// 人力有穷时，简单化吧，虽然想加好多语法糖。
/// 1. ms 语言层面只支持非常少的类型：
/// </summary>
namespace MyScript
{
    public interface IGetSet
    {
        object? Get(object key);
        bool Set(object key, object? val);
    }

    public interface ICall
    {
        object? Call(Args args);
        public static ICall Create(Func<Args, List<object>> func) => new CallWrap(func);

        public static ICall Create(Func<Args, object> func) => new CallWrap2(func);

        private class CallWrap : ICall
        {
            Func<Args, List<object>> func;
            public CallWrap(Func<Args, List<object>> func)
            {
                this.func = func;
            }
            public object Call(Args args)
            {
                return func(args);
            }
        }
        private class CallWrap2 : ICall
        {
            Func<Args, object> func;
            public CallWrap2(Func<Args, object> func)
            {
                this.func = func;
            }
            public object Call(Args args)
            {
                return func(args);
            }
        }
    }

    public interface IForEach
    {
        /// <summary>
        /// 支持 for in 语法，效率有点低哟，就这么着吧
        /// </summary>
        /// <param name="expect_cnt">expect_cnt 是为了能实现 for v in table {}, for k,v in table {}</param>
        /// <returns></returns>
        IEnumerable<object?> GetForEachItor(int expect_cnt = -1);
    }

    /// <summary>
    /// 运行时函数
    /// </summary>
    public class Function: ICall
    {
        public VM vm;// 保存住来自哪个vm，外部调用就方便好多了。
        public FunctionBody code;
        public Table module_table;
        // 环境闭包值，比较特殊的是：当Value == null，指这个变量是全局变量。
        public Dictionary<string, LocalValue?> upvalues;
        // 默认参数。有副作用，这些obj是常驻内存的，有可能被修改。
        public Dictionary<string, object> default_args = new Dictionary<string, object>();

        public object Call(params object[] objs)
        {
            Args args = new Args(objs);
            return Call(args);
        }

        public object Call(Dictionary<string, object> name_args, params object[] objs)
        {
            Args args = new Args(name_args, objs);
            return Call(args);
        }

        public object Call()
        {
            Args args = new Args();
            return Call(args);
        }

        public object Call(Args args)
        {
            Frame frame = new Frame(this);
            // 先填充个this
            frame.AddLocalVal(Utils.MAGIC_THIS, args.that);

            int name_cnt = 0;
            if (code.param_list is not null)
            {
                name_cnt = code.param_list.name_list.Count;
                for (int i = 0; i < name_cnt; i++)
                {
                    var name = code.param_list.name_list[i].token.m_string;
                    object obj = null;
                    if (args.TryGetValue(i, name, out obj))
                    {
                    }
                    else if(default_args != null && default_args.TryGetValue(name, out obj))
                    {
                    }
                    frame.AddLocalVal(name, obj);
                }

                if (code.param_list.kw_name)
                {
                    frame.AddLocalVal(code.param_list.kw_name.m_string, args);// 直接获取所有参数好了
                }
            }
            try
            {
                code.block.Exec(frame);
            }
            catch(ReturnException ep)
            {
                return ep.result;
            }
            catch(ContineException ep)
            {
                throw new RunException(code.source_name, ep.line, "unexpect contine");
            }
            catch(BreakException ep)
            {
                throw new RunException(code.source_name, ep.line, "unexpect break");
            }
            // 其他的异常就透传出去好了。
            return null;
        }
    }

    /// <summary>
    /// 统一的参数格式
    /// </summary>
    public class Args:IGetSet
    {
        public object? that = null;// this
        public Dictionary<string, object> name_args;
        public List<object> args;
        public Frame? frame = null;// VM 调用外部接口时，通过这个可以传递运行是环境，增加功能

        public Args()
        {
            name_args = new Dictionary<string, object>();
            args = new List<object>();
        }

        public Args(Frame frame): this()
        {
            this.frame = frame;
        }

        public Args(params object[] args)
        {
            name_args = new Dictionary<string, object>();
            this.args = new List<object>(args);
        }

        public Args(Dictionary<string, object> name_args, params object[] args)
        {
            this.name_args = name_args;
            this.args = new List<object>(args);
        }

        public bool TryGetValue(int idx, string name, out object ret)
        {
            if(name_args.TryGetValue(name, out ret))
            {
                return true;// 优先级高于数组参数
            }
            else if (idx >= 0 && idx < args.Count)
            {
                ret = args[idx];
                return true;
            }

            ret = null;
            return false;
        }

        public object this[int idx]
        {
            get
            {
                if(idx >= 0 && idx < args.Count)
                {
                    return args[idx];
                }
                return null;
            }
        }

        public object this[string name]
        {
            get
            {
                object ret;
                name_args.TryGetValue(name, out ret);
                return ret;
            }
        }

        public object? Get(object? key)
        {
            if(key is string str)
            {
                return this[str];
            }
            else if(key is int idx)
            {
                return this[idx];
            }
            return null;
        }

        public bool Set(object key, object val)
        {
            throw new Exception("can not modify Args");
        }
    }



    public class LocalValue
    {
        public object? obj;
    }
}
