using FluentAssertions;
using Xunit;

namespace PeruShopHub.UnitTests;

public class SmokeTest
{
    [Fact]
    public void UnitTest_Infrastructure_IsConfigured()
    {
        // Verifies xUnit + FluentAssertions are wired correctly
        true.Should().BeTrue();
    }
}
