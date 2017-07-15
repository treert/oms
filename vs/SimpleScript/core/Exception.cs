﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    class BaseException:Exception
    {
        private string _info = string.Empty;
        public override string Message
        {
            get
            {
                return _info;
            }
        }

        protected void SetInfo(params object[] args)
        {
            var string_build = new StringBuilder();
            foreach(var obj in args)
            {
                string_build.Append(obj);
            }
            _info = string_build.ToString();
        }
    }
    class LexException:BaseException
    {
        public LexException(string source_, int line_, int column_,string msg)
        {
            SetInfo(source_, ":", line_, ":", column_, " ",msg);
        }
    }

    class ParserException:BaseException
    {
        public ParserException(string source_, int line_, int column_, string msg)
        {
            SetInfo(source_, ":", line_, ":", column_, " ", msg);
        }
    }

    class CodeGenerateException : BaseException
    {
        public CodeGenerateException(string source_, int line_, string msg)
        {
            SetInfo(source_, ":", line_, " ", msg);
        }
    }

    class RuntimeException : BaseException
    {
        public RuntimeException(string msg)
        {
            SetInfo(msg);
        }
        public RuntimeException(string file, int line, string msg)
        {
            SetInfo(file, ":", line, " ", msg);
        }
    }

    class OtherException : Exception
    {
        private string _info = string.Empty;
        public override string Message
        {
            get
            {
                return _info;
            }
        }

        public OtherException(string format, params object[] args)
        {
            _info = string.Format(format, args);
        }
    }
}
