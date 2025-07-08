using Xunit;

namespace AuthService.Tests;

public class MainTest
{
    [Fact]
    public void Test1()
    {
        var hello = "hello World";
        Assert.Equal("hello World", hello);
    }
}