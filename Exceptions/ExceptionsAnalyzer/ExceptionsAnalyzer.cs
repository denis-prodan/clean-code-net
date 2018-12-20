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
            ImmutableArray.Create(new[]
            {
                Descriptors.NoExceptionUsageDescriptor,
                Descriptors.RethrowSameExceptionDescriptor,
                Descriptors.RethrowWithoutInnerDescriptor,
                Descriptors.ExceptionAnalyzerErrorDescriptor
            });

        public override void Initialize(AnalysisContext context)
        {
            var proceedNoCheck = Settings.Current.ShouldProceed(x => x.ExceptionsNoCheck);
            var proceedRethrow = Settings.Current.ShouldProceed(x => x.ExceptionsRethrowSame);
            var proceedNoInner = Settings.Current.ShouldProceed(x => x.ExceptionsRwthrowWithoutInner);
            if (!(proceedNoCheck || proceedRethrow || proceedRethrow))
            {
                return;
            }
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.TryStatement);

            void Analyze(SyntaxNodeAnalysisContext con) => AnalyzeSymbol(
                context: con,
                proceedNoCheck: proceedNoCheck,
                proceedRethrow: proceedRethrow,
                proceedNoInner: proceedNoInner);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context, bool proceedNoCheck, bool proceedRethrow, bool proceedNoInner)
        {
            try
            {
                var tryStatement = context.Node as TryStatementSyntax;

                var diagnostics = tryStatement.Catches.Select(AnalyzeCatch)
                    .Where(x => x != null);
                if (!proceedNoCheck)
                    diagnostics = diagnostics.Where(x => x.Id != Descriptors.NoExceptionUsageId);
                if (!proceedRethrow)
                    diagnostics = diagnostics.Where(x => x.Id != Descriptors.RethrowSameExceptionId);
                if (!proceedNoInner)
                    diagnostics = diagnostics.Where(x => x.Id != Descriptors.RethrowWithoutInnerExceptionId);

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

            // if specific exception type used, but no variable, assume that this is intentional handling.
            if (variableName == null)
            {
                return null;
            }

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
                return IsVariableUsedInExpression(variableName, expression);
            }

            if (statement is LocalDeclarationStatementSyntax localDeclaration)
            {
                return IsVariableUsedInLocalDeclaration(variableName, localDeclaration);
            }

            if (statement is ThrowStatementSyntax throwStatement)
            {
                return IsVariableUsedInThrow(variableName, throwStatement);
            }

            if (statement is ReturnStatementSyntax returnStatement)
            {
                return IsVariableUsedInReturn(variableName, returnStatement);
            }

            if (statement is BlockSyntax blockStatement)
            {
                return IsVariableUSedInBlock(variableName, blockStatement);
            }

            if (statement is IfStatementSyntax ifStatement)
            {
                return IsVariableUsedInIf(variableName, ifStatement);
            }

            if (statement is TryStatementSyntax tryStatement)
            {
                return IsVariableUsedInStatement(variableName, tryStatement.Block);
            }

            throw new NotImplementedException($"Unknown statement type {statement}");
        }

        private static StatementAnalysisResult IsVariableUsedInThrow(string variableName,
            ThrowStatementSyntax throwStatementSyntax)
        {
            // "throw;" statement, good rethrow
            if (throwStatementSyntax.Expression == null)
                return StatementAnalysisResult.CorrectRethrow;

            if (throwStatementSyntax.Expression is IdentifierNameSyntax identifier)
            {
                if (identifier.Identifier.ValueText == variableName)
                {
                    // "throw e" - leads to lost stack trace
                    return StatementAnalysisResult.RethrowSameException;
                }
            }

            return IsVariableUsedInExpression(variableName, throwStatementSyntax.Expression)
                ? StatementAnalysisResult.CorrectRethrow // Rethrow with inner exception
                : StatementAnalysisResult.RethrowWithoutInnerException;
        }

        private static StatementAnalysisResult IsVariableUsedInIf(string variableName,
            IfStatementSyntax ifStatementSyntax)
        {
            // negative cases
            var statementUsage = IsVariableUsedInStatement(variableName, ifStatementSyntax.Statement);
            if (statementUsage < StatementAnalysisResult.NoUsage)
                return statementUsage;

            var elseUsage = IsVariableUsedInStatement(variableName, ifStatementSyntax.Else.Statement);
            if (elseUsage < StatementAnalysisResult.NoUsage)
                return elseUsage;

            // positive cases
            if (statementUsage > StatementAnalysisResult.NoUsage)
                return statementUsage;
            if (elseUsage > StatementAnalysisResult.NoUsage)
                return elseUsage;

            // final decision if none above
            var condiitionUsage = IsVariableUsedInExpression(variableName, ifStatementSyntax.Condition);
            return condiitionUsage
                ? StatementAnalysisResult.Used
                : StatementAnalysisResult.NoUsage;
        }

        private static StatementAnalysisResult IsVariableUSedInBlock(string variableName, BlockSyntax blockSyntax)
        {
            var statementsResults = blockSyntax.Statements.Select(x => IsVariableUsedInStatement(variableName, x)).ToList();

            var wrongUsageStatement = statementsResults.FirstOrDefault(x => x < StatementAnalysisResult.NoUsage);
            if (wrongUsageStatement != StatementAnalysisResult.Undefined)
                return wrongUsageStatement;

            var correctUsageStatement = statementsResults.FirstOrDefault(x => x > StatementAnalysisResult.NoUsage);
            if (correctUsageStatement != StatementAnalysisResult.Undefined)
                return correctUsageStatement;

            return StatementAnalysisResult.NoUsage;
        }

        private static StatementAnalysisResult IsVariableUsedInReturn(string variableName,
            ReturnStatementSyntax returnStatementSyntax)
        {
            return IsVariableUsedInExpression(variableName, returnStatementSyntax.Expression)
                ? StatementAnalysisResult.Used
                : StatementAnalysisResult.NoUsage;
        }

        private static StatementAnalysisResult IsVariableUsedInLocalDeclaration(string variableName,
            LocalDeclarationStatementSyntax localDeclarationStatementSyntax)
        {
            return localDeclarationStatementSyntax.Declaration.Variables.Any(x =>
                IsVariableUsedInExpression(variableName, x.Initializer.Value))
                ? StatementAnalysisResult.Used
                : StatementAnalysisResult.NoUsage;
        }

        private static StatementAnalysisResult IsVariableUsedInExpression(string variableName,
            ExpressionStatementSyntax expressionStatementSyntax)
        {
            if (expressionStatementSyntax.Expression == null)
                return StatementAnalysisResult.NoUsage;

            return IsVariableUsedInExpression(variableName, expressionStatementSyntax.Expression)
                ? StatementAnalysisResult.Used
                : StatementAnalysisResult.NoUsage;
        }

        private static bool IsVariableUsedInExpression(string variableName, ExpressionSyntax expression)
        {
            if (expression is InvocationExpressionSyntax invocation)
            {
                var isUsedAsIdentifier = IsVariableUsedInExpression(variableName, invocation.Expression);
                return isUsedAsIdentifier || invocation.ArgumentList.Arguments.Any(x => IsVariableUsedInExpression(variableName, x.Expression));
            }

            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                return IsVariableUsedInExpression(variableName, memberAccess.Expression);
            }

            if (expression is IdentifierNameSyntax identifierName)
            {
                return identifierName.Identifier.ValueText == variableName;
            }

            if (expression is ObjectCreationExpressionSyntax creation)
            {
                return creation.ArgumentList.Arguments.Any(x => IsVariableUsedInExpression(variableName, x.Expression));
            }

            if (expression is InterpolatedStringExpressionSyntax interpolatedString)
            {
                return interpolatedString.Contents
                    .OfType<InterpolationSyntax>()
                    .Any(x => IsVariableUsedInExpression(variableName, x.Expression));
            }

            return false;
        }

        private static string GetCatchDeclarationVariableName(CatchClauseSyntax catchClause)
        {
            return catchClause.Declaration != null ? catchClause.Declaration.Identifier.ValueText : "";
        }
    }
}
