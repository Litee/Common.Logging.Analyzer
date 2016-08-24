using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Common.Logging.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CommonLoggingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CommonLoggingAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var memberAccessExpr = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberAccessExpr?.Name.ToString() != "GetCurrentClassLogger") return;

            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol as IMethodSymbol;
            if (!memberSymbol?.ToString().StartsWith("Common.Logging.LogManager.GetCurrentClassLogger") ?? true) return;

            var diagnostic = Diagnostic.Create(Rule, invocationExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
