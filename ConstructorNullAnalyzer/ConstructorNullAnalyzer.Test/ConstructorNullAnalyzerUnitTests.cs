using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestCommon;

namespace ConstructorNullAnalyzer.Test
{
    [TestClass]
    public class ConstructorNullAnalyzerTest : CodeFixVerifier
    {
        private const string CodeWrapper = 
@"using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{{
    class TypeName{2}
    {{{0}
    }}
    {1}
}}";

        //No diagnostics expected to show up
        [TestMethod]
        public void EmptyIsValid()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void FixForMixedType()
        {
            var constructorDeclaration = @"
        public TypeName(string param1, int param2, String param3, int? param4)
        {
        }";

            var test = BuildDocumentCode(constructorDeclaration);
            var expected = new DiagnosticResult
            {
                Id = ConstructorNullAnalyzer.DiagnosticId,
                Message = string.Format("Constructor should check that parameter(s) {0} are not null", "param1, param3"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 16),
                        new DiagnosticResultLocation("Test0.cs", 12, 32),
                        new DiagnosticResultLocation("Test0.cs", 12, 59)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var newConstructor = @"
        public TypeName(string param1, int param2, String param3, int? param4)
        {
            if (param1 == null)
                throw new ArgumentNullException(nameof(param1));
            if (param3 == null)
                throw new ArgumentNullException(nameof(param3));
        }";
            var fixtest = BuildDocumentCode(newConstructor);

            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void FixerAddsUsingIfMissed()
        {
            var constructorDeclaration = @"
        public TypeName(string param1)
        {
        }";

            var test = BuildDocumentCode(constructorDeclaration);
            var testNoUsing = test.Replace("using System;", "");
            var expected = new DiagnosticResult
            {
                Id = ConstructorNullAnalyzer.DiagnosticId,
                Message = string.Format("Constructor should check that parameter(s) {0} are not null", "param1"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 16),
                        new DiagnosticResultLocation("Test0.cs", 12, 32)
                    }
            };

            VerifyCSharpDiagnostic(testNoUsing, expected);

            var newConstructor = @"
        public TypeName(string param1)
        {
            if (param1 == null)
                throw new ArgumentNullException(nameof(param1));
        }";
            var fixtest = BuildDocumentCode(newConstructor);

            VerifyCSharpFix(testNoUsing, fixtest);
        }

        [TestMethod]
        public void NoChecksIfCoalesceCheck()
        {
            var constructorDeclaration = @"
        private readonly string param2;
        public TypeName(string param1, string param2)
        {
            var s = param1 ?? throw new ArgumentException(nameof(param1));
            this.param2 = param2 ?? throw new ArgumentException(nameof(param2));
        }";

            var test = BuildDocumentCode(constructorDeclaration);

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void SimpleAssignmentsThrowsError()
        {
            var constructorDeclaration = @"
        public TypeName(string param1)
        {
            var s = param1;
        }";

            var test = BuildDocumentCode(constructorDeclaration);

            var expected = new DiagnosticResult
            {
                Id = ConstructorNullAnalyzer.DiagnosticId,
                Message = string.Format("Constructor should check that parameter(s) {0} are not null", "param1"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 16),
                        new DiagnosticResultLocation("Test0.cs", 12, 32),
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void ParamsPassedToBaseAreNotVerified()
        {
            var constructorDeclaration = @"
        public TypeName(string param1, string baseParam): base(baseParam)
        {
            var s = param1;
        }";
            var baseClass = @"
    public class BaseType
    {
        public BaseType(string param1)
        {
            if (param1 == null)
                throw new ArgumentNullException(nameof(param1));
        }
    }";

            var test = BuildDocumentCode(constructorDeclaration, baseClass, ": BaseType");

            var expected = new DiagnosticResult
            {
                Id = ConstructorNullAnalyzer.DiagnosticId,
                Message = string.Format("Constructor should check that parameter(s) {0} are not null", "param1"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 16),
                        new DiagnosticResultLocation("Test0.cs", 12, 32),
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void ContractRequiresValid()
        {
            var constructorDeclaration = @"
        public TypeName(string param1)
        {
            Contract.Requires(param1 != null);
        }";

            var test = BuildDocumentCode(constructorDeclaration);

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ContractFixer()
        {
            var constructorDeclaration = @"
        public TypeName(string param1, string param2)
        {
            field1 = param1;
            var s = param2;
        }

        private string field1;";

            var test = BuildDocumentCode(constructorDeclaration);

            var expected = new DiagnosticResult
            {
                Id = ConstructorNullAnalyzer.DiagnosticId,
                Message = string.Format("Constructor should check that parameter(s) {0} are not null", "param1, param2"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 16),
                        new DiagnosticResultLocation("Test0.cs", 12, 32),
                        new DiagnosticResultLocation("Test0.cs", 12, 47),
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var newConstructor = @"
        public TypeName(string param1, string param2)
        {
            Contract.Requires(param1 != null);
            Contract.Requires(param2 != null);
            field1 = param1;
            var s = param2;
        }

        private string field1;";
            var fixtest = BuildDocumentCode(newConstructor);
            var fixtestWithNewUsing = fixtest.Replace("using System.Diagnostics;",
@"using System.Diagnostics;
using System.Diagnostics.Contracts;");

            VerifyCSharpFix(test, fixtestWithNewUsing, 4);
        }

        [TestMethod]
        public void MixedIfCoalesceFixer()
        {
            var constructorDeclaration = @"
        public TypeName(string param1, string param2, string param3)
        {
            field1 = param1;
            var s = param2;
        }

        private string field1;";

            var test = BuildDocumentCode(constructorDeclaration);

            var expected = new DiagnosticResult
            {
                Id = ConstructorNullAnalyzer.DiagnosticId,
                Message = string.Format("Constructor should check that parameter(s) {0} are not null", "param1, param2, param3"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 16),
                        new DiagnosticResultLocation("Test0.cs", 12, 32),
                        new DiagnosticResultLocation("Test0.cs", 12, 47),
                        new DiagnosticResultLocation("Test0.cs", 12, 62),
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var newConstructor = @"
        public TypeName(string param1, string param2, string param3)
        {
            if (param3 == null)
                throw new ArgumentNullException(nameof(param3));
            field1 = param1 ?? throw new ArgumentNullException(nameof(param1));
            var s = param2 ?? throw new ArgumentNullException(nameof(param2));
        }

        private string field1;";
            var fixtest = BuildDocumentCode(newConstructor);

            VerifyCSharpFix(test, fixtest, 2);
        }

        [TestMethod]
        public void MultipleDeclarationVerifiedAndCoaleseFixed()
        {
            var constructorDeclaration = @"
        public TypeName(string param1)
        {
            string s, k = param1;
        }

        private string field1;";

            var test = BuildDocumentCode(constructorDeclaration);

            var expected = new DiagnosticResult
            {
                Id = ConstructorNullAnalyzer.DiagnosticId,
                Message = string.Format("Constructor should check that parameter(s) {0} are not null", "param1"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 16),
                        new DiagnosticResultLocation("Test0.cs", 12, 32)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var newConstructor = @"
        public TypeName(string param1)
        {
            string s, k = param1 ?? throw new ArgumentNullException(nameof(param1));
        }

        private string field1;";
            var fixtest = BuildDocumentCode(newConstructor);

            VerifyCSharpFix(test, fixtest, 2);
        }

        [TestMethod]
        public void GenericValidatedAndFixed()
        {
            var constructorDeclaration = @"
        public TypeName(List<string> param1)
        {
        }";

            var test = BuildDocumentCode(constructorDeclaration);

            var expected = new DiagnosticResult
            {
                Id = ConstructorNullAnalyzer.DiagnosticId,
                Message = string.Format("Constructor should check that parameter(s) {0} are not null", "param1"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[]
                    {
                        new DiagnosticResultLocation("Test0.cs", 12, 16),
                        new DiagnosticResultLocation("Test0.cs", 12, 38),
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var newConstructor = @"
        public TypeName(List<string> param1)
        {
            if (param1 == null)
                throw new ArgumentNullException(nameof(param1));
        }";
            var fixtest = BuildDocumentCode(newConstructor);

            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void GenericStructNoError()
        {
            var constructorDeclaration = @"
        public TypeName(TestStruct<string> param1)
        {
        }";
            var genericStruct = @"
        public struct TestStruct<T>
        {
        }";

            var test = BuildDocumentCode(constructorDeclaration, genericStruct);

            VerifyCSharpDiagnostic(test);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ConstructorNullAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ConstructorNullAnalyzer();
        }

        private string BuildDocumentCode(string constructor, string addition = "", string baseDeclaration = "") => string.Format(CodeWrapper, constructor, addition, baseDeclaration);
    }
}
