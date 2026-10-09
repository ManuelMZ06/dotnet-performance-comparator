using DotNetPerformanceComparator.Application.Migrations.ExperimentalMigration;

namespace DotNetPerformanceComparator.Tests.Migrations;

public class DotNetSdkSelectorTests
{
    [Fact]
    public void SinSdk10_DevuelveNull()
    {
        var salida = new[] { "8.0.425 [C:\\Program Files\\dotnet\\sdk]", "9.0.300 [C:\\Program Files\\dotnet\\sdk]" };

        Assert.Null(DotNetSdkSelector.SelectLatestStable(salida, 10));
    }

    [Fact]
    public void UnaVersionEstable10_LaSelecciona()
    {
        var salida = new[] { "8.0.425 [C:\\Program Files\\dotnet\\sdk]", "10.0.400 [C:\\Program Files\\dotnet\\sdk]" };

        Assert.Equal("10.0.400", DotNetSdkSelector.SelectLatestStable(salida, 10));
    }

    [Fact]
    public void VariasVersiones10_SeleccionaLaMayorEstable()
    {
        var salida = new[] { "10.0.100 [x]", "10.0.400 [x]", "10.0.201 [x]", "10.1.100 [x]" };

        Assert.Equal("10.1.100", DotNetSdkSelector.SelectLatestStable(salida, 10));
    }

    [Fact]
    public void PreviewYEstable_SeleccionaLaEstable()
    {
        var salida = new[] { "10.0.100 [x]", "10.0.200-preview.3.25201.1 [x]", "10.0.300-rc.1.25451.107 [x]" };

        Assert.Equal("10.0.100", DotNetSdkSelector.SelectLatestStable(salida, 10));
    }

    [Fact]
    public void SoloPreview_NoSeleccionaNadaPeroLaDetecta()
    {
        var salida = new[] { "8.0.425 [x]", "10.0.100-rc.2.25502.107 [x]" };

        Assert.Null(DotNetSdkSelector.SelectLatestStable(salida, 10));
        Assert.True(DotNetSdkSelector.HasPreview(salida, 10));
    }

    [Fact]
    public void EntradasInvalidas_SeIgnoran()
    {
        var salida = new[] { "", "   ", "texto no válido", "10 [x]", "abc.def.ghi [x]", "10.0.400 [C:\\sdk]" };

        Assert.Equal("10.0.400", DotNetSdkSelector.SelectLatestStable(salida, 10));
    }
}
