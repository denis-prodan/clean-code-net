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

            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);        

            var methods = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(x => x.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)));

            foreach(var method in methods)
            {
                ValidateMethod(context, classSymbol, method);
            }
        }

        private static void ValidateMethod(SyntaxNodeAnalysisContext context, INamedTypeSymbol classSymbol, MethodDeclarationSyntax method)
        {
            var semanticModel = context.SemanticModel;
            var returnTypes = GetAllReturnTypes(semanticModel, method).ToList();

            var validatedReturns = returnTypes.Select(x => (parameter: x.node, type: x.typeSymbol, valid: ValidateSymbol(classSymbol, x.typeSymbol))).ToList();
            var validatedParams = method.ParameterList.Parameters.Select(x => (parameter: x, valid: ValidateParameter(semanticModel, classSymbol, x))).ToList();  
            
            foreach (var returnResult in validatedReturns.Where(x => !x.valid))
            {
                

                var diagnostic = Diagnostic.Create(
                    descriptor: Rule,
                    location: method.GetLocation(),
                    additionalLocations: new[] { returnResult.parameter.GetLocation() },
                    properties: new Dictionary<string, string>().ToImmutableDictionary(),
                    effectiveSeverity: DiagnosticSeverity.Warning, 
                    messageArgs: returnResult.type.Name);
                context.ReportDiagnostic(diagnostic);
            }

            foreach (var paramResult in validatedParams.Where(x => !x.valid))
            {
                var diag = Diagnostic.Create(Rule, method.GetLocation(), "");
                context.ReportDiagnostic(diag);

                var diagnostic = Diagnostic.Create(
                    descriptor: Rule,
                    location: method.GetLocation(),
                    additionalLocations: new[] { paramResult.parameter.GetLocation() },
                    properties: new Dictionary<string, string>().ToImmutableDictionary(),
                    effectiveSeverity: DiagnosticSeverity.Warning,
                    messageArgs: "");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<(SyntaxNode node, ITypeSymbol typeSymbol)> GetAllReturnTypes(SemanticModel semanticModel, MethodDeclarationSyntax method)
        {
            yield return (method.ReturnType, semanticModel.GetDeclaredSymbol(method).ReturnType);

            var returnStatements = method.Body.DescendantNodes().OfType<ReturnStatementSyntax>();
            var resultInnerArguments = returnStatements.SelectMany(x => GetReturnStatementParameterNodes(semanticModel, x));

            var resultInnerTypes = resultInnerArguments.Select(x => (node: x, typeInfo: semanticModel.GetTypeInfo(x.Expression)));

            foreach (var resultType in resultInnerTypes)
            {
                yield return (resultType.node, typeSymbol: resultType.typeInfo.Type);
            }
         }

        private static IEnumerable<ArgumentSyntax> GetReturnStatementParameterNodes(SemanticModel semanticModel, ReturnStatementSyntax returnStatement)
        {
            if (returnStatement.Expression is InvocationExpressionSyntax invocationExpression)
            {
                return invocationExpression.ArgumentList.Arguments;
            }

            return new ArgumentSyntax[0];
        }

        private static bool ValidateParameter(SemanticModel semanticModel, INamedTypeSymbol classSymbol, ParameterSyntax parameter)
        {
            var symbol = semanticModel.GetDeclaredSymbol(parameter);

            return ValidateSymbol(classSymbol, symbol.Type);
        }

        private static bool ValidateSymbol(INamedTypeSymbol classSymbol, ITypeSymbol symbol)
        {
            if (symbol.ContainingAssembly == classSymbol.ContainingAssembly
               || symbol.ContainingAssembly.Name.Contains("View")
               || symbol.ContainingAssembly.Name.StartsWith("System"))
            {
                return true;
            }

            return false;
        }
    }
}
