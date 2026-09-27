using System.IO.Compression;
using Dsg.Hrms.Application.Identity.Passwords;

namespace Dsg.Hrms.Infrastructure.Identity.Passwords;

/// <summary>
/// Uygulamaya gomulu yaygin parola listesi (SYG-KMLK-045). Kaynak ve lisans: README.md.
/// </summary>
/// <remarks>
/// Liste ilk kullanimda bellege bir kez yuklenir (yaklasik 144 bin kayit). Kaynak
/// bulunamazsa uygulama parola kabul ETMEZ: denetim devre disi birakilamaz.
/// </remarks>
public sealed class EmbeddedCommonPasswordList : ICommonPasswordList
{
    /// <summary>Gomulu kaynagin adi (csproj'daki <c>LogicalName</c>).</summary>
    public const string ResourceName = "Dsg.Hrms.Infrastructure.CommonPasswords";

    private readonly Lazy<HashSet<string>> _passwords = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <inheritdoc />
    public int Count => _passwords.Value.Count;

    /// <inheritdoc />
    public bool Contains(string loweredPassword) => _passwords.Value.Contains(loweredPassword);

    private static HashSet<string> Load()
    {
        using var resource = typeof(EmbeddedCommonPasswordList).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Yaygin parola listesi bulunamadi ({ResourceName}).");
        using var gzip = new GZipStream(resource, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);

        var set = new HashSet<string>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 0)
            {
                set.Add(line);
            }
        }

        return set;
    }
}
