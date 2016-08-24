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

namespace Common.Logging.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommonLoggingAnalyzerCodeFixProvider)), Shared]
    public class CommonLoggingAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Replace with GetLogger<T>() call";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CommonLoggingAnalyzer.DiagnosticId100); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(title: title, createChangedDocument: c => MakeUppercaseAsync(root, context.Document, declaration, c), equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> MakeUppercaseAsync(SyntaxNode root, Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var typeDeclaration = invocationExpression.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
            var typeName = typeDeclaration.Identifier.ToString();

            var memberAccessExpr = invocationExpression.Expression as MemberAccessExpressionSyntax;

            // Get the symbol representing the type to be renamed.
            //var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            //var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            ExpressionSyntax newInvocationExpression;
            if (typeDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.StaticKeyword))) {
                newInvocationExpression = SyntaxFactory.ParseExpression("Common.Logging.LogManager.GetLogger(typeof(" + typeName + "))");
            }
            else
            {
                newInvocationExpression = SyntaxFactory.ParseExpression("Common.Logging.LogManager.GetLogger<" + typeName + ">()");
            }
            var newMethodName = SyntaxFactory.ParseName("GetLogger");
            var newRoot = root.ReplaceNode(invocationExpression, newInvocationExpression);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}