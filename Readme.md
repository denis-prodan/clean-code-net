# Clean Code .NET
Clean Code .NET is a set of Roslyn analyzers aimed to help developers build more correct and readable code.
At this moment it contains 3 analyzers (hope to improve their count in future):

## Switch analyzer
Ensures that all possible cases are covered in ```switch``` statement for enums and pattern matching.
Consider example:

```csharp
enum TestEnum
{
    Case1,
    Case2,
    Case3
}
    
switch (TestEnum.Case1)
{
    case Test.TestEnum.Case1: { break; }
    case Test.TestEnum.Case2: { break; }
    default: throw new NotImplementedException();
}
```
Will give a warning, because:
1. Intentionally specified that default case is not a normal case (throws exception).
2. Not all possible cases are covered (TestEnum.Case3).
It will allow developer to not miss handling of newly added enum values.

## Constructor null analyzer
Checks if constructor requires all reference type parameters to be not null and provide fixers for it:

```csharp
public Program(string test1, object test2, TestStruct<string> test3, string test4)
{
}
```
This example will be transformed into:
```csharp
public Program(string test1, object test2, TestStruct<string> test3, string test4)
{
    if (test1 == null)
        throw new ArgumentNullException(nameof(test1));
    if (test2 == null)
        throw new ArgumentNullException(nameof(test2));
    if (test4 == null)
        throw new ArgumentNullException(nameof(test4));
}
```
Various options are provided: if-check, if + assignment coalesce (``` ?? throw new ArgumentNullException(nameof(param)) ```) where possible and ```Contract.Requires```

## Named parameters analyzer
Ensures if method/constructor calls has 4 or more parameters to have parameter names.
For example:
```csharp
Method("Foo", "Bar", "Baz", "Qux");
```
Should be:
```csharp
Method(foo: "Foo", bar: "Bar", baz: "Baz", qux: "Qux");
```

## How to use
There are 2 options:
- Install nuget package to project(s) https://www.nuget.org/packages/CleanCodeNet. This is metapackage, you can install any of analyzers separately
- Install Visual Studio extension https://marketplace.visualstudio.com/items?itemName=denis-prodan.clean-code-net