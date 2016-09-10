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
                CodeAction.Create(title: title, createChangedDocument: c => ReplaceMethodCallWithProperOne(root, context.Document, declaration, c), equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> ReplaceMethodCallWithProperOne(SyntaxNode root, Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var typeDeclaration = invocationExpression.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
            var typeName = typeDeclaration.Identifier.ToString();

            var memberAccessExpr = invocationExpression.Expression as MemberAccessExpressionSyntax;

            ExpressionSyntax newInvocationExpression;
            if (typeDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.StaticKeyword))) {
                //newInvocationExpression = SyntaxFactory.ParseExpression("Common.Logging.LogManager.GetLogger(typeof(" + typeName + "))");
                newInvocationExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("LogManager"),
                        SyntaxFactory.IdentifierName("GetLogger")
                    ),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName(typeName)))))
                );
            }
            else
            {
                newInvocationExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("LogManager"),
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("GetLogger"), SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.IdentifierName(typeName))))
                    )
                );
            }
            var newRoot = root.ReplaceNode(invocationExpression, newInvocationExpression);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}