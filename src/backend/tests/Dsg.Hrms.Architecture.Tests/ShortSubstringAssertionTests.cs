using System.Text.RegularExpressions;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// Kisa alt dizeyle "icermez" denetimi (#404): testlerde 1–3 karakterlik bir sabitle yapilan
/// <c>ShouldNotContain</c>, gerekce yorumu (<c>// kisa-alt-dize: ...</c>) tasimalidir.
/// </summary>
/// <remarks>
/// <para>
/// Neden: Rastgele bir deger (nonce, GUID, zaman damgasi, base64) kisa bir alt dizeyi
/// tesadufen icerir. Shouldly karsilastirmayi buyuk/kucuk harf duyarsiz yapar; "3:" gibi iki
/// karakterlik bir dize base64 bir degerde her calistirmada yaklasik 1/64 olasilikla olusur.
/// Test boyle durumda sebepsiz duser ve CI'a guveni asindirir.
/// </para>
/// <para>
/// Ayni hata iki kez yasandi: #295 (kayit testi, rastgele kimlikteki "567") ve #404 (sifreleme
/// testi, nonce'taki "3:"). Ikisinde de yalnizca o test duzeltilmisti. Bu test kalibin
/// kendisini yakalar: kisa alt dize ya belirlenimci bir metne karsi denenir ve bu yorumla
/// yazilir ya da deger parcalarina ayrilip esitlikle denetlenir.
/// </para>
/// </remarks>
public sealed partial class ShortSubstringAssertionTests
{
    private const int MaxShortLength = 3;
    private const string Marker = "kisa-alt-dize:";
    private const int MarkerLookBack = 3;

    [Fact]
    public void Detector_finds_short_literal_without_justification()
    {
        // Denetimin kendisi: bozulmus ornegi yakalamali, gerekceli ornegi gecirmeli.
        Violations("ornek.cs", ["value.ShouldNotContain(\"3:\");"]).ShouldHaveSingleItem();
        Violations("ornek.cs", ["text.ShouldNotContain(\"@\");"]).ShouldHaveSingleItem();
        Violations("ornek.cs", ["// kisa-alt-dize: ileti sabit metin", "text.ShouldNotContain(\"@\");"]).ShouldBeEmpty();
        Violations("ornek.cs", ["text.ShouldNotContain(\"ahmet\");"]).ShouldBeEmpty();
        Violations("ornek.cs", ["text.ShouldNotContain(nationalId);"]).ShouldBeEmpty();
    }

    [Fact]
    public void Short_literal_negative_assertions_carry_a_justification()
    {
        var testsRoot = Path.Combine(FindRepositoryRoot(), "src", "backend", "tests");
        var files = Directory.GetFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part is "bin" or "obj"))
            .ToList();
        files.Count.ShouldBeGreaterThan(50, "Test kaynaklari okunamadi; test hicbir seyi denetlemeden gecerdi.");

        var violations = files
            .SelectMany(path => Violations(Path.GetRelativePath(testsRoot, path), File.ReadAllLines(path)))
            .ToList();

        violations.ShouldBeEmpty(
            "Kisa alt dizeyle 'icermez' denetimi rastgele bir degerde tesadufen duser (#295, #404). " +
            "Deger belirlenimciyse ustteki satira '// kisa-alt-dize: <neden belirlenimci>' yazin; " +
            "rastgele parca iceriyorsa degeri parcalarina ayirip esitlikle denetleyin.");
    }

    private static IEnumerable<string> Violations(string file, string[] lines)
    {
        // Bu dosyanin kendi ornekleri dize icindedir; ShouldNotContain( ile baslamaz, eslesmez.
        for (var i = 0; i < lines.Length; i++)
        {
            foreach (Match match in ShortLiteral().Matches(lines[i]))
            {
                if (match.Groups["text"].Value.Length > MaxShortLength)
                {
                    continue;
                }

                var justified = Enumerable.Range(Math.Max(0, i - MarkerLookBack), i - Math.Max(0, i - MarkerLookBack) + 1)
                    .Any(j => lines[j].Contains(Marker, StringComparison.Ordinal));
                if (!justified)
                {
                    yield return $"{file}:{i + 1}: ShouldNotContain(\"{match.Groups["text"].Value}\")";
                }
            }
        }
    }

    [GeneratedRegex("""(?<![\\"])ShouldNotContain\("(?<text>(?:[^"\\]|\\.)*)"[,)]""")]
    private static partial Regex ShortLiteral();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "src", "backend", "Dsg.Hrms.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Depo koku bulunamadi (src/backend/Dsg.Hrms.slnx).");
    }
}
