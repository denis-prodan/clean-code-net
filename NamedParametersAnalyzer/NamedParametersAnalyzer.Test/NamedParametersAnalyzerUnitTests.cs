using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestCommon;

namespace NamedParametersAnalyzer.Test
{
    [TestClass]
    public class NamedParameterAnalyzerTests : CodeFixVerifier
    {

        private string code = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{{
    class TypeName
    {{
        public TypeName(){0}

        public TypeName(string param1, string param2, string param3, string param4)
        {{
        }}

        public void Method1(string param1, string param2, string param3, string param4)
        {{
        }}

        public void Method1(string param1, string param2, string param3)
        {{
        }}
    }}
}}";

        [TestMethod]
        public void EmptyCode()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void SimpleCheck()
        {
            var call = @"
        {
            Method1(""param1"", param2: ""param2"", param3: ""param3"", param4: ""param4"");
        }";
            var test = GetCode(call);
            var expected = new DiagnosticResult
            {
                Id = NamedParametersAnalyzer.DiagnosticId,
                Message = NamedParametersAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] { new DiagnosticResultLocation("Test0.cs", 15, 13) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void ConstructorCall()
        {
            var call = @"
        {
            TypeName(""param1"", param2: ""param2"", param3: ""param3"", param4: ""param4"");
        }";
            var test = GetCode(call);
            var expected = new DiagnosticResult
            {
                Id = NamedParametersAnalyzer.DiagnosticId,
                Message = NamedParametersAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] { new DiagnosticResultLocation("Test0.cs", 15, 13) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }
        [TestMethod]
        public void ExpressionBodied()
        {
            var call = @" => TypeName(""param1"", param2: ""param2"", param3: ""param3"", param4: ""param4"");";
            var test = GetCode(call);
            var expected = new DiagnosticResult
            {
                Id = NamedParametersAnalyzer.DiagnosticId,
                Message = NamedParametersAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] { new DiagnosticResultLocation("Test0.cs", 13, 30) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void LessThan4Parameters()
        {
            var call = @"
        {
            Method1(""param1"", param2: ""param2"", param3: ""param3"");
        }";
            var test = GetCode(call);

            VerifyCSharpDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new NamedParametersCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NamedParametersAnalyzer();
        }

        private string GetCode(string callToTest) => string.Format(code, callToTest);
    }
}
