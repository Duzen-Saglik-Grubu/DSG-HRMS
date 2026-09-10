using System.Diagnostics;
using Dsg.Hrms.Api.Observability;

namespace Dsg.Hrms.Api.IntegrationTests.Observability;

/// <summary>
/// Izleme kayitlarinda kisisel veri kalmadigini dogrular (ADR-0009 §4).
/// </summary>
/// <remarks>
/// Izleme verisi cogu kurumda "teknik veri" sayilip gozden kacar; oysa SQL metni ve
/// sorgu dizesi duzenli olarak kisisel veri tasir.
/// </remarks>
public sealed class PersonalDataScrubbingProcessorTests
{
    private const string SourceName = "Dsg.Hrms.Tests";

    private static readonly ActivitySource TestSource = new(SourceName);

    /// <summary>Test suresince Activity uretimini etkinlestirir.</summary>
    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            // DIKKAT: burada TestSource ALANINA erisilmez. Yeni bir ActivitySource
            // olusturuldugunda kayitli dinleyiciler cagrilir; bu, tipin statik
            // kurucusu HENUZ calisirken olur ve alan o anda null olurdu.
            ShouldListenTo = source => source.Name == SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
        };

        ActivitySource.AddActivityListener(listener);

        return listener;
    }

    [Theory]
    [InlineData("db.statement", "SELECT national_id FROM person WHERE national_id = '12345678901'")]
    [InlineData("db.query.text", "SELECT * FROM person")]
    [InlineData("url.query", "?tckn=12345678901")]
    [InlineData("url.full", "http://hrms/api/v1/persons?tckn=12345678901")]
    [InlineData("http.url", "http://hrms/api/v1/persons?tckn=12345678901")]
    public void Tags_that_may_carry_personal_data_are_removed(string tagName, string tagValue)
    {
        using var listener = CreateListener();
        using var processor = new PersonalDataScrubbingProcessor();
        using var activity = TestSource.StartActivity("test");

        activity.ShouldNotBeNull();
        activity.SetTag(tagName, tagValue);

        processor.OnEnd(activity);

        // Etiket MASKELENMEZ, silinir: SQL metnini kismen gostermenin tani degeri yoktur.
        activity.GetTagItem(tagName).ShouldBeNull();
    }

    [Fact]
    public void Diagnostic_tags_are_preserved()
    {
        // Asiri temizlik izi ise yaramaz hâle getirir; sorunun yerini gosteren
        // etiketler kalmalidir.
        using var listener = CreateListener();
        using var processor = new PersonalDataScrubbingProcessor();
        using var activity = TestSource.StartActivity("test");

        activity.ShouldNotBeNull();
        activity.SetTag("db.system", "postgresql");
        activity.SetTag("http.route", "/api/v1/persons/{id}");
        activity.SetTag("http.response.status_code", 200);

        processor.OnEnd(activity);

        activity.GetTagItem("db.system").ShouldBe("postgresql");
        activity.GetTagItem("http.route").ShouldBe("/api/v1/persons/{id}");
        activity.GetTagItem("http.response.status_code").ShouldBe(200);
    }
}
