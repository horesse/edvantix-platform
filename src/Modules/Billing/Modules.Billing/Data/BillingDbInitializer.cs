using FSH.Framework.Persistence;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Billing.Data;

public sealed class BillingDbInitializer(
    BillingDbContext dbContext,
    ILogger<BillingDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Billing] applied migrations");
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        // Plans are a global catalogue (IGlobalEntity); seed defaults once. "free" backs the trial
        // fallback; keys align with QuotaOptions plan keys so quota limits resolve.
        if (await dbContext.Plans.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        // Plans are what a school pays Edvantix for. Keys stay stable ("free"/"pro"/"pro-annual")
        // because QuotaOptions.Plans and existing subscriptions key off them; only the school-facing
        // name and blurb are themed.
        dbContext.Plans.Add(BillingPlan.Create(
            "free", "Старт", "USD", 0m, interval: PlanInterval.Monthly,
            description: "Для знакомства и небольшой студии: до 5 учётных записей персонала, "
                + "1 ГБ под материалы. Расписание, группы, счета — без ограничений по функциям."));
        dbContext.Plans.Add(BillingPlan.Create(
            "pro", "Школа", "USD", 29m, interval: PlanInterval.Monthly,
            description: "Для действующей школы: до 100 учётных записей персонала и учеников, "
                + "100 ГБ под материалы уроков и записи занятий, вебхуки и журнал аудита. Оплата помесячно."));
        dbContext.Plans.Add(BillingPlan.Create(
            "pro-annual", "Школа (год)", "USD", 29m,
            interval: PlanInterval.Yearly, annualPrice: 290m,
            description: "Тариф «Школа» с оплатой за год — два месяца в подарок."));
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[Billing] seeded default plans (free, pro, pro-annual)");
    }
}
