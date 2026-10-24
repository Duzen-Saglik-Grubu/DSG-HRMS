using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Dsg.Hrms.Infrastructure.Data;

/// <summary>
/// Sorgularda kullanilan veritabani fonksiyonlari.
/// </summary>
public static class HrmsDbFunctions
{
    /// <summary>
    /// Metni Turkce karaktersiz arama bicimine indirir: <c>lower(translate(x, 'ÇĞIİÖŞÜçğıöşü',
    /// 'CGIIOSUcgiosu'))</c>. <see cref="SearchText.Fold"/> ile ayni kuraldir; suzme veritabaninda
    /// yapilir (ADR-0021 §7, SYG-PERS-024).
    /// </summary>
    /// <remarks>Yalnizca LINQ sorgusunda cevrilir; dogrudan cagrilamaz.</remarks>
    public static string Fold(string? value) =>
        throw new InvalidOperationException($"{nameof(Fold)} yalnizca veritabani sorgusunda kullanilir.");

    /// <summary>Fonksiyonlari modele tanitir.</summary>
    public static void Register(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDbFunction(typeof(HrmsDbFunctions).GetMethod(nameof(Fold))!)
            .HasTranslation(args =>
            {
                var text = new StringTypeMapping("text", System.Data.DbType.String);
                var translated = new SqlFunctionExpression(
                    "translate",
                    [args[0], new SqlConstantExpression(SearchText.TurkishLetters, text), new SqlConstantExpression(SearchText.AsciiLetters, text)],
                    nullable: true,
                    argumentsPropagateNullability: [true, false, false],
                    typeof(string),
                    text);
                return new SqlFunctionExpression("lower", [translated], nullable: true, argumentsPropagateNullability: [true], typeof(string), text);
            });
    }
}
