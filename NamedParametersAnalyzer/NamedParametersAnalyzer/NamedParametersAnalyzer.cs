using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NamedParametersAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamedParametersAnalyzer : DiagnosticAnalyzer
    {
        private const int ParamsThreshold = 4;
        public const string DiagnosticId = "CCN0031";
        private const string AnalyzerErrorDiagnosticId = "CCN0030";

        private static readonly string Title = $"Method calls with {ParamsThreshold} or more parameters should be named";

        public static readonly string MessageFormat = $"Method calls with {ParamsThreshold} or more parameters should have param names";

        private static readonly string Description = "Check that calls with many parameters has their names";
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Title,
            messageFormat: MessageFormat,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description);

        private static readonly DiagnosticDescriptor AnalyzerErrorDescriptor = new DiagnosticDescriptor(
            id: AnalyzerErrorDiagnosticId,
            title: "NamedParametersAnalyzer throws unhandled exception",
            messageFormat: "Analyzer failed with exception: {0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Inner exception inside analyzer. Please, contact author with details");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, AnalyzerErrorDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression, SyntaxKind.ObjectCreationExpression);
        }

        private static void SafeAnalyze(SyntaxNodeAnalysisContext context)
        {
            try
            {
                AnalyzeNode(context);
            }
            catch (Exception e)
            {
                var diagnostic = Diagnostic.Create(
                    descriptor: AnalyzerErrorDescriptor,
                    location: context.Node.GetLocation(),
                    messageArgs: e.ToString());

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext nodeContext)
        {
            var argumentsList = GetArgumentsList(nodeContext.Node);
            if (argumentsList == null || argumentsList.Arguments.Count < ParamsThreshold)
                return;

            var argumentsWithoutNamecolon = argumentsList.Arguments.Where(x => x.NameColon == null);

            if (!argumentsWithoutNamecolon.Any())
                return;

            var call = nodeContext.Node as ExpressionSyntax;
            var callParameters = ParametersHelper.GetInvocationParameters(nodeContext.SemanticModel, call);
            if (callParameters.Any(x => x.isParams))
                return;

            var diagnostic = Diagnostic.Create(
                descriptor: Rule,
                location: nodeContext.Node.GetLocation());

            nodeContext.ReportDiagnostic(diagnostic);
        }

        private static ArgumentListSyntax GetArgumentsList(SyntaxNode node)
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation: return invocation.ArgumentList;
                case ObjectCreationExpressionSyntax creation: return creation.ArgumentList;
                default: return null;
            }
        }
    }
}

