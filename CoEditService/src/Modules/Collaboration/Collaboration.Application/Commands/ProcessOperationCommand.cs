using Collaboration.Application.DataTransferObjects;
using MediatR;

namespace Collaboration.Application.Commands;

public record ProcessOperationCommand(OperationDto Operation) : IRequest<OperationDto>;