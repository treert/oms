﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    public static class LibBase
    {
        public static int Print(Thread th)
        {
            int arg_count = th.GetStatckSize();

            for (int i = 0; i < arg_count; ++i)
            {
                object obj = th.GetValue(i);
                if (obj == null)
                    Console.Write("nil");
                else if (obj is bool)
                    Console.Write("{0}", obj);
                else if (obj is double)
                    Console.Write("{0}", obj);
                else if (obj is string)
                    Console.Write(obj);
                else
                    Console.Write("{0}:{1}", obj.GetType().Name, obj);

                if (i != arg_count - 1)
                {
                    Console.Write("\t");
                }
            }
            Console.WriteLine();
            return 0;
        }

        public static int XModule(Thread th)
        {
            string name = th.GetValue(0) as string;
            if(name != null)
            {
                // todo 没有出错兼容，要完善也不用一啊
                var segments = name.Split('.');
                var table = th.VM.m_global;
                var vm = th.VM;
                for (int i = 0; i < segments.Length; ++i)
                {
                    Table tmp = table.GetValue(segments[i]) as Table;
                    if(tmp == null)
                    {
                        tmp = vm.NewTable();
                        table.SetValue(segments[i], tmp);
                    }
                    table = tmp;
                }
                th.SetModuleEnv(name, table);
            }
            return 0;
        }

        public static void Register(VM vm)
        {
            vm.RegisterGlobalFunc("print", Print);
            vm.RegisterGlobalFunc("module", XModule);
        }
    }
}
