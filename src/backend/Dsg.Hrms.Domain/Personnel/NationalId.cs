namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// T.C. Kimlik Numarasi dogrulamasi (SYG-KMLK-007, SYG-KMLK-014).
/// </summary>
/// <remarks>
/// <para>
/// Resmi sagla algoritmasi: 11 hane, ilk hane 0 olamaz;
/// 10. hane = ((1+3+5+7+9. hanelerin toplami) * 7 - (2+4+6+8. hanelerin toplami)) mod 10;
/// 11. hane = ilk 10 hanenin toplami mod 10.
/// </para>
/// <para>
/// Gecersiz TCKN tasiyan bir LOGO karti kisi olarak OLUSTURULMAZ. Aksi halde
/// uyelik eslestirmesi, hic var olmayan bir kimlige karsi yapilabilirdi.
/// </para>
/// </remarks>
public static class NationalId
{
    /// <summary>TCKN uzunlugu.</summary>
    public const int Length = 11;

    /// <summary>Degerin gecerli bir TCKN olup olmadigini dondurur.</summary>
    public static bool IsValid(string? value)
    {
        if (value is null || value.Length != Length || value[0] == '0')
        {
            return false;
        }

        Span<int> digits = stackalloc int[Length];
        for (var i = 0; i < Length; i++)
        {
            if (!char.IsAsciiDigit(value[i]))
            {
                return false;
            }

            digits[i] = value[i] - '0';
        }

        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];

        // C#'ta negatif sayinin mod'u negatif olabilir; +10 ile pozitife tasinir.
        var tenth = (((oddSum * 7) - evenSum) % 10 + 10) % 10;
        if (digits[9] != tenth)
        {
            return false;
        }

        var firstTenSum = 0;
        for (var i = 0; i < 10; i++)
        {
            firstTenSum += digits[i];
        }

        return digits[10] == firstTenSum % 10;
    }
}
