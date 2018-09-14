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

namespace ExceptionsAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExceptionsAnalyzerCodeFixProvider)), Shared]
    public class ExceptionsAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Replace with \"throw;\" statement";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Descriptors.RethrowSameExceptionId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var throwStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ThrowStatementSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedSolution: c => FixThrow(context.Document, throwStatement, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private async Task<Solution> FixThrow(Document document, ThrowStatementSyntax throwStatement, CancellationToken cancellationToken)
        {
            var newThrow = SyntaxFactory.ThrowStatement();
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            editor.ReplaceNode(throwStatement, newThrow);
            return editor.GetChangedDocument().Project.Solution;
        }
    }
}
