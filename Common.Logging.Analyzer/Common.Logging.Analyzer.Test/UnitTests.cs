using ApprovalTests;
using ApprovalTests.Core;
using ApprovalTests.Reporters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Common.Logging.Analyzer.Test
{
    [TestClass]
    [UseReporter(typeof(DiffReporter))]
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
                Id = "CommonLoggingAnalyzer100",
                Message = "Replace GetCurrentClassLogger() with GetLogger<T>() or GetLogger(typeof(T)) for better peformance",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 39)
                        }
            };

            VerifyCSharpDiagnostic(analysisTest, expected);

            Approvals.Verify(VerifyCSharpFix(analysisTest, allowNewCompilerDiagnostics: true));
        }

        [TestMethod]
        public void ShouldFixGetCurrentClassLoggerMethod_2()
        {
            var analysisTest = @"
    using System;
    using Common.Logging;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            ILog Log = LogManager.GetCurrentClassLogger();
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "CommonLoggingAnalyzer100",
                Message = "Replace GetCurrentClassLogger() with GetLogger<T>() or GetLogger(typeof(T)) for better peformance",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 24)
                        }
            };

            VerifyCSharpDiagnostic(analysisTest, expected);

            var quickFixTest = @"
    using System;
    using Common.Logging;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            ILog Log = LogManager.GetLogger<MyClass>();
        }
    }";
            Approvals.Verify(VerifyCSharpFix(analysisTest, allowNewCompilerDiagnostics: true));
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
                Id = "CommonLoggingAnalyzer100",
                Message = "Replace GetCurrentClassLogger() with GetLogger<T>() or GetLogger(typeof(T)) for better peformance",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 39)
                        }
            };

            VerifyCSharpDiagnostic(analysisTest, expected);
            Approvals.Verify(VerifyCSharpFix(analysisTest, allowNewCompilerDiagnostics: true));
        }


        [TestMethod]
        public void ShouldPassIfTypeNameIsCorrect()
        {
            var analysisTest = @"
    using System;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            Common.Logging.ILog Log1 = Common.Logging.LogManager.GetLogger<MyClass>();
            Common.Logging.ILog Log2 = Common.Logging.LogManager.GetLogger(typeof(MyClass));
        }
    }";

            VerifyCSharpDiagnostic(analysisTest);
        }

        [TestMethod]
        public void ShouldFailIfTypeNameIsWrong()
        {
            var analysisTest1 = @"
    using System;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger<SomeOtherClass>();
        }
    }";
            var analysisTest2 = @"
    using System;

    namespace ConsoleApplication1
    {
        class MyClass
        {
            Common.Logging.ILog Log = Common.Logging.LogManager.GetLogger(typeof(SomeOtherClass));
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "CommonLoggingAnalyzer101",
                Message = "Type T in LogManager.GetLogger<T>() call should match the class in which method is called: MyClass",
                Severity = DiagnosticSeverity.Warning,
                Locations =
        new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 39)
            }
            };

            VerifyCSharpDiagnostic(analysisTest1, expected);
            VerifyCSharpDiagnostic(analysisTest2, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CommonLoggingAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CommonLoggingAnalyzer();
        }
    }

}