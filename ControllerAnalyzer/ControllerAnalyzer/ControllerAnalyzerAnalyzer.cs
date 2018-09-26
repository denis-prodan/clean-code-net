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
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var a = classDeclaration.ReturnType;
            var b = classDeclaration.Parameters;

            //var s = (context.Symbol.ContainingSymbol as INamedTypeSymbol);
            //// Find just those named type symbols with names containing lowercase letters.
            //if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            //{
            //    // For all such symbols, produce a diagnostic.
            //    var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

            //    context.ReportDiagnostic(diagnostic);
            //}
        }
    }
}
