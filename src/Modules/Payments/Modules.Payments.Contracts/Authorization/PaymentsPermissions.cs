using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Payments.Contracts.Authorization;

public static class PaymentsPermissions
{
    public static class Tariffs
    {
        public const string Resource = "Payments.Tariffs";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Manage = $"Permissions.{Resource}.Manage";
    }

    public static class StudentInvoices
    {
        public const string Resource = "Payments.StudentInvoices";
        public const string View     = $"Permissions.{Resource}.View";
        public const string ViewOwn  = $"Permissions.{Resource}.ViewOwn";
        public const string Create   = $"Permissions.{Resource}.Create";
        public const string Issue    = $"Permissions.{Resource}.Issue";
        public const string Cancel   = $"Permissions.{Resource}.Cancel";
        public const string Export   = $"Permissions.{Resource}.Export";
    }

    public static class StudentPayments
    {
        public const string Resource = "Payments.StudentPayments";
        public const string View    = $"Permissions.{Resource}.View";
        public const string Confirm = $"Permissions.{Resource}.Confirm";
        public const string Revoke  = $"Permissions.{Resource}.Revoke";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Tariffs",   ActionConstants.View, Tariffs.Resource, IsBasic: true),
        new("Manage Tariffs", "Manage",              Tariffs.Resource),

        new("View Student Invoices",     ActionConstants.View,   StudentInvoices.Resource, IsBasic: true),
        new("View Own Student Invoices", "ViewOwn",                StudentInvoices.Resource, IsBasic: true),
        new("Create Student Invoices",   ActionConstants.Create, StudentInvoices.Resource),
        new("Issue Student Invoices",    "Issue",                  StudentInvoices.Resource),
        new("Cancel Student Invoices",   "Cancel",                 StudentInvoices.Resource),
        new("Export Student Invoices",   "Export",                 StudentInvoices.Resource),

        new("View Student Payments",    ActionConstants.View, StudentPayments.Resource, IsBasic: true),
        new("Confirm Student Payments", "Confirm",             StudentPayments.Resource),
        new("Revoke Student Payments",  "Revoke",              StudentPayments.Resource),
    ];
}
