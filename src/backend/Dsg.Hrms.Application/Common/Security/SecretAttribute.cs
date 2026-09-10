namespace Dsg.Hrms.Application.Common.Security;

/// <summary>
/// Isaretlendigi ozelligin gunluge <b>hicbir bicimde</b> yazilmamasini saglar.
/// </summary>
/// <remarks>
/// <para>
/// Parola, parola ozeti, dogrulama kodu, jeton, API anahtari, baglanti dizesi ve
/// SMS/e-posta govdesi icin kullanilir.
/// </para>
/// <para>
/// <see cref="PersonalDataAttribute"/> ile arasindaki fark onemlidir: kisisel veri
/// <i>kismen</i> gorunur birakilir cunku sorun tanilamak icin gerekir. Sir ise
/// kismen bile gorunmez; ucu gorunen bir dogrulama kodu kaba kuvvet denemesini
/// kolaylastirir.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SecretAttribute : Attribute;
