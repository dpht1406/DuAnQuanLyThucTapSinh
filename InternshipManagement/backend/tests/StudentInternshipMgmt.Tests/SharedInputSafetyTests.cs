using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Tests;

public class SharedInputSafetyTests
{
    [Theory]
    [InlineData("phat@gmail.com")]
    [InlineData("a.b+tag@outlook.com")]
    [InlineData("sv001@student.edu.vn")]
    [InlineData("  phat@GMAIL.COM  ")]
    public void Email_validator_accepts_valid_addresses(string email)
    {
        Assert.True(EmailAddressValidator.IsValid(email));
    }

    [Theory]
    [InlineData("phat@gmail.co")]
    [InlineData("phat@gmail.con")]
    [InlineData("phat@gmial.com")]
    [InlineData("phat@@gmail.com")]
    [InlineData("phat@gmail")]
    [InlineData("phat@.com")]
    [InlineData(".phat@gmail.com")]
    [InlineData("phat..a@gmail.com")]
    [InlineData("phat @gmail.com")]
    [InlineData("")]
    [InlineData("phat@gmail.com\n")]
    public void Email_validator_rejects_invalid_addresses(string email)
    {
        Assert.False(EmailAddressValidator.IsValid(email));
    }

    [Fact]
    public void Email_validator_rejects_addresses_longer_than_254_characters()
    {
        var email = $"{new string('a', 64)}@{new string('b', 63)}.{new string('c', 63)}.{new string('d', 62)}";

        Assert.True(email.Length > 254);
        Assert.False(EmailAddressValidator.IsValid(email));
    }

    [Theory]
    [InlineData("phat@gmail.co", "phat@gmail.com")]
    [InlineData("phat@gmial.com", "phat@gmail.com")]
    public void Email_validator_suggests_the_corrected_common_domain(string email, string suggestion)
    {
        Assert.False(EmailAddressValidator.TryValidate(email, out var errorMessage));
        Assert.Contains(suggestion, errorMessage);
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+cmd", "'+cmd")]
    [InlineData("-2", "'-2")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("\tformula", "'\tformula")]
    [InlineData("\rformula", "'\rformula")]
    [InlineData("Nguyen Van A", "Nguyen Van A")]
    public void Spreadsheet_helper_escapes_formula_prefixes(string value, string expected)
    {
        Assert.Equal(expected, SpreadsheetSafety.EscapeFormula(value));
    }

    [Fact]
    public void Safe_string_attribute_enforces_length_and_control_character_rules()
    {
        Assert.True(new SafeStringAttribute(20, allowLineBreaks: true).IsValid("line 1\nline 2"));
        Assert.False(new SafeStringAttribute(10, allowLineBreaks: true).IsValid("line\tbreak"));
        Assert.False(new SafeStringAttribute(10).IsValid("line\nbreak"));
        Assert.False(new SafeStringAttribute(3).IsValid("four"));
        Assert.True(new SafeStringAttribute(4, trimBeforeLength: true).IsValid(" abcd "));
    }
}