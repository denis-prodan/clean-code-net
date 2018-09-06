# Switch-analyzer
C# analyzer for non-exhaustive cases in switch with enums.
Verifies that switch statement checks all existing enum values if case if there is no **default** branch or it throws any *Exception*.

For code:
```C#
enum TestEnum
{
    Case1,
    Case2,
    Case3
}

class TestClass
{
    public TestEnum TestMethod()
    {
        switch (TestEnum.Case1)
        {
            case TestEnum.Case1: return TestEnum.Case2;
            case TestEnum.Case2: return TestEnum.Case1;
            default: throw new NotImplementedException();
        }
    }
}
```
    
You will get warning, because **TestEnum.Case3** is not covered in this switch statement.
At this moment analyzer should support common cases:
* Bitwise operators (& and |).
* Parentnesis.
* Function call as switch argument.

New version has support for Interface implementations and base class inheritors checks in switch-case with pattern matching. 
Also, treats "var" case as intended behavior and performs no checks in case if it is present.

You can find more cases in unit tests