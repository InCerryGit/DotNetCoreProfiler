﻿using Interception.Common;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Interception.Generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var interceptions = typeof(InterceptorBase)
                .Assembly
                .GetTypes()
                .SelectMany(type => type.GetRuntimeMethods())
                .Where(method => method.GetCustomAttributes<InterceptAttribute>(false).Any())
                .SelectMany(method => method.GetCustomAttributes<InterceptAttribute>(false).Select(attribute => new { method, attribute }))
                .Select(info =>
                {
                    var returnType = info.method.ReturnType;
                    var parameters = info.method.GetParameters().Select(p => p.ParameterType).ToArray();
                    var signatureHelper = SignatureHelper.GetMethodSigHelper(info.method.CallingConvention, returnType);
                    signatureHelper.AddArguments(parameters, requiredCustomModifiers: null, optionalCustomModifiers: null);
                    var signatureBytes = signatureHelper.GetSignature();

                    if (info.method.IsGenericMethod)
                    {
                        byte IMAGE_CEE_CS_CALLCONV_GENERIC = 0x10;
                        var genericArguments = info.method.GetGenericArguments();

                        var newSignatureBytes = new byte[signatureBytes.Length + 1];
                        newSignatureBytes[0] = (byte)(signatureBytes[0] | IMAGE_CEE_CS_CALLCONV_GENERIC);
                        newSignatureBytes[1] = (byte)genericArguments.Length;
                        Array.Copy(signatureBytes, 1, newSignatureBytes, 2, signatureBytes.Length - 1);

                        signatureBytes = newSignatureBytes;
                    }

                    return new {
                        info.attribute.CallerAssembly,
                        info.attribute.TargetAssemblyName,
                        info.attribute.TargetMethodName,
                        info.attribute.TargetTypeName,
                        info.attribute.TargetMethodParametersCount,
                        WrapperTypeName = info.method.DeclaringType.FullName,
                        WrapperMethodName = info.method.Name,
                        WrapperAssemblyName = info.method.DeclaringType.Assembly.GetName().Name,
                        WrapperAssemblyPath = "/profiler/Interception.dll",
                        WrapperSignature = string.Join(" ", signatureBytes.Select(b => b.ToString("X2")))
                    };
                })
                .ToList();


            var serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            var json = JsonConvert.SerializeObject(interceptions, serializerSettings);
            Console.WriteLine(json);

            File.WriteAllText("interceptions.json", json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }
}