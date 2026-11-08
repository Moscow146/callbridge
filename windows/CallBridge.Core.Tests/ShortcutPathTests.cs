using CallBridge.Core.Startup;

namespace CallBridge.Core.Tests;

public class ShortcutPathTests
{
	[Fact]
	public void ShortcutGetsTheLnkExtensionInTheGivenFolder()
	{
		var path = ShortcutPath.In(Path.Combine("C:", "Users", "Me", "Desktop"), "CallBridge");
		Assert.Equal(Path.Combine("C:", "Users", "Me", "Desktop", "CallBridge.lnk"), path);
	}

	[Fact]
	public void ExtensionIsLnk()
	{
		Assert.EndsWith(".lnk", ShortcutPath.In("x", "Name"));
	}
}
