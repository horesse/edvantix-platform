using FSH.Modules.Auditing.Contracts.Catalog;
using FSH.Modules.Auditing.Contracts.v1.GetAuditLabels;
using FSH.Modules.Auditing.Features.v1.GetAuditLabels;

namespace Auditing.Tests.Catalog;

public sealed class AuditLabelCatalogTests
{
    [Fact]
    public void Entities_And_Fields_Are_NonEmpty_With_NonBlank_Labels()
    {
        AuditLabelCatalog.Entities.ShouldNotBeEmpty();
        AuditLabelCatalog.Fields.ShouldNotBeEmpty();
        AuditLabelCatalog.Entities.Values.ShouldAllBe(v => !string.IsNullOrWhiteSpace(v));
        AuditLabelCatalog.Fields.Values.ShouldAllBe(v => !string.IsNullOrWhiteSpace(v));
    }

    [Fact]
    public void Lookups_Are_Case_Insensitive()
    {
        AuditLabelCatalog.Entities.ContainsKey("student").ShouldBeTrue();
        AuditLabelCatalog.Fields.ContainsKey("STATUS").ShouldBeTrue();
    }

    [Fact]
    public void EntityLabel_Falls_Back_To_Raw_Name_When_Unknown()
    {
        AuditLabelCatalog.EntityLabel("Student").ShouldBe("Ученик");
        AuditLabelCatalog.EntityLabel("SomethingUncatalogued").ShouldBe("SomethingUncatalogued");
    }

    [Fact]
    public void FieldLabel_Falls_Back_To_Raw_Name_When_Unknown()
    {
        AuditLabelCatalog.FieldLabel("Status").ShouldBe("Статус");
        AuditLabelCatalog.FieldLabel("SomeSpecificColumn").ShouldBe("SomeSpecificColumn");
    }

    [Fact]
    public async Task Handler_Returns_The_Catalog_Dictionaries()
    {
        var handler = new GetAuditLabelsQueryHandler();

        var result = await handler.Handle(new GetAuditLabelsQuery(), CancellationToken.None);

        result.Entities.ShouldBe(AuditLabelCatalog.Entities);
        result.Fields.ShouldBe(AuditLabelCatalog.Fields);
    }
}
