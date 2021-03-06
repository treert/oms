﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class ForStatement : SyntaxTree
    {
#nullable disable
        public ForStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public Token name;
        public ExpSyntaxTree exp1;
        public ExpSyntaxTree exp2;
        public ExpSyntaxTree exp3;
        public BlockTree block;
        protected override void _Exec(Frame frame)
        {
            var start = exp1.GetNumber(frame);
            var end = exp2.GetNumber(frame);
            if (start <= end)
            {
                var step = exp3 ? exp3.GetNumber(frame) : 1;
                if (step <= 0)
                {
                    throw frame.NewRunException(Line, $"for step {step} should greater than 0, or will cause forerver loop");
                }
                var cur_block = frame.CurrentBlock;
                for (MyNumber it = start; it <= end; it += step)
                {
                    frame.CurrentBlock = cur_block;
                    try
                    {
                        var b = frame.EnterBlock();
                        frame.AddLocalVal(name.m_string, it);
                        block.Exec(frame);
                    }
                    catch (ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
                frame.CurrentBlock = cur_block;
            }
            else
            {
                var step = exp3 ? exp3.GetNumber(frame) : -1;
                if (step >= 0)
                {
                    throw frame.NewRunException(Line, $"for step {step} should less than 0, or will cause forerver loop");
                }
                var cur_block = frame.CurrentBlock;
                for (var it = start; it >= end; it += step)
                {
                    frame.CurrentBlock = cur_block;
                    try
                    {
                        var b = frame.EnterBlock();
                        frame.AddLocalVal(name.m_string, it);
                        block.Exec(frame);
                    }
                    catch (ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
                frame.CurrentBlock = cur_block;
            }
        }
    }

    public class ForInStatement : SyntaxTree
    {
#nullable disable
        public ForInStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public NameList name_list;
        public ExpSyntaxTree exp;
        public BlockTree block;

        protected override void _Exec(Frame frame)
        {
            var obj = exp.GetResult(frame);
            if (obj == null) return;// 无事发生，虽然按理应该报个错啥的。

            var cur_block = frame.CurrentBlock;
            if (obj is IForEach iter)
            {
                foreach(var it in iter.GetForEachItor(name_list.names.Count))
                {
                    frame.CurrentBlock = cur_block;
                    try
                    {
                        frame.EnterBlock();
                        name_list.DefineLocalValues(frame, it);
                        block.Exec(frame);
                    }
                    catch (ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
            }
            else if (obj is MyFunction func)
            {
                for (; ; )
                {
                    var results = func.Call();
                    if (results != null)
                    {
                        frame.CurrentBlock = cur_block;
                        try
                        {
                            frame.EnterBlock();
                            name_list.DefineLocalValues(frame, results);
                            block.Exec(frame);
                        }
                        catch (ContineException)
                        {
                            continue;
                        }
                        catch (BreakException)
                        {
                            break;
                        }
                    }
                }
            }
            // 想了想，统一支持下 IEnumerate
            else if (obj is IEnumerable ie)
            {
                foreach (var it in ie)
                {
                    frame.CurrentBlock = cur_block;
                    try
                    {
                        frame.EnterBlock();
                        name_list.DefineLocalValues(frame, it);
                        block.Exec(frame);
                    }
                    catch (ContineException)
                    {
                        continue;
                    }
                    catch (BreakException)
                    {
                        break;
                    }
                }
            }
            else
            {
                throw frame.NewRunException(exp.Line, $"for in does not support type {obj.GetType().FullName}");
            }
            frame.CurrentBlock = cur_block;
        }
    }

    public class ForeverStatement : SyntaxTree
    {
#nullable disable
        public ForeverStatement(int line_, string source)
        {
            Line = line_;
            Source = source;
        }
#nullable restore
        public BlockTree block;

        protected override void _Exec(Frame frame)
        {
            var cur_block = frame.CurrentBlock;
            //int cnt = 0;
            for (; ; )
            {
                //if(cnt++ >= int.MaxValue)
                //{
                //    throw frame.NewRunException(line, "forever loop seens can not ended");
                //}
                frame.CurrentBlock = cur_block;
                try
                {
                    frame.EnterBlock();
                    block.Exec(frame);
                }
                catch (ContineException)
                {
                    continue;
                }
                catch (BreakException)
                {
                    break;
                }
            }
            frame.CurrentBlock = cur_block;
        }
    }
}
