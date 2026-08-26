using System.Linq.Expressions;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Services;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace Identity.Tests.Services;

/// <summary>
/// Tests for UserRegistrationService.InviteAsync — focuses on the "don't invite an existing
/// user" guard (docs/04 Задачи/Задачи · Доработки каркаса.md → Identity): user creation, with
/// its built-in duplicate-email check (RequireUniqueEmail), happens before any reset token is
/// generated or invite mail enqueued, so a duplicate e-mail throws with none of that having run.
/// Follows the same UserManager-substitution approach as UserPasswordServiceTests. The happy
/// path (role assignment, default groups, mail content) needs a real EF context past
/// CreateAsync and is covered by the Integration.Tests invite flow instead.
/// </summary>
public sealed class UserRegistrationServiceTests
{
    private const string TenantId = "codefi";

    private readonly UserManager<FshUser> _userManager;
    private readonly IJobService _jobService;
    private readonly IMailService _mailService;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;

    public UserRegistrationServiceTests()
    {
        _userManager = Substitute.For<UserManager<FshUser>>(
            Substitute.For<IUserStore<FshUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _jobService = Substitute.For<IJobService>();
        _mailService = Substitute.For<IMailService>();
        _tenantAccessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();

        var mtContext = Substitute.For<IMultiTenantContext<AppTenantInfo>>();
        mtContext.TenantInfo.Returns(new AppTenantInfo(TenantId, TenantId, "Codefi"));
        _tenantAccessor.MultiTenantContext.Returns(mtContext);
    }

    private UserRegistrationService CreateSut() =>
        new(_userManager, null!, _jobService, _mailService, _tenantAccessor, null!);

    [Fact]
    public async Task InviteAsync_Should_ThrowAndNotSendMail_When_EmailAlreadyExists()
    {
        // Arrange — CreateAsync fails the way ASP.NET Identity does for a duplicate e-mail
        // (RequireUniqueEmail is on, see IdentityModule).
        _userManager.CreateAsync(Arg.Any<FshUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Email already taken." }));

        var sut = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => sut.InviteAsync(
            "Jamie", "Rivera", "jamie.rivera@codefi.com.br", "Teacher", "https://appbase.codefi.com.br", CancellationToken.None));

        // No token was minted and no mail was queued — the duplicate check runs before either.
        await _userManager.DidNotReceive().GeneratePasswordResetTokenAsync(Arg.Any<FshUser>());
        _jobService.DidNotReceive().Enqueue(Arg.Any<Expression<Func<Task>>>());
        await _mailService.DidNotReceive().SendAsync(Arg.Any<MailRequest>(), Arg.Any<CancellationToken>());
    }
}
