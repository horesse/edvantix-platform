using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Rooms;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.DeleteRoom;

public sealed class DeleteRoomCommandValidator : AbstractValidator<DeleteRoomCommand>
{
    public DeleteRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
    }
}
