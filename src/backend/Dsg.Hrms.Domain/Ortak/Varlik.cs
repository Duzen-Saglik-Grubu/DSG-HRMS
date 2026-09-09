namespace Dsg.Hrms.Domain.Ortak;

/// <summary>
/// Veritabaninda saklanan tum is varliklarinin temel tipi.
/// </summary>
/// <remarks>
/// <para>
/// Kimlik tasarimi ADR-0004 §2'ye dayanir:
/// </para>
/// <list type="bullet">
///   <item><see cref="Id"/> — veritabani ici birincil anahtar (<c>bigint</c>).</item>
///   <item><see cref="GenelId"/> — API'de kullanilan dis kimlik (<c>uuid</c>).</item>
/// </list>
/// <para>
/// Iki ayri kimlik tutulmasinin nedeni: <c>bigint</c> dizin boyutu ve ekleme
/// performansi acisindan iyidir, ancak sirali oldugu icin disariya verildiginde
/// kayit sayisini ve olusturma sirasini sizdirir. API'de <see cref="GenelId"/>
/// kullanilir.
/// </para>
/// </remarks>
public abstract class Varlik
{
    /// <summary>Veritabani ici birincil anahtar. Disariya verilmez.</summary>
    public long Id { get; protected set; }

    /// <summary>API'de kullanilan dis kimlik.</summary>
    public Guid GenelId { get; protected set; } = Guid.CreateVersion7();
}
