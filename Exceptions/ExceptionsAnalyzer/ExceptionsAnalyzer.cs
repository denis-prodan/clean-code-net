using System;
using System.Collections.Immutable;
using System.Linq;
using CleanCode.NET.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExceptionsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExceptionsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                Descriptors.NoExceptionUsageDescriptor,
                Descriptors.RethrowSameExceptionDescriptor,
                Descriptors.RethrowWithoutInnerDescriptor,
                Descriptors.ExceptionAnalyzerErrorDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            if (Settings.Current.IsInitialized && Settings.Current.ExceptionsSeverity == Severity.Ignore)
            {
                return;
            }
            context.RegisterSyntaxNodeAction(AnalyzeSymbol,SyntaxKind.TryStatement);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            try
            {
                var tryStatement = context.Node as TryStatementSyntax;

                var diagnostics = tryStatement.Catches.Select(AnalyzeCatch)
                    .Where(x => x != null);

                foreach (var analysisResult in diagnostics)
                {
                    context.ReportDiagnostic(analysisResult);
                }
            }
            catch (Exception e)
            {
                var diagnostic = Diagnostic.Create(
                    descriptor: Descriptors.ExceptionAnalyzerErrorDescriptor,
                    location: context.Node.GetLocation(),
                    messageArgs: e.ToString());

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static Diagnostic AnalyzeCatch(CatchClauseSyntax catchSyntax)
        {
            var variableName = GetCatchDeclarationVariableName(catchSyntax);

            var catchAnalysisResults = catchSyntax.Block.Statements
                .Select(x => (node: x, result:IsVariableUsedInStatement(variableName, x)))
                .Distinct()
                .ToList();

            if (catchAnalysisResults.Count == 0 || catchAnalysisResults.All(x => x.result == StatementAnalysisResult.NoUsage))
            {
                return Diagnostic.Create(Descriptors.NoExceptionUsageDescriptor, catchSyntax.CatchKeyword.GetLocation());
            }

            var rethrowCase = catchAnalysisResults.FirstOrDefault(x => x.result == StatementAnalysisResult.RethrowSameException);
            if (rethrowCase.node != null)
            {
                return Diagnostic.Create(Descriptors.RethrowSameExceptionDescriptor, rethrowCase.node.GetLocation());
            }

            if (catchAnalysisResults.Any(x => x.result == StatementAnalysisResult.RethrowWithoutInnerException))
            {
                return Diagnostic.Create(Descriptors.RethrowWithoutInnerDescriptor, catchSyntax.CatchKeyword.GetLocation());
            }

            return null;
        }

        private static StatementAnalysisResult IsVariableUsedInStatement(string variableName, StatementSyntax statement)
        {
            if (statement is ExpressionStatementSyntax expression)
            {
                return IsVariableUsedInExpression(variableName, expression.Expression) 
                    ? StatementAnalysisResult.Used 
                    : StatementAnalysisResult.NoUsage;
            }

            if (statement is LocalDeclarationStatementSyntax localDeclaration)
            {
               return localDeclaration.Declaration.Variables.Any(x => IsVariableUsedInExpression(variableName, x.Initializer.Value))
                   ? StatementAnalysisResult.Used
                   : StatementAnalysisResult.NoUsage;
            }

            if (statement is ThrowStatementSyntax throwStatement)
            {
                // "throw;" statement, good rethrow
                if (throwStatement.Expression == null)
                    return StatementAnalysisResult.CorrectRethrow;
                if (throwStatement.Expression != null)
                {
                    if (throwStatement.Expression is IdentifierNameSyntax identifier)
                    {
                        if (identifier.Identifier.ValueText == variableName)
                        {
                            // "throw e" - leads to lost stack trace
                            return StatementAnalysisResult.RethrowSameException;
                        }
                    }

                    return IsVariableUsedInExpression(variableName, throwStatement.Expression)
                        ? StatementAnalysisResult.CorrectRethrow // Rethrow with inner exception
                        : StatementAnalysisResult.RethrowWithoutInnerException;
                }
            }

            throw new NotImplementedException($"Unknown statement type {statement}");
        }

        private static bool IsVariableUsedInExpression(string variableName, ExpressionSyntax expression)
        {
            if (expression is InvocationExpressionSyntax invocation)
            {
                return invocation.ArgumentList.Arguments.Any(x => IsVariableUsedInExpression(variableName, x.Expression));
            }

            if (expression is IdentifierNameSyntax identifierName)
            {
                return identifierName.Identifier.ValueText == variableName;
            }

            if (expression is ObjectCreationExpressionSyntax creation)
            {
                return creation.ArgumentList.Arguments.Any(x => IsVariableUsedInExpression(variableName, x.Expression));
            }

            return false;
        }

        private static string GetCatchDeclarationVariableName(CatchClauseSyntax catchClause)
        {
            return catchClause.Declaration != null ? catchClause.Declaration.Identifier.ValueText : "";
        }
    }
}
