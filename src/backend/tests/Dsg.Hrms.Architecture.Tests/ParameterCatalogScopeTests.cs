using System.Reflection;
using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// Parametre katalogunun geri yonde kapsam denetimi (#211): katalogdaki her parametre onayli
/// bir sistem gereksinimine dayanir ve kodda okunur (SYG-KMLK-075, 076).
/// </summary>
/// <remarks>
/// <para>
/// Ileri yondeki denetim (her SYG'nin bir testi var) CI'daki izlenebilirlik betigindedir.
/// Bu test tersini sinar: kodda olup onayli gereksinimde olmayan bir parametre.
/// </para>
/// <para>
/// Neden: T5'e ait PRM-HSP-03 (IK'nin kilitli hesabi acabilmesi) #84'te T3 kataloguna
/// eklendi, hicbir isleve baglanmadi ve 27.09–12.10.2026 arasinda G2'den ve bes kabul
/// adayindan fark edilmeden gecti (#198). Bu test o gun yazilmis olsaydi #84'te yakalardi.
/// </para>
/// </remarks>
public sealed class ParameterCatalogScopeTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    public static TheoryData<string, string> CatalogEntries()
    {
        var data = new TheoryData<string, string>();
        foreach (var (name, definition) in CatalogFields())
        {
            data.Add(definition.Key, name);
        }

        return data;
    }

    [Fact]
    public void Every_catalog_field_is_in_the_catalog_list()
    {
        // Alan tanimlanip All listesine eklenmezse parametre ekraninda gorunmez, test de
        // onu sinamaz.
        CatalogFields().Select(f => f.Definition.Key).ShouldBe(ParameterCatalog.All.Select(p => p.Key), ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void Every_parameter_is_listed_by_an_approved_system_requirement(string key, string name)
    {
        var requirements = SystemRequirementParameterCells();

        requirements.ShouldContain(
            cell => cell.Contains(key, StringComparison.Ordinal),
            $"{key} ({name}) hicbir SYG maddesinin Parametre sutununda yok. Parametre ya onayli bir gereksinime baglanir ya da katalogdan cikarilir (#198).");
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void Every_parameter_is_read_by_the_code(string key, string name)
    {
        var usage = $"ParameterCatalog.{name}";

        ProductionSources().ShouldContain(
            source => source.Contains(usage, StringComparison.Ordinal),
            $"{key} ({name}) katalog disinda hicbir yerde okunmuyor; ekranda gorunur ama hicbir davranisi degistirmez (#198).");
    }

    private static IEnumerable<(string Name, ParameterDefinition Definition)> CatalogFields() =>
        typeof(ParameterCatalog)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(ParameterDefinition))
            .Select(f => (f.Name, (ParameterDefinition)f.GetValue(null)!));

    // SYG belgelerindeki gereksinim satirlarinin son hucresi (Parametre sutunu).
    private static List<string> SystemRequirementParameterCells()
    {
        var folder = Path.Combine(RepositoryRoot, "docs", "33061", "TEC.3-sistem-yazilim-gereksinimleri", "gereksinimler");
        var cells = Directory.GetFiles(folder, "SYG-*.md")
            .SelectMany(File.ReadAllLines)
            .Where(line => line.StartsWith("| **SYG-", StringComparison.Ordinal))
            .Select(line => line.TrimEnd().TrimEnd('|').Split('|').Last().Trim())
            .ToList();
        cells.ShouldNotBeEmpty("SYG belgeleri okunamadi; test hicbir seyi denetlemeden gecerdi.");
        return cells;
    }

    // Uretim kodu: testler, migration'lar ve katalogun kendisi haric.
    private static List<string> ProductionSources()
    {
        var backend = Path.Combine(RepositoryRoot, "src", "backend");
        var sources = Directory.GetFiles(backend, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsExcluded(Path.GetRelativePath(backend, path)))
            .Select(File.ReadAllText)
            .ToList();
        sources.ShouldNotBeEmpty("Kaynak dosyalar okunamadi; test hicbir seyi denetlemeden gecerdi.");
        return sources;
    }

    private static bool IsExcluded(string relativePath)
    {
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts[0] == "tests"
            || parts.Any(p => p is "bin" or "obj" or "Migrations")
            || parts[^1] == "ParameterCatalog.cs";
    }

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
