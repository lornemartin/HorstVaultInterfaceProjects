using HorstMFG.Web.Services;

namespace HorstMFG.Web.Tests;

public class BomFieldMappersTests
{
    /*
     - [Theory] tells xUnit that this test takes parameters and should run once per data row. A plain [Fact] runs once with
    no parameters, like the tests in VaultBomIngestServiceTests.cs.
    - Each [InlineData(...)] is one test case. Its four values go into the method's four parameters, in order:

    | Position | Parameter   | Meaning                                                                        |
    |----------|-------------|--------------------------------------------------------------------------------|
    | 1        | baseIsStock | The part's Is Stock checkbox in Vault (P1 or P2, whichever plant is importing) |
    | 2        | exceptions  | That plant's Stock Exception text, e.g. "X|LA-SB1000*"                         |
    | 3        | product     | The top-level product being imported                                           |
    | 4        | expected    | What ResolveIsStock should return                                              |

    - The body calls the real BomFieldMappers.ResolveIsStock with the first three values. Assert.Equal checks the result
     against the fourth. If they differ, that case fails and xUnit reports which row it was.

    For example, this row:
    [InlineData(true, "LA-SB1000*", "LA-SB100054", false)]
    reads as: "The part is marked stock. The exception list contains LA-SB1000*. We're importing LA-SB100054, which
    matches the wildcard, so the flag flips and the part should come out as not stock." 
    */
    [Theory]
    // No exception list or no product: base flag passes through unchanged.
    [InlineData(true,  null,          "PROD-A", true)]
    [InlineData(false, "",            "PROD-A", false)]
    [InlineData(true,  "   ",         "PROD-A", true)]
    [InlineData(true,  "PROD-A",      null,     true)]
    // Product listed: flag flips in both directions.
    [InlineData(true,  "PROD-A",      "PROD-A", false)]
    [InlineData(false, "PROD-A",      "PROD-A", true)]
    [InlineData(true,  "X|PROD-A|Y",  "PROD-A", false)]
    // Product not listed.
    [InlineData(true,  "X|Y",         "PROD-A", true)]
    [InlineData(false, "X|Y",         "PROD-A", false)]
    // Matching is trimmed, case-insensitive, and ignores .iam/.ipt extensions.
    [InlineData(true,  " x | prod-a ", "PROD-A", false)]
    [InlineData(true,  "PROD-A.iam",  "prod-a", false)]
    [InlineData(true,  "PROD-A",      "PROD-A.iam", false)]
    [InlineData(true,  "||PROD-A||",  "PROD-A", false)]
    // Whole-name matches only, and only '|' delimits.
    [InlineData(true,  "PROD-AB",     "PROD-A", true)]
    [InlineData(true,  "PROD",        "PROD-A", true)]
    [InlineData(true,  "PROD-A,X",    "PROD-A", true)]
    // '*' wildcard: any run of characters (including none), still whole-name and case-insensitive.
    [InlineData(true,  "LA-SB1000*",       "LA-SB100054",     false)]
    [InlineData(true,  "LA-SB1000*",       "LA-SB100072",     false)]
    [InlineData(false, "LA-SB1000*",       "LA-SB100054",     true)]
    [InlineData(true,  "LA-SB1000*",       "LA-SB1000",       false)]
    [InlineData(true,  "la-sb1000*",       "LA-SB100054",     false)]
    [InlineData(true,  "X|LA-SB1000*|Y",   "LA-SB100054.iam", false)]
    [InlineData(true,  "*-SB1000*",        "LA-SB100054",     false)]
    [InlineData(true,  "LA-*-54",          "LA-SB1000-54",    false)]
    [InlineData(true,  "LA-SB1000*",       "LA-SB2000",       true)]
    [InlineData(true,  "LA-SB1000*",       "XLA-SB100054",    true)]
    [InlineData(true,  "LA-SB1000*4",      "LA-SB100045",     true)]
    [InlineData(true,  "LA-SB1000*|LA-SB2000*", "LA-SB100078", false)]
    [InlineData(false, "LA-SB1000*|LA-SB2000*", "LA-SB2000-54", true)]
    // Regex metacharacters other than '*' are literal.
    [InlineData(true,  "LA.SB*",           "LAXSB1",          true)]
    [InlineData(true,  "LA.SB*",           "LA.SB1",          false)]
    public void ResolveIsStock_AppliesExceptionList(bool baseIsStock, string? exceptions, string? product, bool expected)
    {
        Assert.Equal(expected, BomFieldMappers.ResolveIsStock(baseIsStock, exceptions, product));
    }

    [Theory]
    [InlineData("0.375 in",   "0.375")]
    [InlineData("0.375",      "0.375")]
    [InlineData(" 1.000 IN ", "1.000")]
    [InlineData("10 GA",      "10 GA")]
    [InlineData("",           "")]
    public void NormalizeThickness_StripsInchUnit(string input, string expected)
    {
        Assert.Equal(expected, BomFieldMappers.NormalizeThickness(input));
    }
}
