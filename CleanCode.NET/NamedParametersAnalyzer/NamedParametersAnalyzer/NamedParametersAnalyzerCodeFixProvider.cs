using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace NamedParametersAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamedParametersCodeFixProvider)), Shared]
    public class NamedParametersCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add parameter name(s)";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NamedParametersAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var fixerMethod = GetFixerMethod(context, root, diagnosticSpan);

            if (fixerMethod == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedSolution: fixerMethod,
                    equivalenceKey: Title),
                diagnostic);
        }

        private Func<CancellationToken, Task<Solution>> GetFixerMethod(CodeFixContext context, SyntaxNode root, TextSpan location)
        {
            var nodes = root.FindToken(location.Start).Parent.AncestorsAndSelf().ToArray();

            var invocation = nodes
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (invocation != null)
            {
                return cancellationToken => UpdateInvocationArguments(context, invocation, cancellationToken);
            }

            var creation = nodes
                .OfType<ObjectCreationExpressionSyntax>()
                .FirstOrDefault();

            if (creation != null)
            {
                return cancellationToken => UpdateCreationArguments(context, creation, cancellationToken);
            }

            return null;
        }

        private async Task<Solution> UpdateInvocationArguments(CodeFixContext context, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var declaration = await GetDeclarationNode(context, invocation, cancellationToken) as MethodDeclarationSyntax;

            var updatedArguments = GetUpdatedArguments(invocation.ArgumentList, declaration.ParameterList);
            var newInvocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(updatedArguments)));

            var documentEditor = await DocumentEditor.CreateAsync(context.Document, cancellationToken);
            documentEditor.ReplaceNode(invocation, newInvocation);
            var newDocument = documentEditor.GetChangedDocument();

            return newDocument.Project.Solution;
        }

        private async Task<Solution> UpdateCreationArguments(CodeFixContext context, ObjectCreationExpressionSyntax creation, CancellationToken cancellationToken)
        {
            var declaration = await GetDeclarationNode(context, creation, cancellationToken) as ConstructorDeclarationSyntax;

            var updatedArguments = GetUpdatedArguments(creation.ArgumentList, declaration.ParameterList);
            var newCreation = creation.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(updatedArguments)));

            var documentEditor = await DocumentEditor.CreateAsync(context.Document, cancellationToken);
            documentEditor.ReplaceNode(creation, newCreation);
            var newDocument = documentEditor.GetChangedDocument();

            return newDocument.Project.Solution;
        }

        private async Task<SyntaxNode> GetDeclarationNode(CodeFixContext context, ExpressionSyntax call, CancellationToken cancellationToken)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken);
            var invocationModel = semanticModel
                .GetSymbolInfo(call, context.CancellationToken)
                .Symbol as IMethodSymbol;

            var syntaxReference = invocationModel.DeclaringSyntaxReferences.FirstOrDefault();
            var declaration = syntaxReference.GetSyntax(context.CancellationToken);
            return declaration;
        }

        private IEnumerable<ArgumentSyntax> GetUpdatedArguments(ArgumentListSyntax arguments,
            ParameterListSyntax parametersList)
        {
            var argumentsAndParams = parametersList.Parameters.Zip(arguments.Arguments, 
                (param, arg) => (arg: arg, param: param));

            var updatedArguments = argumentsAndParams.Select(x => FixArgumentIfNeeded(x.arg, x.param));

            return updatedArguments;
        }

        private ArgumentSyntax FixArgumentIfNeeded(ArgumentSyntax argument, ParameterSyntax param)
        {
            if (argument.NameColon != null)
                return argument;

            return argument.WithNameColon(SyntaxFactory.NameColon(param.Identifier.Text));
        }

    }
}
