using System;
using XSharp.Assembler.x86.SSE;

using XSharp;
using static XSharp.XSRegisters;
using CPUx86 = XSharp.Assembler.x86;
using Label = XSharp.Assembler.Label;

namespace Cosmos.IL2CPU.X86.IL
{
    [Cosmos.IL2CPU.OpCode(ILOpCode.Code.Mul)]
    public class Mul : ILOp
    {
        public Mul(XSharp.Assembler.Assembler aAsmblr)
            : base(aAsmblr)
        {
        }

        public override void Execute(Il2cpuMethodInfo aMethod, ILOpCode aOpCode)
        {
            var xStackContent = aOpCode.StackPopTypes[0];
            var xStackContentSize = SizeOfType(xStackContent);
            var xStackContentIsFloat = TypeIsFloat(xStackContent);
            string BaseLabel = GetLabel(aMethod, aOpCode) + ".";
            DoExecute(xStackContentSize, xStackContentIsFloat, BaseLabel);
        }

        public static void DoExecute(uint xStackContentSize, bool xStackContentIsFloat, string aBaseLabel)
        {
            if (xStackContentSize > 4)
            {
                if (xStackContentIsFloat)
                {
                    XS.SSE2.MoveSD(XMM0, RSP, sourceIsIndirect: true);
                    XS.Add(RSP, 8);
                    XS.SSE2.MoveSD(XMM1, RSP, sourceIsIndirect: true);
                    XS.SSE2.MulSD(XMM1, XMM0);
                    XS.SSE2.MoveSD(RSP, XMM1, destinationIsIndirect: true);
                }
                else
                {
                    // div of both == LEFT_LOW * RIGHT_LOW + ((LEFT_LOW * RIGHT_HIGH + RIGHT_LOW * LEFT_HIGH) << 32)
                    string Simple32Multiply = aBaseLabel + "Simple32Multiply";
                    string MoveReturnValue = aBaseLabel + "MoveReturnValue";

                    // right value
                    // low
                    //  SourceReg = CPUx86.Registers.ESP, SourceIsIndirect = true
                    // high
                    //  SourceReg = CPUx86.Registers.ESP, SourceIsIndirect = true, SourceDisplacement = 4

                    // left value
                    // low
                    //  SourceReg = CPUx86.Registers.ESP, SourceIsIndirect = true, SourceDisplacement = 8
                    // high
                    //  SourceReg = CPUx86.Registers.ESP, SourceIsIndirect = true, SourceDisplacement = 12

                    // compair LEFT_HIGH, RIGHT_HIGH , on zero only simple multiply is used
                    //mov RIGHT_HIGH to eax, is useable on Full 64 multiply
                    XS.Set(RAX, RSP, sourceDisplacement: 4);
                    XS.Or(RAX, RSP, sourceDisplacement: 12);
                    XS.Jump(CPUx86.ConditionalTestEnum.Zero, Simple32Multiply);
                    // Full 64 Multiply

                    // copy again, or could change EAX
                    //TODO is there an opcode that does OR without change EAX?
                    XS.Set(RAX, RSP, sourceDisplacement: 4);
                    // eax contains already RIGHT_HIGH
                    // multiply with LEFT_LOW
                    XS.Multiply(RSP, displacement: 8);
                    // save result of LEFT_LOW * RIGHT_HIGH
                    XS.Set(RCX, RAX);

                    //mov RIGHT_LOW to eax
                    XS.Set(RAX, RSP, sourceIsIndirect: true);
                    // multiply with LEFT_HIGH
                    XS.Multiply(RSP, displacement: 12);
                    // add result of LEFT_LOW * RIGHT_HIGH + RIGHT_LOW + LEFT_HIGH
                    XS.Add(RCX, RAX);

                    //mov RIGHT_LOW to eax
                    XS.Set(RAX, RSP, sourceIsIndirect: true);
                    // multiply with LEFT_LOW
                    XS.Multiply(RSP, displacement: 8);
                    // add LEFT_LOW * RIGHT_HIGH + RIGHT_LOW + LEFT_HIGH to high dword of last result
                    XS.Add(RDX, RCX);

                    XS.Jump(MoveReturnValue);

                    XS.Label(Simple32Multiply);
                    //mov RIGHT_LOW to eax
                    XS.Set(RAX, RSP, sourceIsIndirect: true);
                    // multiply with LEFT_LOW
                    XS.Multiply(RSP, displacement: 8);

                    XS.Label(MoveReturnValue);
                    // move high result to left high
                    XS.Set(RSP, RDX, destinationDisplacement: 12);
                    // move low result to left low
                    XS.Set(RSP, RAX, destinationDisplacement: 8);
                    // pop right 64 value
                    XS.Add(RSP, 8);
                }
            }
            else
            {
                if (xStackContentIsFloat)
                {
                    XS.SSE.MoveSS(XMM0, RSP, sourceIsIndirect: true);
                    XS.Add(RSP, 4);
                    XS.SSE.MoveSS(XMM1, RSP, sourceIsIndirect: true);
                    XS.SSE.MulSS(XMM1, XMM0);
                    XS.SSE.MoveSS(RSP, XMM1, destinationIsIndirect: true);
                }
                else
                {
                    XS.Pop(RAX);
                    XS.Multiply(RSP, isIndirect: true, size: RegisterSize.Long64);
                    XS.Add(RSP, 4);
                    XS.Push(RAX);
                }
            }
        }
    }
}
