using Dsg.Hrms.Domain.Settings;

namespace Dsg.Hrms.Domain.Tests.Settings;

/// <summary>Parametre satirinin deger kurallari (SYG-KMLK-075).</summary>
public sealed class SystemParameterTests
{
    [Fact]
    public void Setting_the_same_value_is_not_a_change()
    {
        // Degismeyen deger yazilmaz; aksi halde denetim izine anlamsiz satir duserdi.
        var parameter = SystemParameter.Create("PRM-KML-03");

        parameter.SetValue("5").ShouldBeTrue();
        parameter.SetValue("5").ShouldBeFalse();
        parameter.SetValue("6").ShouldBeTrue();
        parameter.Value.ShouldBe("6");
    }

    [Fact]
    public void Protected_value_and_plain_value_are_mutually_exclusive()
    {
        var parameter = SystemParameter.Create("PRM-ENT-06");
        parameter.SetValue("acik");

        parameter.SetProtectedValue("v1:sifreli");

        parameter.ProtectedValue.ShouldBe("v1:sifreli");
        parameter.Value.ShouldBeNull();

        parameter.SetValue("acik").ShouldBeTrue();
        parameter.ProtectedValue.ShouldBeNull();
    }

    [Fact]
    public void Key_is_required()
    {
        Should.Throw<ArgumentException>(() => SystemParameter.Create(" "));
    }

    [Fact]
    public void Protected_value_is_required()
    {
        Should.Throw<ArgumentException>(() => SystemParameter.Create("PRM-ENT-06").SetProtectedValue(""));
    }
}
