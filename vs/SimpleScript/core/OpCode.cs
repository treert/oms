﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScript
{
    /*
    code: int32_t
    A   : uint8_t
    B   : uint8_t
    C   : uint8_t
    Bx  : int16_t (B+C)
    */
    enum OpType
    {
        OpType_InValid = 0,
        OpType_LoadNil,                 // A    R(A) := nil
        OpType_LoadBool,                // AB   R(A) := (B == 1)
        OpType_LoadInt,                 // ABx  R(A) := Bx
        OpType_LoadConst,               // ABx  R(A) := Const(Bx)
        OpType_LoadFunc,                // ABx  R(A) := ChildFunc(Bx)
        OpType_Move,                    // AB   R(A) := R(B)
        OpType_GetGlobal,               // ABx  R(A) := Global(Bx)
        OpType_SetGlobal,               // ABx  Global(Bx) := R(A)
        OpType_Call,                    // ABC  R(A)..R(top-1) := Call(R(A),B:fix arg count,C==1:any arg to top)
        OpType_VarArg,                  // A    R(A)..R(top-1) := ...
        OpType_Ret,                     // ABC  return C!=1 ? R(A)..R(B) : R(A)..R(top-1)
        OpType_JmpFalse,                // ABx  if not R(A) then pc += Bx
        OpType_JmpTrue,                 // ABx  if R(A) then pc += Bx
        OpType_JmpNil,                  // ABx  if R(A) == nil then pc += Bx
        OpType_Jmp,                     // Bx   pc += Bx
        OpType_Neg,                     // A    R(A) = -R(A)
        OpType_Not,                     // A    R(A) = not R(A)
        OpType_Len,                     // A    R(A) = #R(A)
        OpType_Add,                     // ABC  R(A) = R(B) + R(C)
        OpType_Sub,                     // ABC  R(A) = R(B) - R(C)
        OpType_Mul,                     // ABC  R(A) = R(B) * R(C)
        OpType_Div,                     // ABC  R(A) = R(B) / R(C)
        OpType_Pow,                     // ABC  R(A) = R(B) ^ R(C)
        OpType_Mod,                     // ABC  R(A) = R(B) % R(C)
        OpType_Concat,                  // ABC  R(A) = R(B) .. R(C)
        OpType_Less,                    // ABC  R(A) = R(B) < R(C)
        OpType_Greater,                 // ABC  R(A) = R(B) > R(C)
        OpType_Equal,                   // ABC  R(A) = R(B) == R(C)
        OpType_UnEqual,                 // ABC  R(A) = R(B) ~= R(C)
        OpType_LessEqual,               // ABC  R(A) = R(B) <= R(C)
        OpType_GreaterEqual,            // ABC  R(A) = R(B) >= R(C)
        OpType_NewTable,                // A    R(A) = {}
        OpType_AppendTable,             // AB   R(A).append(R(B)..R(top-1))
        OpType_SetTable,                // ABC  R(A)[R(B)] = R(C)
        OpType_GetTable,                // ABC  R(C) = R(A)[R(B)]
        OpType_TableIter,               // AB   R(A) = get_iter(R(B))
        OpType_TableIterNext,           // ABC  R(B) = iter_key(R(A)), R(C) = iter_key(R(A)) 
        OpType_ForStep,                 // ABC  if CheckStep(R(A),R(B),R(C)) { ++pc } next code is jmp tail
        OpType_StackShrink,             // A    shrink stack to R(A)
        OpType_SetTop,                  // A    R(top)..R(A) := nil; top = A
    }
    struct Instruction
    {
        System.Int32 _opcode;
        public Instruction(OpType op, int res)
        {
            _opcode = (((int)op) << 24) | (res & 0xffffff);
        }
        public void SetBx(int bx)
        {
            _opcode = _opcode | (bx & 0xffff);
        }
        public int GetBx()
        {
            return (Int16)(_opcode & 0xffff);
        }
        public OpType GetOp()
        {
            return (OpType)((_opcode >> 24) & 0xff);
        }
        public int GetA()
        {
            return (_opcode >> 16) & 0xff;
        }
        public int GetB()
        {
            return (_opcode >> 8) & 0xff;
        }
        public int GetC()
        {
            return _opcode & 0xff;
        }
        public static Instruction A(OpType op, int a)
        {
            return new Instruction(op, a << 16);
        }
        public static Instruction AB(OpType op, int a, int b)
        {
            return new Instruction(op, (a<<16) | (b<<8));
        }
        public static Instruction ABC(OpType op, int a, int b,int c)
        {
            return new Instruction(op, (a << 16) | (b << 8) | (c));
        }
        public static Instruction ABx(OpType op, int a, int bx)
        {
            return new Instruction(op, (a<<16) | bx);
        }
        public static Instruction Bx(OpType op, int bx)
        {
            return new Instruction(op, bx);
        }
    }
}