using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Rooms;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.UpdateRoom;

public sealed class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Location).MaximumLength(256);
    }
}
