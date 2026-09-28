using System.Net;
using Dsg.Hrms.Api.IntegrationTests.Identity;
using Dsg.Hrms.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Audit;

/// <summary>
/// Denetim izinin UYGULAMANIN KENDI bagimlilik kaydiyla calismasi (#93).
/// </summary>
/// <remarks>
/// Diger denetim izi testleri baglami kendi kurduklari seceneklerle olusturur; bu yuzden
/// uygulamada ara katmanlarin iki kez eklenmesini goremezlerdi. Bu test uygulamanin tamamini
/// kurar ve TEK degisikligin TEK satir urettigini denetler.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class AuditRegistrationTests : IClassFixture<RegistrationApiFixture>
{
    private readonly RegistrationApiFixture _fixture;

    public AuditRegistrationTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task One_change_produces_exactly_one_audit_row()
    {
        await _fixture.ResetAsync();
        using var client = _fixture.CreateClient();

        var (status, body) = await client.PostJsonAsync("/api/v1/identity/registrations", new
        {
            nationalId = NationalId(1),
            birthDate = "1985-04-12",
            email = "ahmet.yilmaz@duzen.com.tr",
        });
        status.ShouldBe(HttpStatusCode.OK);

        var id = Guid.Parse(body.GetProperty("registrationId").GetString()!);
        var rows = await _fixture.WithDbAsync(c => c.ChangeLog.CountAsync(e => e.EntityId == id && e.EntityName == nameof(RegistrationAttempt)));

        rows.ShouldBe(1);
    }
}
