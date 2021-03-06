﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    // 语法结构树

    class Parser
    {
        static bool IsExpReturnAnyCountValue(SyntaxTree t)
        {
            if(t is Terminator)
            {
                return (t as Terminator).token.m_type == (int)TokenType.DOTS;
            }
            else if(t is FuncCall)
            {
                return true;
            }
            return false;
        }

        static bool IsVar(SyntaxTree t)
        {
            return t is TableAccess || t is Terminator;
        }

        Lex _lex;
        Token _current;
        Token _look_ahead;
        Token _look_ahead2;
        Token NextToken()
        {
            if(_look_ahead != null)
            {
                _current = _look_ahead;
                _look_ahead = _look_ahead2;
                _look_ahead2 = null;
            }
            else
            {
                _current = _lex.GetNextToken();
            }
            return _current;
        }
        Token LookAhead()
        {
            if (_look_ahead == null)
                _look_ahead = _lex.GetNextToken();
            return _look_ahead;
        }
        Token LookAhead2()
        {
            LookAhead();
            if (_look_ahead2 == null)
                _look_ahead2 = _lex.GetNextToken();
            return _look_ahead2;
        }
        bool IsMainExp()
        {
            int token_type = LookAhead().m_type;
            return
                token_type == (int)TokenType.NIL ||
                token_type == (int)TokenType.FALSE ||
                token_type == (int)TokenType.TRUE ||
                token_type == (int)TokenType.NUMBER ||
                token_type == (int)TokenType.STRING ||
                token_type == (int)TokenType.DOTS ||
                token_type == (int)TokenType.FUNCTION ||
                token_type == (int)TokenType.NAME ||
                token_type == (int)'(' ||
                token_type == (int)'{' ||
                token_type == (int)'-' ||
                token_type == (int)'#' ||
                token_type == (int)TokenType.NOT;
        }
        int GetOpPriority(Token t)
        {
            switch(t.m_type)
            {
                case (int)'^': return 100;
                case (int)'*':
                case (int)'/':
                case (int)'%': return 80;
                case (int)'+':
                case (int)'-': return 70;
                case (int)TokenType.CONCAT: return 60;
                case (int)'>':
                case (int)'<':
                case (int)TokenType.GE:
                case (int)TokenType.LE:
                case (int)TokenType.NE:
                case (int)TokenType.EQ: return 50;
                case (int)TokenType.AND: return 40;
                case (int)TokenType.OR: return 30;
                default: return 0;
            }
        }
        bool IsRightAssociation(Token t)
        {
            return t.m_type == (int)'^';
        }

        SyntaxTree ParseExp(int left_priority = 0)
        {
            var exp = ParseMainExp();
            while(true)
            {
                int right_priority = GetOpPriority(LookAhead());
                if (left_priority < right_priority ||(left_priority == right_priority && IsRightAssociation(LookAhead())))
                {
                    // C++的函数参数执行顺序没有明确定义，方便起见，不在函数参数里搞出两个有依赖的函数调用，方便往C++里迁移
                    var op = NextToken();
                    exp = new BinaryExpression(exp, op, ParseExp(right_priority));
                }
                else
                {
                    return exp;
                }
            }
        }

        SyntaxTree ParseMainExp()
        {
            SyntaxTree exp;
            switch(LookAhead().m_type)
            {
                case (int)TokenType.NIL:
                case (int)TokenType.FALSE:
                case (int)TokenType.TRUE:
                case (int)TokenType.NUMBER:
                case (int)TokenType.STRING:
                case (int)TokenType.DOTS:
                    exp = new Terminator(NextToken());
                    break;
                case (int)TokenType.FUNCTION:
                    exp = ParseFunctionDef();
                    break;
                case (int)TokenType.NAME:
                case (int)'(':
                    exp = ParsePrefixExp();
                    break;
                case (int)'{':
                    exp = ParseTableConstructor();
                    break;
                // unop exp priority is 90 less then ^
                case (int)'-':
                case (int)'#':
                case (int)TokenType.NOT:
                    var unexp = new UnaryExpression(LookAhead().m_line);
                    unexp.op = NextToken();
                    unexp.exp = ParseExp(90);
                    exp = unexp;
                    break;
                default:
                    throw NewParserException("unexpect token for main exp", _look_ahead);
            }
            return exp;
        }
        ExpressionList ParseExpList(bool is_args = false)
        {
            var exp = new ExpressionList(LookAhead().m_line);
            exp.exp_list.Add(ParseExp());
            while(LookAhead().m_type == (int)',')
            {
                NextToken();
                if (is_args && LookAhead().m_type == (int)')')
                {
                    break;// func call args can have a extra ","
                }
                exp.exp_list.Add(ParseExp());
            }
            exp.return_any_value = IsExpReturnAnyCountValue(exp.exp_list[exp.exp_list.Count - 1]);
            return exp;
        }
        FunctionBody ParseFunctionDef()
        {
            NextToken();
            return ParseFunctionBody();
        }
        TableAccess ParseTableAccessor(SyntaxTree table)
        {
            NextToken();// skip '[' or '.'

            var index_access = new TableAccess(_current.m_line);
            index_access.table = table;
            if(_current.m_type == (int)'[')
            {
                index_access.index = ParseExp();
                if (NextToken().m_type != (int)']')
                    throw NewParserException("expect ']'", _current);
            }
            else
            {
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw NewParserException("expect 'id' after '.'", _current);
                index_access.index = new Terminator(new Token(_current.m_string));
            }
            return index_access;
        }
        FuncCall ParseFunctionCall(SyntaxTree caller)
        {
            var func_call = new FuncCall(LookAhead().m_line);
            func_call.caller = caller;
            func_call.args = ParseArgs();
            return func_call;
        }
        ExpressionList ParseArgs()
        {
            ExpressionList exp_list = null;
            if(LookAhead().m_type == (int)'(')
            {
                NextToken();
                if (LookAhead().m_type != (int)')')
                    exp_list = ParseExpList(true);

                if (NextToken().m_type != (int)')')
                    throw NewParserException("expect ')' to end function-args", _current);
            }
            else if(LookAhead().m_type == (int)'{')
            {
                exp_list = new ExpressionList(LookAhead().m_line);
                exp_list.exp_list.Add(ParseTableConstructor());
            }
            else
                throw NewParserException("expect '(' or '{' to start function-args", _look_ahead);
            return exp_list;
        }
        SyntaxTree ParsePrefixExp()
        {
            NextToken();
            Debug.Assert(_current.m_type == (int)TokenType.NAME
                || _current.m_type == (int)'(');
            SyntaxTree exp;
            if(_current.m_type == (int)'(')
            {
                exp = ParseExp();
                if (NextToken().m_type != (int)')')
                    throw NewParserException("expect ')'", _current);
            }
            else
            {
                exp = new Terminator(_current);
            }

            // table index or func call
            for(;;)
            {
                if(LookAhead().m_type == (int)'['
                    || LookAhead().m_type == (int)'.')
                {
                    exp = ParseTableAccessor(exp);
                }
                else if(LookAhead().m_type == (int)'('
                    || LookAhead().m_type == (int)'{')
                {
                    exp = ParseFunctionCall(exp);
                }
                else
                {
                    break;
                }
            }
            return exp;
        }
        SyntaxTree ParseOtherStatement()
        {
            // lua做了限制，其他语句只有两种，assign statement and func call
            // SS还增加几个语法支持，+=，-=，++，--
            SyntaxTree exp;
            if (LookAhead().m_type == (int)TokenType.NAME)
            {
                exp = ParsePrefixExp();
                if(IsVar(exp))
                {
                    if (LookAhead().m_type == (int)TokenType.ADD_ONE)
                    {
                        // ++
                        NextToken();
                        var special_statement = new SpecialAssginStatement(_current.m_line);
                        special_statement.var = exp;
                        special_statement.is_add_op = true;
                        return special_statement;
                    }
                    else if (LookAhead().m_type == (int)TokenType.ADD_SELF)
                    {
                        // +=
                        NextToken();
                        var special_statement = new SpecialAssginStatement(_current.m_line);
                        special_statement.var = exp;
                        special_statement.exp = ParseExp();
                        special_statement.is_add_op = true;
                        return special_statement;
                    }
                    else if (LookAhead().m_type == (int)TokenType.DEC_ONE)
                    {
                        // --
                        NextToken();
                        var special_statement = new SpecialAssginStatement(_current.m_line);
                        special_statement.var = exp;
                        special_statement.is_add_op = false;
                        return special_statement;
                    }
                    else if (LookAhead().m_type == (int)TokenType.DEC_SELF)
                    {
                        // -=
                        NextToken();
                        var special_statement = new SpecialAssginStatement(_current.m_line);
                        special_statement.var = exp;
                        special_statement.exp = ParseExp();
                        special_statement.is_add_op = false;
                        return special_statement;
                    }

                    // assign statement
                    var assign_statement = new AssignStatement(LookAhead().m_line);
                    assign_statement.var_list.Add(exp);
                    while(LookAhead().m_type != (int)'=')
                    {
                        if (NextToken().m_type != (int)',')
                            throw NewParserException("expect ',' to split var-list", _current);
                        if (LookAhead().m_type != (int)TokenType.NAME)
                            throw NewParserException("expect 'id' to start var", _look_ahead);
                        exp = ParsePrefixExp();
                        if (!IsVar(exp))
                            throw NewParserException("expect var here", _current);
                        assign_statement.var_list.Add(exp);
                    }
                    NextToken();// skip '='
                    assign_statement.exp_list = ParseExpList();

                    return assign_statement;
                }
                else
                {
                    Debug.Assert(exp is FuncCall);
                    return exp;
                }
            }
            else
            {
                if (IsMainExp())
                    throw NewParserException("unsupport statement", _look_ahead);
                return null;
            }
        }
        TableField ParseTableIndexField()
        {
            NextToken();
            var field = new TableField(_current.m_line);
            field.index = ParseExp();
            if (NextToken().m_type != ']')
                throw NewParserException("expect ']'", _current);
            if (NextToken().m_type != '=')
                throw NewParserException("expect '='", _current);
            field.value = ParseExp();
            return field;
        }
        TableField ParseTableNameField()
        {
            var field = new TableField(LookAhead().m_line);
            field.index = new Terminator(new Token(NextToken().m_string));
            NextToken();// skip '='
            field.value = ParseExp();
            return field;
        }
        TableField ParseTableArrayField()
        {
            var field = new TableField(LookAhead().m_line);
            field.index = null;// default is null
            field.value = ParseExp();
            return field;
        }
        TableDefine ParseTableConstructor()
        {
            NextToken();
            var table = new TableDefine(_current.m_line);
            TableField last_field = null;
            while(LookAhead().m_type != '}')
            {
                if (LookAhead().m_type == (int)'[')
                    last_field = ParseTableIndexField();
                else if(LookAhead().m_type == (int)TokenType.NAME
                    && LookAhead2().m_type == (int)'=')
                    last_field = ParseTableNameField();
                else
                    last_field = ParseTableArrayField();

                table.fields.Add(last_field);

                if(LookAhead().m_type != '}')
                {
                    NextToken();
                    if(_current.m_type != (int)','
                        && _current.m_type != (int)';')
                        throw NewParserException("expect ',' or ';' to split table fields", _current);
                }
            }
            if (NextToken().m_type != '}')
                throw NewParserException("expect '}' for table", _current);

            if(last_field != null && last_field.index == null)
            {
                table.last_field_append_table = IsExpReturnAnyCountValue(last_field.value);
            }

            return table;
        }
        Chunk ParseChunk()
        {
            var block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.EOS)
                throw NewParserException("expect <eof>", _current);
            var tree = new Chunk();
            tree.block = block;
            return tree;
        }
        Block ParseBlock()
        {
            var block = new Block(LookAhead().m_line);
            for (; ; )
            {
                SyntaxTree statement = null;
                var token_ahead = LookAhead();
                switch (token_ahead.m_type)
                {
                    case (int)';':
                        NextToken(); continue;
                    case (int)TokenType.DO:
                        statement = ParseDoStatement(); break;
                    case (int)TokenType.WHILE:
                        statement = ParseWhileStatement(); break;
                    case (int)TokenType.IF:
                        statement = ParseIfStatement(); break;
                    case (int)TokenType.FOR:
                        statement = ParseForStatement(); break;
                    case (int)TokenType.FOREACH:
                        statement = ParseForEachStatement(); break;
                    case (int)TokenType.FUNCTION:
                        statement = ParseFunctionStatement(); break;
                    case (int)TokenType.LOCAL:
                        statement = ParseLocalStatement(); break;
                    case (int)TokenType.RETURN:
                        statement = ParseReturnStatement(); break;
                    case (int)TokenType.BREAK:
                        statement = ParseBreakStatement(); break;
                    case (int)TokenType.CONTINUE:
                        statement = ParseContinueStatement(); break;
                    case (int)TokenType.ASYNC:
                        statement = ParseAsyncStatement(); break;
                    case (int)TokenType.GOTO:
                        throw NewParserException("'goto' is reserve key word", _look_ahead);
                    case (int)TokenType.ECHO:
                        throw NewParserException("'echo' is reserve key word", _look_ahead);
                    case (int)TokenType.REPEAT:
                        throw NewParserException("'repeat' is reserve key word", _look_ahead);
                    case (int)TokenType.UNTIL:
                        throw NewParserException("'until' is reserve key word", _look_ahead);
                    default:
                        statement = ParseOtherStatement();
                        break;
                }
                if (statement == null)
                    break;
                // lua will check {return,break}
                block.statements.Add(statement);
            }
            return block;
        }
        ReturnStatement ParseReturnStatement()
        {
            NextToken();
            var statement = new ReturnStatement(_current.m_line);
            if(IsMainExp())
            {
                statement.exp_list = ParseExpList();
            }
            return statement;
        }
        BreakStatement ParseBreakStatement()
        {
            NextToken();
            return new BreakStatement(_current.m_line);
        }
        ContinueStatement ParseContinueStatement()
        {
            NextToken();
            return new ContinueStatement(_current.m_line);
        }

        AsyncCall ParseAsyncStatement()
        {
            NextToken();
            var statement = new AsyncCall(_current.m_line);
            if(LookAhead().m_type == (int)TokenType.DO)
            {
                var func = new FunctionBody(_current.m_line);
                NextToken();
                func.block = ParseBlock();
                if (NextToken().m_type != (int)TokenType.END)
                    throw NewParserException("expect 'end' after async function-body", _current);
                statement.caller = func;
            }
            else
            {
                var tmp_token = LookAhead();
                // must be funccall
                SyntaxTree tmp = ParseOtherStatement();
                if(tmp is FuncCall)
                {
                    var t = tmp as FuncCall;
                    statement.caller = t.caller;
                    statement.args = t.args;
                }
                else
                {
                    throw NewParserException("expect 'func call' after async", tmp_token);
                }
            }
            return statement;
        }

        DoStatement ParseDoStatement()
        {
            NextToken();// skip 'do'

            var do_statement = new DoStatement(_current.m_line);
            do_statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw NewParserException("expect 'end' for do-statement", _current);
            return do_statement;
        }
        WhileStatement ParseWhileStatement()
        {
            NextToken();// skip 'while'
            var statement = new WhileStatement(_current.m_line);

            var exp = ParseExp();
            if (NextToken().m_type != (int)TokenType.DO)
                throw NewParserException("expect 'do' for while-statement", _current);

            var block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw NewParserException("expect 'end' for while-statement", _current);

            statement.exp = exp;
            statement.block = block;
            return statement;
        }
        IfStatement ParseIfStatement()
        {
            NextToken();// skip 'if' or 'elseif'
            var statement = new IfStatement(_current.m_line);

            var exp = ParseExp();
            if (NextToken().m_type != (int)TokenType.THEN)
                throw NewParserException("expect 'then' for if-statement", _current);

            var true_branch = ParseBlock();
            var false_branch = ParseFalseBranchStatement();

            statement.exp = exp;
            statement.true_branch = true_branch;
            statement.false_branch = false_branch;
            return statement;
        }
        SyntaxTree ParseFalseBranchStatement()
        {
            if (LookAhead().m_type == (int)TokenType.ELSEIF)
            {
                // syntax sugar for elseif
                return ParseIfStatement();
            }
            else if (LookAhead().m_type == (int)TokenType.ELSE)
            {
                NextToken();
                var block = ParseBlock();
                if (NextToken().m_type != (int)TokenType.END)
                    throw NewParserException("expect 'end' for else-statement", _current);
                return block;
            }
            else if (LookAhead().m_type == (int)TokenType.END)
            {
                NextToken();
                return null;
            }
            else
                throw NewParserException("expect 'end' for if-statement", _look_ahead);
        }
        FunctionStatement ParseFunctionStatement()
        {
            NextToken();// skip 'function'

            var statement = new FunctionStatement(_current.m_line);
            statement.func_name = ParseFunctionName();
            statement.func_body = ParseFunctionBody();
            return statement;
        }
        FunctionName ParseFunctionName()
        {
            if (NextToken().m_type != (int)TokenType.NAME)
                throw NewParserException("expect 'id' after 'function'", _current);
            
            var func_name = new FunctionName(_current.m_line);
            func_name.names.Add(_current);
            while(LookAhead().m_type == (int)'.')
            {
                NextToken();
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw NewParserException("unexpect token in function name after '.'", _current);
                func_name.names.Add(_current);
            }

            return func_name;
        }
        FunctionBody ParseFunctionBody()
        {
            if (NextToken().m_type != (int)'(')
                throw NewParserException("expect '(' to start function-body", _current);
            var statement = new FunctionBody(_current.m_line);
            statement.param_list = ParseParamList();
            if (NextToken().m_type != (int)')')
                throw NewParserException("expect ')' after param-list", _current);
            statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw NewParserException("expect 'end' after function-body", _current);
            
            return statement;
        }
        ParamList ParseParamList()
        {
            var statement = new ParamList(LookAhead().m_line);
            statement.name_list.Add(new Token(OmsConf.MAGIC_THIS));

            // special func(a,b,c,) is OK
            while (LookAhead().m_type == (int)TokenType.NAME)
            {
                statement.name_list.Add(NextToken());
                if(LookAhead().m_type == (int)',')
                {
                    NextToken();
                }
                else
                {
                    break;
                }
            }

            if (LookAhead().m_type == (int)')')
            {
                return statement;
            }
            else if(LookAhead().m_type == (int)TokenType.DOTS)
            {
                NextToken();
                statement.is_var_arg = true;
            }
            else
                throw NewParserException("unexpect token at param-list end", _look_ahead);

            return statement;
        }
        SyntaxTree ParseForStatement()
        {
            NextToken();// skip 'for'

            if (LookAhead().m_type != (int)TokenType.NAME)
                throw NewParserException("expect 'id' after 'for'", _look_ahead);
            if (LookAhead2().m_type == (int)'=')
                return ParseForNumStatement();
            else
                return ParseForInStatement();
        }
        ForStatement ParseForNumStatement()
        {
            var statement = new ForStatement(_current.m_line);
            var name = NextToken();
            Debug.Assert(_current.m_type == (int)TokenType.NAME);
            NextToken();// skip '='
            Debug.Assert(_current.m_type == (int)'=');

            statement.name = name;
            statement.exp1 = ParseExp();
            if (NextToken().m_type != (int)',')
                throw NewParserException("expect ',' in for-statement", _current);
            statement.exp2 = ParseExp();
            if (LookAhead().m_type == ',')
            {
                NextToken();
                statement.exp3 = ParseExp();
            }

            if (NextToken().m_type != (int)TokenType.DO)
                throw NewParserException("expect 'do' to start for-body", _current);
            statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw NewParserException("expect 'end' to complete for-body", _current);

            return statement;
        }
        ForInStatement ParseForInStatement()
        {
            var statement = new ForInStatement(_current.m_line);
            statement.name_list = ParseNameList();
            if (NextToken().m_type != (int)TokenType.IN)
                throw NewParserException("expect 'in' in for-in-statement", _current);
            // 这个结构特殊，返回的是迭代器，
            statement.exp_list = ParseExpList();

            if (NextToken().m_type != (int)TokenType.DO)
                throw NewParserException("expect 'do' to start for-in-body", _current);
            statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw NewParserException("expect 'end' to complete for-in-body", _current);

            return statement;
        }
        ForEachStatement ParseForEachStatement()
        {
            NextToken();// skip 'foreach'

            var statement = new ForEachStatement(_current.m_line);
            if(NextToken().m_type != (int)TokenType.NAME)
                throw NewParserException("expect 'id' in foreach-statement", _current);
            if(LookAhead().m_type == (int)',')
            {
                statement.k = _current;
                NextToken();
                if(NextToken().m_type != (int)TokenType.NAME)
                    throw NewParserException("expect 'id' in foreach-statement after ','", _current);
                statement.v = _current;
            }
            else
            {
                statement.v = _current;
            }

            if (NextToken().m_type != (int)TokenType.IN)
                throw NewParserException("expect 'in' in foreach-statement", _current);
            statement.exp = ParseExp();

            if (NextToken().m_type != (int)TokenType.DO)
                throw NewParserException("expect 'do' to start foreach-body", _current);
            statement.block = ParseBlock();
            if (NextToken().m_type != (int)TokenType.END)
                throw NewParserException("expect 'end' to complete foreach-body", _current);

            return statement;
        }
        SyntaxTree ParseLocalStatement()
        {
            NextToken();// skip 'local'

            if (LookAhead().m_type == (int)TokenType.FUNCTION)
                return ParseLocalFunction();
            else if (LookAhead().m_type == (int)TokenType.NAME)
                return ParseLocalNameList();
            else
                throw NewParserException("unexpect token after 'local'", _look_ahead);
        }
        LocalFunctionStatement ParseLocalFunction()
        {
            NextToken();// skip 'function'
            var statement = new LocalFunctionStatement(_current.m_line);

            if (NextToken().m_type != (int)TokenType.NAME)
                throw NewParserException("expect 'id' after 'local function'", _current);

            statement.name = _current;
            statement.func_body = ParseFunctionBody();
            return statement;
        }
        LocalNameListStatement ParseLocalNameList()
        {
            var statement = new LocalNameListStatement(_current.m_line);
            statement.name_list = ParseNameList();
            if(LookAhead().m_type == (int)'=')
            {
                NextToken();
                statement.exp_list = ParseExpList();
            }
            return statement;
        }
        NameList ParseNameList()
        {
            var statement = new NameList(LookAhead().m_line);
            statement.names.Add(NextToken());
            Debug.Assert(_current.m_type == (int)TokenType.NAME);
            while(LookAhead().m_type == ',')
            {
                NextToken();
                if (NextToken().m_type != (int)TokenType.NAME)
                    throw NewParserException("expect 'id' after ','", _current);
                statement.names.Add(_current);
            }
            return statement;
        }
        private ParserException NewParserException(string msg, Token token)
        {
            Debug.Assert(token != null);
            return new ParserException(_lex.GetSourceName(), token.m_line, token.m_column, msg);
        }

        public SyntaxTree Parse(Lex lex_)
        {
            _lex = lex_;
            _current = null;
            _look_ahead = null;
            _look_ahead2 = null;
            return ParseChunk();
        }
    }
}
