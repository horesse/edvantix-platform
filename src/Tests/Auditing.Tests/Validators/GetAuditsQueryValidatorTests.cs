using FSH.Modules.Auditing.Contracts.v1.GetAudits;
using FSH.Modules.Auditing.Features.v1.GetAudits;

namespace Auditing.Tests.Validators;

public sealed class GetAuditsQueryValidatorTests
{
    private readonly GetAuditsQueryValidator _validator = new();

    [Fact]
    public void Passes_When_No_Entity_Filters()
        => _validator.Validate(new GetAuditsQuery()).IsValid.ShouldBeTrue();

    [Fact]
    public void Passes_When_EntityName_And_EntityKey_Both_Set()
    {
        var result = _validator.Validate(new GetAuditsQuery
        {
            EntityName = "Student",
            EntityKey = "Id:3f2504e0-4f89-11d3-9a0c-0305e82c3301",
        });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Passes_When_Only_EntityName_Set()
        => _validator.Validate(new GetAuditsQuery { EntityName = "Student" }).IsValid.ShouldBeTrue();

    [Fact]
    public void Fails_When_EntityKey_Set_Without_EntityName()
    {
        var result = _validator.Validate(new GetAuditsQuery { EntityKey = "Id:abc" });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAuditsQuery.EntityName));
    }
}
