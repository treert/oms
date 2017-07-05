﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    /// <summary>
    /// 脚本虚拟机
    /// 1. 资源管理
    ///     1. 全局表
    ///     2. gc管理，new管理【现在完全没管这个】
    /// 2. 线程管理
    /// 3. 对外接口
    ///     1. DoString
    ///     2. DoFile
    ///     3. CompileString
    ///     4. CompileFile
    ///     5. CallClosure
    /// </summary>
    public class VM
    {
        //**************** do ********************************/
        public void DoString(string s, String module_name = "")
        {
            var func = Parse(s, module_name);
            CallFunction(func);
        }

        public void DoFile(string file_name)
        {
            using (FileStream stream = new FileStream(file_name, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Function func = null;
                if (ReadBom(stream))
                {
                    // utf-8 bom source
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var source = reader.ReadToEnd();
                        func = Parse(source, file_name);
                    }
                }
                else
                {
                    // compiled binary
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        func = Function.Deserialize(reader);
                    }
                }
                CallFunction(func);
            }
        }

        //**************** call ********************************/
        public object[] CallClosure(Closure closure, params object[] args)
        {
            var work_thread = GetWorkThread();

            work_thread.PushValue(closure);
            for (int i = 0; i < args.Length; ++i )
            {
                work_thread.PushValue(args[i]);
            }
            work_thread.Run();

            // get results
            int count = work_thread.GetTopIdx();
            object[] ret = new object[count];
            for (int i = 0; i < count; ++i )
            {
                ret[i] = work_thread.GetValue(i);
            }
            work_thread.Clear();

            PutWorkThread(work_thread);
            return ret;
        }

        private object[] CallFunction(Function func, params object[] args)
        {
            var closure = NewClosure();
            closure.func = func;
            closure.env_table = m_global;

            return CallClosure(closure, args);
        }

        //**************** compile *****************************/
        public void ComileFile(string src_file, string out_file = "")
        {
            if (src_file == out_file)
            {
                throw new OtherException("out file is same as src file, file {0}", src_file);
            }
            if (string.IsNullOrEmpty(out_file))
            {
                out_file = src_file + "c";
            }

            using(FileStream src_stream = new FileStream(src_file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (StreamReader reader = new StreamReader(src_stream))
            {
                if (ReadBom(src_stream) == false)
                {
                    throw new OtherException("file {0} has compiled", src_file);
                }
                using (FileStream out_stream = new FileStream(out_file, FileMode.Create))
                {
                    var source = reader.ReadToEnd();
                    CompileString(source, out_stream, src_file);
                }
            }
        }

        public void CompileString(string source, Stream out_stream, string module_name = "")
        {
            var func = Parse(source, module_name);
            out_stream.WriteByte(_header[0]);
            out_stream.WriteByte(_header[1]);
            out_stream.WriteByte(_header[2]);
            using (BinaryWriter writer = new BinaryWriter(out_stream))
            {
                func.Serialize(writer);
            }
        }

        bool ReadBom(Stream src_stream)
        {
            byte[] bom = new byte[3];
            src_stream.Read(bom, 0, 3);
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                // utf-8 bom source
                return true;
            }
            else if (bom[0] == _header[0] && bom[1] == _header[1] && bom[2] == _header[2])
            {
                // binary source
                return false;
            }
            else
            {
                throw new Exception("Only support compile binary source or utf-8 bom source");
            }
        }
        //**************** parse *******************************/
        private Function Parse(string source, string module_name)
        {
            _lex.Init(source, module_name);
            var tree = _parser.Parse(_lex);
            var func = _code_generator.Generate(tree);
            return func;
        }

        private Function Parse(Stream stream, string module_name)
        {
            if(ReadBom(stream))
            {
                // utf-8 bom source
                using (StreamReader reader = new StreamReader(stream))
                {
                    var source = reader.ReadToEnd();
                    return Parse(source, module_name);
                }
            }
            else
            {
                // compiled binary
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    return Function.Deserialize(reader);
                }
            }
        }

        //**************** global table *****************************/
        public Table m_global;

        public void RegisterGlobalFunc(string name, CFunction cfunc)
        {
            m_global.SetValue(name,cfunc);
        }

        /************** some new manager **********************************/
        public Table NewTable()
        {
            return new Table();
        }
        internal Closure NewClosure()
        {
            return new Closure();
        }
        /*****************************************************************/

        Lex _lex;
        Parser _parser;
        CodeGenerate _code_generator;
        Thread _thread;
        Stack<Thread> _other_threads;
        byte[] _header = new byte[3] { 0, (byte)'s', (byte)'s' };

        Thread GetWorkThread()
        {
            if(!_thread.IsRuning())
            {
                return _thread;
            }
            // 这样可以兼容宿主协程，因为不存在执行栈帧来回穿插的情况
            if(_other_threads.Count == 0)
            {
                return new Thread(this);
            }
            else
            {
                return _other_threads.Pop();
            }
        }

        void PutWorkThread(Thread th)
        {
            if(th != _thread)
            {
                _other_threads.Push(th);
            }
        }

        public VM()
        {
            _lex = new Lex();
            _parser = new Parser();
            _code_generator = new CodeGenerate();
            _thread = new Thread(this);
            _other_threads = new Stack<Thread>();

            m_global = NewTable();
        }
    }
}
