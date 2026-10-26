// Allows the current car model to be recreated during a pack reload.
using System;using System.Collections.Generic;using System.Reflection;using System.Reflection.Emit;namespace SpKnpp {public sealed partial class Exporter {  public static IEnumerable<T> SyncSameModelCreateTranspiler<T>(IEnumerable<T> instructions,MethodBase __originalMethod) {
   var code=new List<T>(instructions);
   Type instruction=typeof(T),machine=__originalMethod.DeclaringType;
   FieldInfo opcode=instruction.GetField("opcode"),operand=instruction.GetField("operand");
   FieldInfo state=machine.GetField("<>4__this",Flags),target=machine.GetField("newCarId",Flags);
   ConstructorInfo ctor=instruction.GetConstructor(new[]{typeof(OpCode),typeof(object)});
   if(opcode==null || operand==null || state==null || state.FieldType.FullName!="BaseRaceState" || target==null || target.FieldType!=typeof(int) || ctor==null)
    throw new InvalidOperationException("Unsupported CarX same-model car creation layout.");
   int found=-1;
   for(int i=0;i+4<code.Count;i++) {
    MethodInfo getter=operand.GetValue(code[i]) as MethodInfo;
    OpCode branch=(OpCode)opcode.GetValue(code[i+3]),leave=(OpCode)opcode.GetValue(code[i+4]);
    if((OpCode)opcode.GetValue(code[i])!=OpCodes.Callvirt || getter==null || getter.Name!="get_carId" || getter.DeclaringType.FullName!="RaceCar"
     || (OpCode)opcode.GetValue(code[i+1])!=OpCodes.Ldarg_0 || (OpCode)opcode.GetValue(code[i+2])!=OpCodes.Ldfld
     || !target.Equals(operand.GetValue(code[i+2])) || (branch!=OpCodes.Bne_Un && branch!=OpCodes.Bne_Un_S)
     || (leave!=OpCodes.Leave && leave!=OpCodes.Leave_S)) continue;
    if(found!=-1) throw new InvalidOperationException("Ambiguous CarX same-model creation guard.");
    found=i;
   }
   if(found<0) throw new InvalidOperationException("CarX same-model creation guard was not found. Car refresh was not changed.");
   object destination=operand.GetValue(code[found+3]);
   var added=new List<T>();
   added.Add((T)ctor.Invoke(new object[]{OpCodes.Ldarg_0,null}));
   added.Add((T)ctor.Invoke(new object[]{OpCodes.Ldfld,state}));
   added.Add((T)ctor.Invoke(new object[]{OpCodes.Ldarg_0,null}));
   added.Add((T)ctor.Invoke(new object[]{OpCodes.Ldfld,target}));
   added.Add((T)ctor.Invoke(new object[]{OpCodes.Call,typeof(Exporter).GetMethod("DeveloperShouldRecreateCar",BindingFlags.Public|BindingFlags.Static)}));
   added.Add((T)ctor.Invoke(new object[]{OpCodes.Brtrue,destination}));
   // Keep the native early return for ordinary gameplay. Only this RCLI operation
   // may take the existing native creation branch when the requested model matches.
   code.InsertRange(found+4,added);
   return code;
  }
}}
