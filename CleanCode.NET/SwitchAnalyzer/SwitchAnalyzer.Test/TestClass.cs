using System;


namespace SwitchAnalyzer.OtherNamespace
{
    enum OtherEnum
    {
        Case1,
        Case2,
        Case3
    }
}
namespace SwitchAnalyzer.Test
{
    enum TestEnum
    {
        Case1,
        Case2,
        Case3
    }

    class TestClass : TestClass.ITestInterface
    {

        public TestClass(string foo)
        {
            Foo = foo;
        }
        
        public string Foo { get; set; }

        public void TestMethod1()
        {
            var s = OtherNamespace.OtherEnum.Case1;

            switch (s)
            {
                case OtherNamespace.OtherEnum.Case2: { break; }
            }
        }

        public TestEnum TestMethod2()
        {
            var s = TestEnum.Case1;

            switch (GetEnum(s))
            {
                case Test.TestEnum.Case1 & TestEnum.Case1: { break; }
                case Test.TestEnum.Case2: { break; }
                default: { break; }
            }

            return TestEnum.Case1;
        }

        public TestEnum TestMethod3()
        {
            var s = new TestClass("");

            switch (s)
            {
                case TestClass a when a.Foo == "Test" && a.Foo == "Zoo": return TestEnum.Case1;
                case var inter: return TestEnum.Case2;
                //default: { break; }
            }
        }

        public TestEnum TestMethod4()
        {
            BaseClass s = new TestClass2();

            switch (s)
            {
                case TestClass2 a when a.Foo == "Test" && a.Bar == 1: return TestEnum.Case1;
                case TestClass3 b when b.Foo == "Test" && b.Baz == 0.1: return TestEnum.Case2;
                case BaseClass b: return TestEnum.Case3;
                case var inter: return TestEnum.Case3;
            }
        }
        public interface ITestInterface
        {
        }

        public class NotImplementedExceptionInheritor : NotImplementedException
        {
        }

        private TestEnum GetEnum(TestEnum enumValue)
        {
            return enumValue;
        }
    }
}
