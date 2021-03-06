using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal class SerializableTypeDescription : ISerializableTypeDescription
    {
        private readonly LibraryTypes _libraryTypes;
        private TypeSyntax _typeSyntax;

        public SerializableTypeDescription(SemanticModel semanticModel, INamedTypeSymbol type, IEnumerable<IMemberDescription> members, LibraryTypes libraryTypes)
        {
            Type = type;
            Members = members.ToList();
            SemanticModel = semanticModel;
            _libraryTypes = libraryTypes;

            var t = type;
            Accessibility accessibility = t.DeclaredAccessibility;
            while (t is not null)
            {
                if ((int)t.DeclaredAccessibility < (int)accessibility)
                {
                    accessibility = t.DeclaredAccessibility;
                }

                t = t.ContainingType;
            }

            Accessibility = accessibility;
            TypeParameters = new();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tp in type.GetAllTypeParameters())
            {
                var tpName = GetTypeParameterName(names, tp);
                TypeParameters.Add((tpName, tp));
            }

            SerializationHooks = new();
            if (type.GetAttributes(libraryTypes.SerializationCallbacksAttribute, out var hookAttributes))
            {
                foreach (var hookAttribute in hookAttributes)
                {
                    var hookType = (INamedTypeSymbol)hookAttribute.ConstructorArguments[0].Value;
                    SerializationHooks.Add(hookType);
                }
            }

            static string GetTypeParameterName(HashSet<string> names, ITypeParameterSymbol tp)
            {
                var count = 0;
                var result = tp.Name;
                while (names.Contains(result))
                {
                    result = $"{tp.Name}_{++count}";
                }

                names.Add(result);
                return result.EscapeIdentifier();
            }
        }

        private INamedTypeSymbol Type { get; }

        public Accessibility Accessibility { get; }

        public TypeSyntax TypeSyntax => _typeSyntax ??= Type.ToTypeSyntax();

        public bool HasComplexBaseType => !IsValueType &&
                                          Type.BaseType != null &&
                                          Type.BaseType.SpecialType != SpecialType.System_Object;

        public INamedTypeSymbol BaseType => Type.EnumUnderlyingType ?? Type.BaseType;

        public string Namespace => Type.GetNamespaceAndNesting();

        public string GeneratedNamespace => Namespace switch
        {
            { Length: > 0 } ns => $"{CodeGenerator.CodeGeneratorName}.{ns}",
            _ => CodeGenerator.CodeGeneratorName
        };

        public string Name => Type.Name;

        public bool IsValueType => Type.IsValueType;
        public bool IsSealedType => Type.IsSealed;
        public bool IsEnumType => Type.EnumUnderlyingType != null;

        public bool IsGenericType => Type.IsGenericType;

        public List<(string Name, ITypeParameterSymbol Parameter)> TypeParameters { get; }

        public List<IMemberDescription> Members { get; }
        public SemanticModel SemanticModel { get; }

        public bool IsEmptyConstructable
        {
            get
            {
                if (Type.Constructors.Length == 0)
                {
                    return true;
                }

                foreach (var ctor in Type.Constructors)
                {
                    if (ctor.Parameters.Length != 0)
                    {
                        continue;
                    }

                    switch (ctor.DeclaredAccessibility)
                    {
                        case Accessibility.Public:
                            return true;
                    }
                }

                return false;
            }
        }

        public bool IsPartial
        {
            get
            {
                foreach (var reference in Type.DeclaringSyntaxReferences)
                {
                    var syntax = reference.GetSyntax();
                    if (syntax is TypeDeclarationSyntax typeDeclaration && typeDeclaration.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool UseActivator => Type.HasAttribute(_libraryTypes.UseActivatorAttribute) || !IsEmptyConstructable;

        public bool TrackReferences => !IsValueType && !Type.HasAttribute(_libraryTypes.SuppressReferenceTrackingAttribute);
        public bool OmitDefaultMemberValues => Type.HasAttribute(_libraryTypes.OmitDefaultMemberValuesAttribute);

        public List<INamedTypeSymbol> SerializationHooks { get; }

        public bool IsImmutable => IsEnumType || Type.HasAnyAttribute(_libraryTypes.ImmutableAttributes);

        public ExpressionSyntax GetObjectCreationExpression(LibraryTypes libraryTypes) => InvocationExpression(ObjectCreationExpression(TypeSyntax));
    }
}