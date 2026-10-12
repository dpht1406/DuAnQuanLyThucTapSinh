using FluentAssertions;
using StudentInternshipMgmt.Infrastructure.Persistence.Seed;

namespace StudentInternshipMgmt.Infrastructure.Tests.Regression;

public class SeedDataRegressionTests
{
    [Fact]
    public void Admin_seed_requires_password_change()
    {
        SeedData.Users.Should()
            .ContainSingle()
            .Which.MustChangePassword.Should()
            .BeTrue();
    }
}
