using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Dsg.Hrms.Application.Tests.Settings;

/// <summary>Kurumsal logo (PRM-GRN-01, SYG-KMLK-069).</summary>
public sealed class BrandingServiceTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 7];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 7];

    private readonly IBrandLogoStore _store = Substitute.For<IBrandLogoStore>();

    private BrandingService Service() => new(_store, NullLogger<BrandingService>.Instance);

    [Fact]
    public async Task Type_is_taken_from_the_file_signature()
    {
        await Service().SetLogoAsync(Png, CancellationToken.None);
        _store.Received(1).Add(Arg.Is<BrandLogo>(l => l.ContentType == "image/png" && l.SizeBytes == Png.Length));

        var existing = BrandLogo.Create(Png, "image/png", "eski");
        _store.FindAsync(Arg.Any<CancellationToken>()).Returns(existing);
        await Service().SetLogoAsync(Jpeg, CancellationToken.None);
        existing.ContentType.ShouldBe("image/jpeg");
        existing.Sha256.Length.ShouldBe(64);
    }

    [Fact]
    public async Task Svg_html_empty_and_oversized_files_are_refused()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray();

        (await Should.ThrowAsync<BusinessRuleException>(() => Service().SetLogoAsync(svg, CancellationToken.None)))
            .Message.ShouldBe("Logo PNG veya JPEG biçiminde olmalıdır.");
        await Should.ThrowAsync<BusinessRuleException>(() => Service().SetLogoAsync([], CancellationToken.None));
        await Should.ThrowAsync<BusinessRuleException>(() => Service().SetLogoAsync([.. Png, .. new byte[BrandLogo.MaxSizeBytes]], CancellationToken.None));
        _store.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Removing_a_missing_logo_does_nothing()
    {
        await Service().RemoveLogoAsync(CancellationToken.None);

        _store.DidNotReceiveWithAnyArgs().Remove(default!);
        await _store.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
