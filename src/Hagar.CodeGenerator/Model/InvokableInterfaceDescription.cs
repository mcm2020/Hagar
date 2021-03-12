using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Hagar.CodeGenerator
{
    internal class InvokableInterfaceDescription : IInvokableInterfaceDescription
    {
        public InvokableInterfaceDescription(
            LibraryTypes libraryTypes,
            SemanticModel semanticModel,
            INamedTypeSymbol interfaceType,
            string name,
            List<MethodDescription> methods,
            INamedTypeSymbol proxyBaseType,
            bool isExtension)
        {
            ValidateBaseClass(libraryTypes, proxyBaseType);
            SemanticModel = semanticModel;
            InterfaceType = interfaceType;
            ProxyBaseType = proxyBaseType;
            IsExtension = isExtension;
            Methods = methods;
            Name = name;
            GeneratedNamespace = CodeGenerator.CodeGeneratorName + "." + InterfaceType.GetNamespaceAndNesting();
        }

        private static void ValidateBaseClass(LibraryTypes l, INamedTypeSymbol baseClass)
        {
            var found = false;
            foreach (var member in baseClass.GetMembers("SendRequest"))
            {
                if (member is not IMethodSymbol method)
                {
                    Throw(member, "not method");
                }

                if (method.TypeParameters.Length != 0)
                {
                    Throw(member, "type params");
                }

                if (method.Parameters.Length != 2)
                {
                    Throw(member, "params length");
                }

                if (!SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, l.IResponseCompletionSource))
                {
                    Throw(member, "param 0");
                }

                if (!SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, l.IInvokable))
                {
                    Throw(member, "param 1");
                }

                if (!method.ReturnsVoid)
                {
                    Throw(member, "return type");
                }

                found = true;
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"Proxy base class {baseClass} does not contain a definition for void SendRequest(IResponseCompletionSource, IInvokable)");
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Throw(ISymbol m, string x) => throw new InvalidOperationException("Complaint: " + x + " for symbol: " + m.ToDisplayString());
        }

        public string Name { get; }
        public INamedTypeSymbol InterfaceType { get; }
        public List<MethodDescription> Methods { get; }
        public INamedTypeSymbol ProxyBaseType { get; }
        public bool IsExtension { get; }
        public SemanticModel SemanticModel { get; }
        public string GeneratedNamespace { get; }
    }
}