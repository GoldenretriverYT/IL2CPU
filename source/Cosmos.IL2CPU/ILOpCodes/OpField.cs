﻿using System;
using System.Reflection;


namespace Cosmos.IL2CPU.ILOpCodes
{
  public class OpField : ILOpCode
  {
    public FieldInfo Value { get; }

    public OpField(Code aOpCode, int aPos, int aNextPos, FieldInfo aValue, _ExceptionRegionInfo aCurrentExceptionRegion)
      : base(aOpCode, aPos, aNextPos, aCurrentExceptionRegion)
    {
      Value = aValue;
    }

    public override int GetNumberOfStackPops(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Stsfld:
          return 1;
        case Code.Ldsfld:
          return 0;
        case Code.Stfld:
          return 2;
        case Code.Ldfld:
          return 1;
        case Code.Ldsflda:
          return 0;
        case Code.Ldflda:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    public override int GetNumberOfStackPushes(MethodBase aMethod)
    {
      switch (OpCode)
      {
        case Code.Stsfld:
          return 0;
        case Code.Ldsfld:
          return 1;
        case Code.Stfld:
          return 0;
        case Code.Ldfld:
          return 1;
        case Code.Ldsflda:
          return 1;
        case Code.Ldflda:
          return 1;
        default:
          throw new NotImplementedException("OpCode '" + OpCode + "' not implemented!");
      }
    }

    protected override void DoInitStackAnalysis(MethodBase aMethod)
    {
      base.DoInitStackAnalysis(aMethod);

      switch (OpCode)
      {
        case Code.Ldsfld:
          StackPushTypes[0] = Value.FieldType;
          if (StackPushTypes[0].IsEnum)
          {
            StackPushTypes[0] = StackPushTypes[0].GetEnumUnderlyingType();
          }
          return;
        case Code.Ldsflda:
          StackPushTypes[0] = typeof(IntPtr);
          return;
        case Code.Ldfld:
          StackPushTypes[0] = Value.FieldType;
          if (StackPushTypes[0].IsEnum)
          {
            StackPushTypes[0] = StackPushTypes[0].GetEnumUnderlyingType();
          }
          if (!Value.DeclaringType.IsValueType)
          {
            StackPopTypes[0] = Value.DeclaringType;
          }
          return;
        case Code.Ldflda:
          StackPopTypes[0] = Value.DeclaringType;
          if (StackPopTypes[0].IsEnum)
          {
            StackPopTypes[0] = StackPopTypes[0].GetEnumUnderlyingType();
          }
          if (StackPopTypes[0].IsValueType &&
              !StackPopTypes[0].IsPrimitive)
          {
            StackPopTypes[0] = StackPopTypes[0].MakeByRefType();
          }
          StackPushTypes[0] = typeof(IntPtr);
          return;
      }
    }

    protected override void DoInterpretStackTypes(ref bool aSituationChanged)
    {
      base.DoInterpretStackTypes(ref aSituationChanged);
      switch (OpCode)
      {
        case Code.Stfld:
          if (StackPopTypes[1] == null)
          {
            return;
          }
          var expectedType = Value.FieldType;
          if (expectedType.IsEnum)
          {
            expectedType = expectedType.GetEnumUnderlyingType();
          }
          else if (Value.DeclaringType.IsValueType && !Value.DeclaringType.IsPrimitive)
          {
            expectedType = typeof(void*);
          }
          if (StackPopTypes[1] == expectedType ||
              StackPopTypes[1] == Value.FieldType)
          {
            return;
          }
          if (ILOp.IsIntegralType(expectedType) &&
              ILOp.IsIntegralType(StackPopTypes[1]))
          {
            return;
          }
          if (expectedType == typeof(bool))
          {
            if (StackPopTypes[1] == typeof(int))
            {
              return;
            }
          }
          if (StackPopTypes[1] == typeof(NullRef))
          {
            return;
          }
          if (expectedType.IsAssignableFrom(StackPopTypes[1]))
          {
            return;
          }
          if (StackPopTypes[0] == null)
          {
            return;
          }

          if (Value.FieldType.IsAssignableFrom(StackPopTypes[0]))
          {
            return;
          }
          if (ILOp.IsIntegralType(Value.FieldType) &&
              ILOp.IsIntegralType(StackPopTypes[0]))
          {
            return;
          }
          if (Value.FieldType == typeof(bool) &&
              ILOp.IsIntegralType(StackPopTypes[0]))
          {
            return;
          }
          if (Value.FieldType.IsEnum)
          {
            if (ILOp.IsIntegralType(StackPopTypes[0]))
            {
              return;
            }
          }
          if (ILOp.IsPointer(Value.FieldType) &&
              ILOp.IsPointer(StackPopTypes[0]))
          {
            return;
          }
          if (Value.FieldType.IsClass &&
              StackPopTypes[0] == typeof(NullRef))
          {
            return;
          }
          throw new Exception("Wrong Poptype encountered! (Type = " + StackPopTypes[0].FullName + ", expected = " + expectedType.FullName + ")");
        // throw new Exception("Wrong Poptype encountered!");
        case Code.Stsfld:
          if (StackPopTypes[0] == null)
          {
            return;
          }
          if(StackPopTypes[0] == typeof(void*))
          {
          return;
          }
          expectedType = Value.FieldType;
          if (expectedType.IsEnum)
          {
            expectedType = expectedType.GetEnumUnderlyingType();
          }
          if (StackPopTypes[0] == expectedType ||
              StackPopTypes[0] == Value.FieldType)
          {
            return;
          }
          if (ILOp.IsIntegralType(expectedType) &&
              ILOp.IsIntegralType(StackPopTypes[0]))
          {
            return;
          }
          if (expectedType == typeof(bool))
          {
            if (StackPopTypes[0] == typeof(int))
            {
              return;
            }
          }
          if (expectedType.IsAssignableFrom(StackPopTypes[0]))
          {
            return;
          }
          if (StackPopTypes[0] == typeof(NullRef))
          {
            return;
          }
          if ((StackPopTypes[0] == typeof(IntPtr)
               || StackPopTypes[0] == typeof(UIntPtr))
              & expectedType.IsPointer)
          {
            return;
          }
          throw new Exception("Wrong Poptype encountered! (Type = " + StackPopTypes[0].FullName + ", expected = " + expectedType.FullName + ")");
        case Code.Ldfld:
          if (StackPopTypes[0] == null)
          {
            return;
          }
          if (!Value.DeclaringType.IsValueType)
          {
            return;
          }
          if (StackPopTypes[0] == Value.DeclaringType.MakePointerType() ||
              StackPopTypes[0] == Value.DeclaringType.MakeByRefType() ||
              StackPopTypes[0] == typeof(void*) ||
              StackPopTypes[0] == typeof(IntPtr))
          {
            return;
          }
          if (StackPopTypes[0] == Value.DeclaringType)
          {
            return;
          }
          throw new Exception("Wrong Poptype encountered! (Type = " + StackPopTypes[0].FullName + ", expected = " + Value.DeclaringType.FullName + ")");
      }
    }
  }
}
