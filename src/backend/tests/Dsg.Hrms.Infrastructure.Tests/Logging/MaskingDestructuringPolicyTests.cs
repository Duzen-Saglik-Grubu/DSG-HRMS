using System.Globalization;
using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Infrastructure.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Dsg.Hrms.Infrastructure.Tests.Logging;

/// <summary>
/// Gunluk ciktisinda kisisel verinin duz metin olarak yer almadigini dogrular
/// (ADR-0009 §4).
/// </summary>
/// <remarks>
/// Testler maskeleyiciyi degil, <b>gercek Serilog hattini</b> calistirir ve uretilen
/// JSON metnini denetler. Amaç bilincli: birim testi gecen bir maskeleyici, hatta
/// yanlis baglandigi icin isini yapmiyor olabilirdi.
/// </remarks>
public sealed class MaskingDestructuringPolicyTests
{
    private const string NationalId = "12345678901";
    private const string Phone = "5321234567";
    private const string Email = "ahmet.yilmaz@duzen.com.tr";
    private const string VerificationCode = "483920";

    private sealed class PersonSample
    {
        public string FullName { get; init; } = "Ahmet Yilmaz";

        [PersonalData(PersonalDataKind.NationalId)]
        public string NationalId { get; init; } = MaskingDestructuringPolicyTests.NationalId;

        [PersonalData(PersonalDataKind.Phone)]
        public string MobilePhone { get; init; } = Phone;

        [PersonalData(PersonalDataKind.Email)]
        public string Email { get; init; } = MaskingDestructuringPolicyTests.Email;

        [Secret]
        public string VerificationCode { get; init; } = MaskingDestructuringPolicyTests.VerificationCode;

        public ContactSample Contact { get; init; } = new();
    }

    private sealed class ContactSample
    {
        [PersonalData(PersonalDataKind.Phone)]
        public string EmergencyPhone { get; init; } = "5439876543";

        public string City { get; init; } = "Ankara";
    }

    [Fact]
    public void Personal_data_never_appears_in_plain_text_in_the_log_output()
    {
        var output = Render(logger => logger.Information("Personel kaydi {@Person}", new PersonSample()));

        output.ShouldNotContain(NationalId);
        output.ShouldNotContain(Phone);
        output.ShouldNotContain(Email);
        output.ShouldNotContain(VerificationCode);
        output.ShouldNotContain("5439876543");
    }

    [Fact]
    public void Masked_values_stay_useful_for_diagnosis()
    {
        var output = Render(logger => logger.Information("Personel kaydi {@Person}", new PersonSample()));

        // Kaydin dogru kisiye ait oldugunu teyit etmeye yeter, kisiyi tanimlamaya yetmez.
        output.ShouldContain("123*****901");
        output.ShouldContain("532*****67");
        output.ShouldContain("ah***@duzen.com.tr");
    }

    [Fact]
    public void Non_sensitive_fields_are_written_as_they_are()
    {
        // Asiri maskeleme gunlugu ise yaramaz hâle getirir; denge onemlidir.
        var output = Render(logger => logger.Information("Personel kaydi {@Person}", new PersonSample()));

        output.ShouldContain("Ahmet Yilmaz");
        output.ShouldContain("Ankara");
    }

    [Fact]
    public void Nested_objects_are_masked_as_well()
    {
        var output = Render(logger => logger.Information("Personel kaydi {@Person}", new PersonSample()));

        output.ShouldContain("543*****43");
    }

    private sealed class TouchTrackingSample
    {
        public bool SecretWasRead { get; private set; }

        [Secret]
        public string Password
        {
            get
            {
                SecretWasRead = true;
                return "cok-gizli-parola";
            }
        }
    }

    [Fact]
    public void Secret_values_are_not_even_read()
    {
        // Okunmayan deger sizamaz: sir alanlarda deger hic elde edilmez.
        var sample = new TouchTrackingSample();

        var output = Render(logger => logger.Information("Giris denemesi {@Request}", sample));

        sample.SecretWasRead.ShouldBeFalse();
        output.ShouldNotContain("cok-gizli-parola");
        output.ShouldContain(Mask.SecretPlaceholder);
    }

    private sealed class ThrowingSample
    {
        public string Safe { get; init; } = "okunabilir";

        public string Broken => Safe.Length > 0
            ? throw new InvalidOperationException("Hesaplanamadi.")
            : string.Empty;
    }

    [Fact]
    public void An_unreadable_property_does_not_break_logging()
    {
        // Gunluk yazmak, uygulamayi durdurmak icin gecerli bir sebep degildir.
        var output = Render(logger => logger.Information("Kayit {@Value}", new ThrowingSample()));

        output.ShouldContain("okunabilir");
    }

    /// <summary>
    /// Gercek Serilog hattini calistirir ve uretilen JSON metnini dondurur.
    /// </summary>
    private static string Render(Action<ILogger> write)
    {
        var sink = new CapturingSink();

        using (var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Destructure.With(new MaskingDestructuringPolicy())
            .WriteTo.Sink(sink)
            .CreateLogger())
        {
            write(logger);
        }

        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var formatter = new JsonFormatter(renderMessage: true);

        foreach (var logEvent in sink.Events)
        {
            formatter.Format(logEvent, writer);
        }

        return writer.ToString();
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
