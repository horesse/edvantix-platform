using FSH.Modules.Files;
using FSH.Modules.Files.Services;
using Shouldly;

namespace Files.Tests.Services;

public class FileCategoryPolicyTests
{
    private static Dictionary<string, FileCategoryOptions> Config() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["Document"] = new FileCategoryOptions { AllowedExtensions = [".pdf"], MaxBytes = 100 },
        ["LessonMaterial"] = new FileCategoryOptions
        {
            AllowedExtensions = [".pdf"],
            MaxBytes = 100,
            OwnerTypes = ["LessonMaterial"],
        },
    };

    [Fact]
    public void Unbound_Category_Is_Allowed_For_Any_Owner_Type()
    {
        var outcome = FileCategoryPolicy.Check(Config(), "Document", "MyFiles", out _);

        outcome.ShouldBe(FileCategoryPolicy.Outcome.Allowed);
    }

    [Fact]
    public void Bound_Category_Allows_Its_Own_Owner_Type()
    {
        var outcome = FileCategoryPolicy.Check(Config(), "LessonMaterial", "LessonMaterial", out _);

        outcome.ShouldBe(FileCategoryPolicy.Outcome.Allowed);
    }

    [Fact]
    public void Bound_Category_Rejects_A_Foreign_Owner_Type()
    {
        var outcome = FileCategoryPolicy.Check(Config(), "LessonMaterial", "Ticket", out _);

        outcome.ShouldBe(FileCategoryPolicy.Outcome.CategoryNotForOwnerType);
    }

    [Fact]
    public void Owner_Type_Bound_Elsewhere_Cannot_Use_A_Looser_Category()
    {
        var outcome = FileCategoryPolicy.Check(Config(), "Document", "LessonMaterial", out var required);

        outcome.ShouldBe(FileCategoryPolicy.Outcome.OwnerTypeRequiresBoundCategory);
        required.ShouldBe(["LessonMaterial"]);
    }
}
