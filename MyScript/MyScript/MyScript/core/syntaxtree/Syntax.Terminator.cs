﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyScript
{
    public class Terminator : ExpSyntaxTree
    {
        public Token token;
        public Terminator(Token token_, string source)
        {
            token = token_;
            Line = token_.m_line;
            Source = source;
        }

        protected override object _GetResults(Frame frame)
        {
            object obj = null;
            if (token.Match(TokenType.NAME))
            {
                obj = frame.Read(token.m_string);
            }
            else if (token.Match(Keyword.NIL))
            {
                obj = null;
            }
            else if (token.Match(Keyword.TRUE))
            {
                obj = true;
            }
            else if (token.Match(Keyword.FALSE))
            {
                obj = false;
            }
            else if (token.Match(TokenType.NUMBER))
            {
                obj = token.m_number;
            }
            else if (token.Match(TokenType.STRING))
            {
                obj = token.m_string;
            }
            else
            {
                Debug.Assert(false);
            }
            return obj;
        }
    }


}
