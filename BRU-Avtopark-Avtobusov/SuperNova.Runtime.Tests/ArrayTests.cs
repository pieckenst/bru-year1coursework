using SuperNova.Runtime.Interpreter;

namespace SuperNova.Runtime.Tests;

public class ArrayTests : BaseVBTestFixture
{
    [Test]
    public async Task StringArray_Get_Set()
    {
        await Run(@"Dim Arr(10) As String
Arr(0) = ""Hello""
Debug.Print Arr(0)");
        AssertDebugLog(["Hello"]);
    }

    [Test]
    public async Task StringArray_Get_Set_Base10()
    {
        await Run(@"Dim Arr(10 To 20) As String
Arr(20) = ""Hello""
Debug.Print Arr(20)");
        AssertDebugLog(["Hello"]);
    }

    [Test]
    public async Task StringArray_Get_Set_Two_Dim()
    {
        await Run(@"Dim Arr(10, 20) As String
Arr(0, 1) = ""Hello""
Debug.Print Arr(0, 1)");
        AssertDebugLog(["Hello"]);
    }

    [Test]
    public async Task StringArray_Set_Two_Dims_OutOfBounds()
    {
        var ex = (VBRunTimeException)Assert.ThrowsAsync<VBRunTimeException>(async () => await Run(@"Dim Arr(10, 20) As String
Arr(0, 21) = ""Hello"""));
        Assert.AreEqual(VBStandardError.SubscriptOutOfRange.ErrNo, ex.Error.ErrNo);
    }

    [Test]
    public async Task StringArray_Get_Two_Dims_OutOfBounds()
    {
        var ex = (VBRunTimeException)Assert.ThrowsAsync<VBRunTimeException>(async () => await Run(@"Dim Arr(10, 20) As String
Debug.Print Arr(0, 21)"));
        Assert.AreEqual(VBStandardError.SubscriptOutOfRange.ErrNo, ex.Error.ErrNo);
    }

    [Test]
    [TestCase(0)]
    [TestCase(1)]
    public async Task Array_Bound_OptionBase(int @base)
    {
        await Run($"Option Base {@base}" + @"
Dim Arr(20) As String
Debug.Print LBound(Arr)
Debug.Print UBound(Arr)
");
        AssertDebugLog([@base, 20]);
    }

    [Test]
    public async Task UndefinedSize_Array()
    {
        await Run(@"Dim Arr() As String");
    }

    [Test]
    public async Task StringArray_ReDim()
    {
        await Run(@"Dim Arr()
ReDim Arr(10) As String
Arr(0) = ""Hello""
Debug.Print Arr(0)");
        AssertDebugLog(["Hello"]);
    }
}