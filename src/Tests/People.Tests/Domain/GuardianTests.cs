using FSH.Modules.People.Domain;

namespace People.Tests.Domain;

public sealed class GuardianTests
{
    private static Guardian CreateValidGuardian()
        => Guardian.Create(" Sidorova ", " Olga ", "+15550002", "olga@example.com");

    [Fact]
    public void Create_Should_TrimFields_When_Created()
    {
        Guardian guardian = CreateValidGuardian();

        guardian.LastName.ShouldBe("Sidorova");
        guardian.FirstName.ShouldBe("Olga");
    }

    [Fact]
    public void DisplayName_Should_CombineLastAndFirstName()
    {
        Guardian guardian = CreateValidGuardian();

        guardian.DisplayName.ShouldBe("Sidorova Olga");
    }

    [Fact]
    public void LinkUser_UnlinkUser_Should_SetAndClearUserId()
    {
        Guardian guardian = CreateValidGuardian();

        guardian.LinkUser("user-7");
        guardian.UserId.ShouldBe("user-7");

        guardian.UnlinkUser();
        guardian.UserId.ShouldBeNull();
    }
}
