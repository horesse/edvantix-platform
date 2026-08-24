using FSH.Modules.Payments.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.StudentInvoices;

public sealed record GetStudentBalanceQuery(Guid StudentId) : IQuery<StudentBalanceDto>;
