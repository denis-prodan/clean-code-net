using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ControllerAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ControllerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CN0041";

        private static readonly string Title = "Controller methods should not return or accept classes for other assemblies";
        private static readonly string MessageFormat = "Method {0} returns/accepts classes from another assembly";
        private static readonly string Description = "Using classes from another assembly that doesn't contain only external models considered as bad practice and layer mixing.";
        private const string Category = "Conventions";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            // todo: consider more accurate way
            if (!classDeclaration.Identifier.ValueText.Contains("Controller"))
            {
                return;
            }

            var classAssembly = classDeclaration.Identifier.
            var methods = classDeclaration.Members.OfType<MethodDeclarationSyntax>();

            foreach(var method in methods)
            {
                ValidateMethod(classAssembly, method);
            }
        }

        private static void ValidateMethod(string classAssembly, MethodDeclarationSyntax method)
        {
            var a = method.ReturnType;
            var methodTypes = method.ParameterList.Parameters.SelectMany(x => GetParameterTypes(x));

            var type = a.GetType();
            if (type.AssemblyQualifiedName == classAssembly 
                || type.AssemblyQualifiedName.Contains("View")
                || type.AssemblyQualifiedName.StartsWith("System"))
            {
                return;
            }
        }

        private static IEnumerable<TypeSyntax> GetParameterTypes(ParameterSyntax parameter)
        {
            yield return parameter.Type;
        }
    }
}
