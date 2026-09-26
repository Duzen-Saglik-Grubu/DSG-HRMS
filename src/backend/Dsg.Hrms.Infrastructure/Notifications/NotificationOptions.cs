using System.ComponentModel.DataAnnotations;

namespace Dsg.Hrms.Infrastructure.Notifications;

/// <summary>
/// Ileti gonderim ayarlari (ADR-0012 §7, #85).
/// </summary>
/// <remarks>
/// <para>
/// Saglayici erisim bilgileri burada DEGIL, parametre deposundadir (<c>PRM-ENT-01…06</c>).
/// Bu sinif yalnizca gonderimin <b>kime</b> yapilabilecegini belirler.
/// </para>
/// <para>
/// Varsayilan <see cref="DeliveryMode.LogOnly"/>: yanlis yapilandirilmis bir ortam
/// gercek personele ileti gondermez. Uretim <c>Send</c> kipini acikca secer.
/// </para>
/// </remarks>
public sealed class NotificationOptions : IValidatableObject
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "Notifications";

    /// <summary>Gonderim kipi.</summary>
    public DeliveryMode DeliveryMode { get; init; } = DeliveryMode.LogOnly;

    /// <summary>
    /// <see cref="DeliveryMode.AllowList"/> kipinde ileti gonderilebilecek e-posta adresleri
    /// ve <c>5XXXXXXXXX</c> bicimli telefonlar; virgulle ayrilir. Buyuk/kucuk harf duyarsiz.
    /// </summary>
    /// <remarks>
    /// Tek bir metin olarak alinir: ortam degiskeninde (<c>Notifications__AllowedRecipients</c>)
    /// liste vermenin en sade yolu budur.
    /// </remarks>
    public string? AllowedRecipients { get; init; }

    /// <summary>Izin listesinin ogeleri.</summary>
    public IReadOnlyList<string> AllowedRecipientList =>
        (AllowedRecipients ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Kuyruk kapasitesi. Dolunca yeni ileti reddedilir.</summary>
    [Range(10, 100_000, ErrorMessage = "Bildirim kuyrugu kapasitesi 10-100000 araliginda olmalidir.")]
    public int QueueCapacity { get; init; } = 1000;

    /// <summary>Alicinin gonderim kipine gore gonderilip gonderilemeyecegi.</summary>
    public bool Permits(string recipient) => DeliveryMode switch
    {
        DeliveryMode.Send => true,
        DeliveryMode.AllowList => AllowedRecipientList.Contains(recipient.Trim(), StringComparer.OrdinalIgnoreCase),
        _ => false,
    };

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DeliveryMode == DeliveryMode.AllowList && AllowedRecipientList.Count == 0)
        {
            yield return new ValidationResult(
                "Notifications:DeliveryMode AllowList iken Notifications:AllowedRecipients bos olamaz.",
                [nameof(AllowedRecipients)]);
        }
    }
}

/// <summary>Gonderim kipi.</summary>
public enum DeliveryMode
{
    /// <summary>Hicbir ileti gonderilmez; gonderim kaydi <c>Suppressed</c> olarak yazilir. Icerik YAZILMAZ.</summary>
    LogOnly = 0,

    /// <summary>Yalnizca <see cref="NotificationOptions.AllowedRecipients"/> listesindekilere gonderilir (UAT, gelistirme).</summary>
    AllowList = 1,

    /// <summary>Tum alicilara gonderilir (uretim).</summary>
    Send = 2,
}
