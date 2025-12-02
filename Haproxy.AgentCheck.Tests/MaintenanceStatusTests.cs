using Xunit;

namespace Lucca.Infra.Haproxy.AgentCheck.Tests;

public class MaintenanceStatusTests
{
    [Fact]
    public void MaintenanceStatus_DefaultValue_IsFalse()
    {
        var sut = new MaintenanceStatus();

        Assert.False(sut.IsMaintenance);
    }

    [Fact]
    public void MaintenanceStatus_SetToTrue_ReturnsTrue()
    {
        var sut = new MaintenanceStatus();

        sut.IsMaintenance = true;

        Assert.True(sut.IsMaintenance);
    }

    [Fact]
    public void MaintenanceStatus_SetToFalse_ReturnsFalse()
    {
        var sut = new MaintenanceStatus { IsMaintenance = true };

        sut.IsMaintenance = false;

        Assert.False(sut.IsMaintenance);
    }
}
