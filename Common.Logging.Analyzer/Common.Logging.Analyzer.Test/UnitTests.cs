using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Common.Logging.Analyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        [TestMethod]
        public void ShouldPassIfFastMethodIsUsed()
        {
            var analysisTest = @"
    using System;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger<MyClass>();
        }
    }";

            VerifyCSharpDiagnostic(analysisTest);
        }

        [TestMethod]
        public void ShouldFixGetCurrentClassLoggerMethod()
        {
            var analysisTest = @"
    using System;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            Common.Logging.ILog Log = Common.Logging.LogManager.GetCurrentClassLogger();
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "CommonLoggingAnalyzer",
                Message = "Do not use GetCurrentClassLogger() method - use GetLogger<> instead",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 39)
                        }
            };

            VerifyCSharpDiagnostic(analysisTest, expected);

            var quickFixTest = @"
    using System;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger<MyClass>();
        }
    }";
            VerifyCSharpFix(analysisTest, quickFixTest, allowNewCompilerDiagnostics: true);
        }

        [TestMethod]
        public void ShouldFixGetCurrentClassLoggerMethodInStaticClass()
        {
            var analysisTest = @"
    using System;

    namespace ConsoleApplication1
    {
        static class MyClass
        {
            Common.Logging.ILog Log = Common.Logging.LogManager.GetCurrentClassLogger();
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "CommonLoggingAnalyzer",
                Message = "Do not use GetCurrentClassLogger() method - use GetLogger<> instead",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 39)
                        }
            };

            VerifyCSharpDiagnostic(analysisTest, expected);

            var quickFixTest = @"
    using System;

    namespace ConsoleApplication1
    {
        static class MyClass
        {
            Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger(typeof(MyClass));
        }
    }";
            VerifyCSharpFix(analysisTest, quickFixTest, allowNewCompilerDiagnostics: true);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CommonLoggingAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CommonLoggingAnalyzerAnalyzer();
        }
    }
}