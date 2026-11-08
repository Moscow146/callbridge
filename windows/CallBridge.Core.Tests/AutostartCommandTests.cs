using CallBridge.Core.Startup;

namespace CallBridge.Core.Tests;

public class AutostartCommandTests
{
	[Fact]
	public void CommandQuotesThePathAndStartsMinimized()
	{
		var command = AutostartCommand.Build(@"C:\Program Files\CallBridge\CallBridge.exe");
		Assert.Equal("\"C:\\Program Files\\CallBridge\\CallBridge.exe\" --minimized", command);
	}

	[Fact]
	public void MinimizedArgumentIsRecognised()
	{
		Assert.True(AutostartCommand.StartsMinimized(["--minimized"]));
		Assert.True(AutostartCommand.StartsMinimized(["--theme", "dark", "--MINIMIZED"]));
	}

	[Fact]
	public void OtherArgumentsStartVisible()
	{
		Assert.False(AutostartCommand.StartsMinimized([]));
		Assert.False(AutostartCommand.StartsMinimized(["--theme", "dark"]));
		Assert.False(AutostartCommand.StartsMinimized(["--minimized-not"]));
	}
}
