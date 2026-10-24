using Dsg.Hrms.Domain.Reference;

namespace Dsg.Hrms.Domain.Tests.Reference;

/// <summary>Referans tanimi kurallari (SYG-PERS-072; ADR-0022 §1, KR-129).</summary>
public sealed class ReferenceDefinitionTests
{
    [Fact]
    public void Duty_is_created_active_and_can_be_renamed_and_deactivated()
    {
        var duty = ReferenceDefinition.Create(ReferenceType.Duty, " G001 ", " Biyolog ", 1);

        duty.Code.ShouldBe("G001");
        duty.Name.ShouldBe("Biyolog");
        duty.IsActive.ShouldBeTrue();
        duty.Rename("Biyolog").ShouldBeFalse();
        duty.Rename("Kıdemli Biyolog").ShouldBeTrue();
        duty.Deactivate().ShouldBeTrue();
        duty.Deactivate().ShouldBeFalse();
    }

    [Fact]
    public void Blood_group_cannot_be_created_by_a_user()
    {
        // KR-129: kan grubu tanimlari yalnizca migration ile gelir.
        Should.Throw<InvalidOperationException>(() => ReferenceDefinition.Create(ReferenceType.BloodGroup, "9", "Yeni", 9));
    }

    [Fact]
    public void Name_is_required()
    {
        Should.Throw<ArgumentException>(() => ReferenceDefinition.Create(ReferenceType.Title, "5", "  ", 5));
    }
}
