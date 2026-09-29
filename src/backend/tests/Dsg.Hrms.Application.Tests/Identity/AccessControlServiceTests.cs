using System.ComponentModel.DataAnnotations;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Dsg.Hrms.Application.Tests.Identity;

/// <summary>Eylem yetkisi: ilk sistem yoneticisi ve izin listesi (SYG-KMLK-074).</summary>
public sealed class AccessControlServiceTests
{
    private readonly IAccessControlStore _store = Substitute.For<IAccessControlStore>();

    private AccessControlService Service(string? administrators) =>
        new(_store, new AccessControlOptions { BootstrapAdministrators = administrators }, NullLogger<AccessControlService>.Instance);

    [Fact]
    public async Task Listed_email_gets_the_system_administrator_role_once_case_insensitively()
    {
        var role = (Role)Activator.CreateInstance(typeof(Role), nonPublic: true)!;
        typeof(Role).GetProperty(nameof(Role.Id))!.SetValue(role, 1L);
        _store.FindRoleAsync(Role.SystemAdministratorCode, Arg.Any<CancellationToken>()).Returns(role);
        _store.HasRoleAsync(7, role.Id, Arg.Any<CancellationToken>()).Returns(false, true);

        var service = Service(" Mehmet.Kaya@duzen.com.tr , ayse@duzen.com.tr ");

        (await service.EnsureBootstrapAdministratorAsync(7, "mehmet.kaya@DUZEN.com.tr", CancellationToken.None)).ShouldBeTrue();
        (await service.EnsureBootstrapAdministratorAsync(7, "mehmet.kaya@duzen.com.tr", CancellationToken.None)).ShouldBeFalse();
        _store.Received(1).Add(Arg.Is<object>(o => o is UserRole));
    }

    [Fact]
    public async Task Unlisted_or_missing_email_changes_nothing()
    {
        var service = Service("yonetici@duzen.com.tr");

        (await service.EnsureBootstrapAdministratorAsync(7, "baskasi@duzen.com.tr", CancellationToken.None)).ShouldBeFalse();
        (await service.EnsureBootstrapAdministratorAsync(7, null, CancellationToken.None)).ShouldBeFalse();
        (await Service(null).EnsureBootstrapAdministratorAsync(7, "yonetici@duzen.com.tr", CancellationToken.None)).ShouldBeFalse();

        await _store.DidNotReceiveWithAnyArgs().FindRoleAsync(default!, default);
    }

    [Fact]
    public void Options_parse_the_comma_separated_list_and_reject_invalid_addresses()
    {
        new AccessControlOptions { BootstrapAdministrators = " A@duzen.com.tr,, b@duzen.com.tr " }
            .BootstrapAdministratorEmails.ShouldBe(["a@duzen.com.tr", "b@duzen.com.tr"]);

        var invalid = new AccessControlOptions { BootstrapAdministrators = "a@duzen.com.tr, gecersiz" };
        invalid.Validate(new ValidationContext(invalid)).ShouldNotBeEmpty();
        new AccessControlOptions().Validate(new ValidationContext(new AccessControlOptions())).ShouldBeEmpty();
    }

    [Fact]
    public void Every_role_permission_is_in_the_fixed_list()
    {
        foreach (var (role, permissions) in Permissions.ByRole)
        {
            permissions.ShouldAllBe(p => Permissions.All.ContainsKey(p), $"{role} rolunde listede olmayan izin var.");
        }

        Permissions.All.Keys.ShouldAllBe(p => System.Text.RegularExpressions.Regex.IsMatch(p, "^[a-z]+\\.[a-z]+\\.[a-z]+$"));
    }
}
