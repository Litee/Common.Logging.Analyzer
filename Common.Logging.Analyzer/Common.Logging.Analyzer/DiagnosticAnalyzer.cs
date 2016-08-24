using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Common.Logging.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CommonLoggingAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Naming";

        public const string DiagnosticId100 = "CommonLoggingAnalyzer100";
        public const string DiagnosticId101 = "CommonLoggingAnalyzer101";

        private static readonly LocalizableString Title100 = new LocalizableResourceString(nameof(Resources.Analyzer100Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat100 = new LocalizableResourceString(nameof(Resources.Analyzer100MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description100 = new LocalizableResourceString(nameof(Resources.Analyzer100Description), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString Title101 = new LocalizableResourceString(nameof(Resources.Analyzer101Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat101 = new LocalizableResourceString(nameof(Resources.Analyzer101MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description101 = new LocalizableResourceString(nameof(Resources.Analyzer101Description), Resources.ResourceManager, typeof(Resources));

        private static DiagnosticDescriptor DoNotUseGetCurrentClassLoggerMethodRule = new DiagnosticDescriptor(DiagnosticId100, Title100, MessageFormat100, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description100);
        private static DiagnosticDescriptor GetLoggerMethodTypeParameterMustMatchCurrentClassRule = new DiagnosticDescriptor(DiagnosticId101, Title101, MessageFormat101, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description101);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(DoNotUseGetCurrentClassLoggerMethodRule, GetLoggerMethodTypeParameterMustMatchCurrentClassRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(WarnIfGetCurrentClassLoggerIsUsed, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(WarnIfTypeNameIsWrongInGetLogger, SyntaxKind.InvocationExpression);
        }

        private static void WarnIfGetCurrentClassLoggerIsUsed(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var memberAccessExpr = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberAccessExpr?.Name.ToString() != "GetCurrentClassLogger") return;

            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol as IMethodSymbol;
            if (!memberSymbol?.ToString().StartsWith("Common.Logging.LogManager.GetCurrentClassLogger") ?? true) return;

            var diagnostic = Diagnostic.Create(DoNotUseGetCurrentClassLoggerMethodRule, invocationExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static void WarnIfTypeNameIsWrongInGetLogger(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var memberAccessExpression = invocationExpression.Expression as MemberAccessExpressionSyntax;
            var methodName = memberAccessExpression?.Name;
            if (methodName?.Identifier.ToString() != "GetLogger") return;

            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol as IMethodSymbol;
            if (!memberSymbol?.ToString().StartsWith("Common.Logging.LogManager.GetLogger") ?? true) return;

            var typeDeclaration = invocationExpression.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();
            var typeName = typeDeclaration.Identifier.ToString();

            if (methodName is GenericNameSyntax) { // GetLogger<T>()
                var typeArguments = ((GenericNameSyntax)methodName).TypeArgumentList.Arguments;
                if (typeArguments.Any(x => x.ToString() == typeName)) return;
            }
            else // GetLogger(Type)
            {
                var typeArguments = invocationExpression.ArgumentList.Arguments;
                if (!typeArguments.Any()) return;
                var typeOfExp = typeArguments[0].Expression as TypeOfExpressionSyntax;
                if (typeOfExp == null || typeOfExp.Type.ToString() == typeName) return;
            }

            var diagnostic = Diagnostic.Create(GetLoggerMethodTypeParameterMustMatchCurrentClassRule, invocationExpression.GetLocation(), typeName);
            context.ReportDiagnostic(diagnostic);
        }

    }
}
