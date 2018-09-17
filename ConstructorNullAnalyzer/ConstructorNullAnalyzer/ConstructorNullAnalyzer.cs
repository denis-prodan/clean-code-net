using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ConstructorNullAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstructorNullAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Correctness";
        private const string AnalyzerErrorDiagnosticId = "CCN0010";
        public const string DiagnosticId = "CCN0011";
        private static readonly LocalizableString Title = "Not checked reference parameter in constructor";
        private static readonly LocalizableString MessageFormat = "Constructor should check that parameter(s) {0} are not null";
        private static readonly LocalizableString Description = "All reference type parameters should be checked for not-null";

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
            title: "ConstructorNullAnalyzer throws unhandled exception",
            messageFormat: "Analyzer failed with exception: {0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "Inner exception inside analyzer. Please, contact author with details");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, AnalyzerErrorDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeConstructorAndCatchUnhandledExceptions, SyntaxKind.ConstructorDeclaration);
        }

        private static void AnalyzeConstructorAndCatchUnhandledExceptions(SyntaxNodeAnalysisContext context)
        {
            try
            {
                AnalyzeConstructor(context);
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

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructor = (ConstructorDeclarationSyntax) context.Node;
            var referenceParameters = GetReferenceParameters(constructor, context.SemanticModel).ToList();

            var baseParameters = GetBaseParametersIfAny(constructor);

            var parametersThatNotPassedToInitializer = referenceParameters.Except(baseParameters);

            var checkedValues = GetCheckedIdentifiers(constructor).ToImmutableHashSet();

            var notCheckedParameters = parametersThatNotPassedToInitializer
                .Where(parameter => !checkedValues.Contains(parameter))
                .ToList();

            if (!notCheckedParameters.Any())
            {
                return;
            }

            var affectedParameters = constructor.ParameterList.Parameters
                .Where(x => notCheckedParameters.Any(par => par == x.Identifier.Text))
                .Select(x => x.Identifier);
            var paramsList = string.Join(", ", notCheckedParameters);
            var diagnostic = Diagnostic.Create(descriptor: Rule,
                location: constructor.Identifier.GetLocation(),
                additionalLocations: affectedParameters.Select(x => x.GetLocation()),
                messageArgs: paramsList);

            context.ReportDiagnostic(diagnostic);
        }

        private static IEnumerable<string> GetBaseParametersIfAny(ConstructorDeclarationSyntax constructor)
        {
            var initializer = constructor.Initializer;
            if (initializer == null)
            {
                return new string[0];
            }

            return initializer.ArgumentList.Arguments
                .Select(x => x.Expression)
                .OfType<IdentifierNameSyntax>()
                .Select(x => x.Identifier.ValueText);
        }

        private static IEnumerable<string> GetCheckedIdentifiers(ConstructorDeclarationSyntax constructor)
        {
            return constructor.Body.Statements
                .SelectMany(GetCheckedValuesInStatement)
                .Where(x => !string.IsNullOrEmpty(x));
        }

        private static IEnumerable<string> GetCheckedValuesInStatement(StatementSyntax statement)
        {
            if (statement is IfStatementSyntax ifStatement)
            {
                var conditionExpression = ifStatement.Condition as BinaryExpressionSyntax;
                var checkedVariables = CheckBinaryExpression(conditionExpression, SyntaxKind.EqualsEqualsToken);
                return checkedVariables;
            }

            if (statement is LocalDeclarationStatementSyntax declarationStatement)
            {
                return declarationStatement.Declaration.Variables
                    .Where(x => x.Initializer != null)
                    .Select(x => x.Initializer.Value)
                    .OfType<BinaryExpressionSyntax>()
                    .Select(ProcessBinaryIfCoalesce);
            }

            if (statement is ExpressionStatementSyntax expressionStatement)
            {
                if (expressionStatement.Expression is AssignmentExpressionSyntax assignmentExpression)
                {
                    if (assignmentExpression.Right is BinaryExpressionSyntax binaryExpression)
                    {
                        return new[] {ProcessBinaryIfCoalesce(binaryExpression)};
                    }
                }

                if (expressionStatement.Expression is InvocationExpressionSyntax invocationStatement)
                {
                    if (!(invocationStatement.Expression is MemberAccessExpressionSyntax memberAccessSyntax))
                        return new List<string>();
                    if (memberAccessSyntax.Name.Identifier.Text != "Requires" ||
                        !(memberAccessSyntax.Expression is IdentifierNameSyntax memberIdentifier))
                        return new List<string>();

                    if (memberIdentifier.Identifier.Text == "Contract")
                    {
                        return invocationStatement.ArgumentList.Arguments
                            .Select(x => x.Expression)
                            .OfType<BinaryExpressionSyntax>()
                            .SelectMany(x => CheckBinaryExpression(x, SyntaxKind.ExclamationEqualsToken));
                    }
                }
            }

            return new List<string>();
        }

        private static string ProcessBinaryIfCoalesce(BinaryExpressionSyntax expression)
        {
            if (expression.IsKind(SyntaxKind.CoalesceExpression) 
                && expression.Left is IdentifierNameSyntax identifier)
            {
                return identifier.Identifier.ValueText;
            }

            return null;
        }

        private static List<string> CheckBinaryExpression(BinaryExpressionSyntax binaryExpression, SyntaxKind expectedKind)
        {
            if (binaryExpression.Left is IdentifierNameSyntax leftIdentifier 
                && binaryExpression.Right.IsKind(SyntaxKind.NullLiteralExpression)
                && binaryExpression.OperatorToken.IsKind(expectedKind))
            {
                return new List<string> {leftIdentifier.Identifier.Text};
            }

            if (binaryExpression.Left.IsKind(SyntaxKind.NullLiteralExpression) 
                && binaryExpression.Right is IdentifierNameSyntax rightIdentifier
                && binaryExpression.OperatorToken.IsKind(expectedKind))
            {
                return new List<string> { rightIdentifier.Identifier.Text };
            }

            if (binaryExpression.Left is BinaryExpressionSyntax leftBinaryExpression
                && binaryExpression.Right is BinaryExpressionSyntax rightBinaryExpression)
            {
                var leftResult = CheckBinaryExpression(leftBinaryExpression, expectedKind);
                var rightResult = CheckBinaryExpression(rightBinaryExpression, expectedKind);
                return leftResult.Union(rightResult).ToList();
            }

            return new List<string>();
        }

        private static IEnumerable<string> GetReferenceParameters(ConstructorDeclarationSyntax constructor, SemanticModel semanticModel)
        {
            var parameterList = constructor.ParameterList;

            var symbols = semanticModel.LookupSymbols(parameterList.GetLocation().SourceSpan.Start);
            var namedTypeSymbols = symbols
                .Where(x => x.Kind == SymbolKind.NamedType)
                .OfType<INamedTypeSymbol>()
                .ToImmutableDictionary(x => x.MetadataName, x => x.IsReferenceType);

            var allParameterNames = parameterList.Parameters
                .Select(x => (shouldCheck: CheckNecessity(x.Type, namedTypeSymbols), paramName: x.Identifier.ValueText));

            return allParameterNames
                .Where(x => x.shouldCheck)
                .Select(x => x.paramName);
        }

        private static bool ShouldCheckPredefinedTypeForNull(string typeName)
        {
            if (typeName == "object" || typeName == "string")
            {
                return true;
            }

            return false;
        }

        private static bool ShouldCheckNamedTypeForNull(string typeName, IReadOnlyDictionary<string, bool> symbols)
        {
            if (symbols.TryGetValue(typeName, out var isReference))
            {
                return isReference;
            }
            return false;
        }

        private static bool ShouldCheckGenericTypeForNull(string typeName, int arity, IReadOnlyDictionary<string, bool> symbols)
        {
            var metadataName = $"{typeName}`{arity}";

            return ShouldCheckNamedTypeForNull(metadataName, symbols);
        }

        private static bool CheckNecessity(TypeSyntax typeSyntax, IReadOnlyDictionary<string, bool> symbols)
        {
            if (typeSyntax is PredefinedTypeSyntax ts)
            {
                var keyword = ts.Keyword.Text;
                return ShouldCheckPredefinedTypeForNull(keyword);
            }

            if (typeSyntax is GenericNameSyntax gs)
            {
                var typeName = gs.Identifier.Text;

                return ShouldCheckGenericTypeForNull(typeName, gs.Arity, symbols);
            }

            if (typeSyntax is IdentifierNameSyntax ins)
            {
                var typeName = ins.Identifier.Text;
                return ShouldCheckNamedTypeForNull(typeName, symbols);
            }

            if (typeSyntax is NullableTypeSyntax)
            {
                return false;
            }

            return false;
        }
    }
}
