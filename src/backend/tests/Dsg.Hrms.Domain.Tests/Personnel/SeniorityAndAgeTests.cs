using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Domain.Tests.Personnel;

/// <summary>
/// Kidem (SYG-PERS-039) ve yas (SYG-PERS-035) hesabi.
/// </summary>
public sealed class SeniorityAndAgeTests
{
    private static readonly DateOnly Today = new(2026, 10, 22);

    [Fact]
    public void Terminated_employment_counts_the_termination_day()
    {
        // AN-04 (KR-127): cikis gunu sayilir; giris 01.01.2020, cikis 31.12.2020 tam bir yildir.
        var seniority = Seniority.Calculate(new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31), Today);

        seniority.ShouldBe(new Seniority(1, 0, 0));
        seniority.Text.ShouldBe("1 Yıl 0 Ay 0 Gün");
    }

    [Fact]
    public void Active_employment_counts_up_to_but_not_including_today()
    {
        Seniority.Calculate(new DateOnly(2025, 10, 22), null, Today).ShouldBe(new Seniority(1, 0, 0));
        Seniority.Calculate(new DateOnly(2025, 10, 23), null, Today).ShouldBe(new Seniority(0, 11, 29));
    }

    [Fact]
    public void Hired_today_has_zero_days()
    {
        var seniority = Seniority.Calculate(Today, null, Today);

        seniority.ShouldBe(new Seniority(0, 0, 0));
        seniority.Text.ShouldBe("0 Gün");
    }

    [Theory]
    [InlineData(2024, 3, 15, 2026, 6, 20, "2 Yıl 3 Ay 5 Gün")]
    [InlineData(2026, 7, 17, 2026, 10, 22, "3 Ay 5 Gün")]
    [InlineData(2026, 10, 10, 2026, 10, 22, "12 Gün")]
    [InlineData(2024, 1, 31, 2024, 2, 29, "1 Ay 0 Gün")]
    public void Text_omits_leading_zero_units(int y1, int m1, int d1, int y2, int m2, int d2, string expected)
    {
        Seniority.Calculate(new DateOnly(y1, m1, d1), null, new DateOnly(y2, m2, d2)).Text.ShouldBe(expected);
    }

    [Theory]
    [InlineData(2026, 1, 31, 2026, 2, 28, 0, 1, 0)]
    [InlineData(2026, 1, 31, 2026, 3, 1, 0, 1, 1)]
    [InlineData(2024, 2, 29, 2025, 2, 28, 1, 0, 0)]
    [InlineData(2025, 8, 31, 2025, 9, 30, 0, 1, 0)]
    public void Month_end_takes_the_last_day_of_the_shorter_month(int y1, int m1, int d1, int y2, int m2, int d2, int years, int months, int days)
    {
        // Eklenen ayda o gun yoksa ayin son gunu alinir: 31.01'e bir ay 28.02'dir.
        Seniority.Calculate(new DateOnly(y1, m1, d1), null, new DateOnly(y2, m2, d2)).ShouldBe(new Seniority(years, months, days));
    }

    [Fact]
    public void Termination_before_hire_yields_zero()
    {
        // LOGO'da hatali girilmis bir kart ekrani dusurmemeli.
        Seniority.Calculate(new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1), Today).ShouldBe(new Seniority(0, 0, 0));
    }

    [Theory]
    [InlineData(1985, 4, 12, 2026, 4, 11, 40)]
    [InlineData(1985, 4, 12, 2026, 4, 12, 41)]
    [InlineData(1985, 4, 12, 2026, 10, 22, 41)]
    public void Age_increases_on_the_birthday(int by, int bm, int bd, int ty, int tm, int td, int expected)
    {
        Age.On(new DateOnly(by, bm, bd), new DateOnly(ty, tm, td)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(2027, 2, 28, 26)]
    [InlineData(2027, 3, 1, 27)]
    [InlineData(2028, 2, 29, 28)]
    public void Leap_day_birthday_ages_on_the_first_of_march_in_common_years(int ty, int tm, int td, int expected)
    {
        // AN-05: 29 Subat dogumlu kisi artik yil olmayan yillarda 1 Mart'ta buyur.
        Age.On(new DateOnly(2000, 2, 29), new DateOnly(ty, tm, td)).ShouldBe(expected);
    }
}
