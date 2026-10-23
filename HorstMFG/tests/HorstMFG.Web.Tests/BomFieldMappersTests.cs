using HorstMFG.Web.Services;

namespace HorstMFG.Web.Tests;

public class BomFieldMappersTests
{
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
    public void ResolveIsStock_AppliesExceptionList(bool baseIsStock, string? exceptions, string? product, bool expected)
    {
        Assert.Equal(expected, BomFieldMappers.ResolveIsStock(baseIsStock, exceptions, product));
    }
}
