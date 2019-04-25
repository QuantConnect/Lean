using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using RestSharp;
using System.IO;
using ICSharpCode.SharpZipLib.BZip2;

namespace TempHelper
{
    public static class AppDomainExtensions
    {
        public static AppDomain DefineDynamicAssembly(this AppDomain value, AssemblyName name, AssemblyBuilderAccess access)
        {
            throw new Exception();
        }
        public static AppDomain DefineDynamicModule(this AppDomain value, string name)
        { 
            throw new Exception();
        }
        public static TypeBuilder DefineType(this AppDomain value, string name,TypeAttributes attr,Type type)
        {
            throw new Exception();
        }
    }

    public static class TypeBuilderExtensions
    {
        public static Type CreateType(this TypeBuilder value)
        {
            throw new Exception();
        }
    }
    public static class RestRequestExtensions
    {
        public static IRestRequest AddFile(this RestRequest value, string name, Action<Stream> writer, string fileName, string contentType = null)
        {
            return value.AddFile(name, writer, fileName, 0, contentType);
        }
    }

    public class FieldDescriptor
    {
        public FieldDescriptor(string fieldName, Type fieldType)
        {
            FieldName = fieldName;
            FieldType = fieldType;
        }
        public string FieldName { get; }
        public Type FieldType { get; }
    }

    public static class MyTypeBuilder
    {
        public static object CreateNewObject()
        {
            var myTypeInfo = CompileResultTypeInfo();
            var myType = myTypeInfo.AsType();
            var myObject = Activator.CreateInstance(myType);

            return myObject;
        }

        public static TypeInfo CompileResultTypeInfo()
        {
            TypeBuilder tb = GetTypeBuilder();
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var yourListOfFields = new List<FieldDescriptor>()
            {
                new FieldDescriptor("YourProp1",typeof(string)),
                new FieldDescriptor("YourProp2", typeof(int))
            };
            foreach (var field in yourListOfFields)
                CreateProperty(tb, field.FieldName, field.FieldType);

            TypeInfo objectTypeInfo = tb.CreateTypeInfo();
            return objectTypeInfo;
        }

        private static TypeBuilder GetTypeBuilder()
        {
            var typeSignature = "MyDynamicType";
            var an = new AssemblyName(typeSignature);
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    null);
            return tb;
        }

        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}