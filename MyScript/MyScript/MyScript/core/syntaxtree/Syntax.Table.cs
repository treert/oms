﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MyScript
{
    public class TableDefine : ExpSyntaxTree
    {
        public TableDefine(int line_)
        {
            _line = line_;
        }
        public List<TableField> fields = new List<TableField>();

        protected override object _GetResults(Frame frame)
        {
            Table ret = new Table();
            for (var i = 0; i < fields.Count; i++)
            {
                var f = fields[i];

                object key = f.index.GetResult(frame);
                if (key == null)
                {
                    throw frame.NewRunException(f.line, "Table key can not be nil");
                }
                ret.Set(key, f.value.GetResult(frame));
            }
            return ret;
        }
    }

    public class TableField : SyntaxTree
    {
        public TableField(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree index;
        public ExpSyntaxTree value;
    }

    public class TableAccess : ExpSyntaxTree
    {
        public TableAccess(int line_)
        {
            _line = line_;
        }
        public ExpSyntaxTree table;
        public ExpSyntaxTree index;

        protected override object _GetResults(Frame frame)
        {
            var t = table.GetResult(frame);
            var idx = index.GetResult(frame);
            if (idx == null)
            {
                throw frame.NewRunException(index.line, "table index can not be null");
            }
            var ret = ExtUtils.Get(t, idx);
            return ret;
        }

        private object GetResultForAssgin(Frame frame)
        {
            object t = null;
            if(table is TableAccess tmp)
            {
                t = tmp.GetResultForAssgin(frame);
            }
            else
            {
                t = table.GetResult(frame);
            }
            var idx = index.GetResult(frame);
            if (idx == null)
            {
                throw frame.NewRunException(line, "table index can not be null");
            }

            if (t is Table t2)
            {
                var ret = t2.Get(idx);
                if(ret == null)
                {
                    ret = new Table();
                    t2.Set(idx, ret);
                }
                return ret;
            }
            else
            {
                return ExtUtils.Get(t, idx);
            }
        }

        public void Assign(Frame frame, object val)
        {
            // 像PHP一样，针对Table, 做一个优化: a.b.c.d = 1，连着创建2个Table。实际使用时，很方便。
            object t = null;
            if (table is TableAccess tmp)
            {
                t = tmp.GetResultForAssgin(frame);
            }
            else
            {
                t = table.GetResult(frame);
            }
            var idx = index.GetResult(frame);
            if (idx == null)
            {
                throw frame.NewRunException(line, "table index can not be null");
            }
            ExtUtils.Set(t, idx, val);
        }
    }

}
