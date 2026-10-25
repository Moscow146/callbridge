using CallBridge.Core.Session;

namespace CallBridge.Core.Tests;

public class PairingInfoTests
{
	[Fact]
	public void UriCarriesAllFields()
	{
		var uri = PairingInfo.BuildUri("abc123", 47800, ["192.168.1.5", "10.0.0.2"], "My PC");
		Assert.Equal("callbridge://pair?h=192.168.1.5,10.0.0.2&p=47800&c=abc123&n=My%20PC", uri);
	}

	[Fact]
	public void GeneratedCodesAreDistinctHex()
	{
		var a = CallBridgeServer.GenerateCode();
		var b = CallBridgeServer.GenerateCode();
		Assert.Equal(16, a.Length);
		Assert.Matches("^[0-9a-f]{16}$", a);
		Assert.NotEqual(a, b);
	}
}
