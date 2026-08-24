using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Rooms;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.CreateRoom;

public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Location).MaximumLength(256);
    }
}
