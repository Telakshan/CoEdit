using Collaboration.Application.DataTransferObjects;
using Collaboration.Application.Services;
using Collaboration.Domain.Abstract;
using Collaboration.Domain.Operations;
using MediatR;

namespace Collaboration.Application.Commands;

public class ProcessOperationCommandHandler(
    ISessionStateService sessionService,
    IOperationalTransform ot,
    IDistributedLockService lockService)
    : IRequestHandler<ProcessOperationCommand, OperationDto>
{
    public async Task<OperationDto> Handle(ProcessOperationCommand request, CancellationToken cancellationToken)
    {
        var operationDto = request.Operation;
        var documentId = operationDto.DocumentId;

        // Acquire lock to ensure atomic processing of operation for this document
        // We retry a few times if lock is busy
        IAsyncDisposable? lockHandle = null;
        for (int i = 0; i < 5; i++)
        {
            lockHandle = await lockService.AcquireLockAsync(documentId.ToString(), TimeSpan.FromSeconds(5));
            if (lockHandle != null) break;
            await Task.Delay(50, cancellationToken);
        }

        if (lockHandle == null)
        {
            throw new Exception("Could not acquire lock for document operation processing.");
        }

        try
        {
            // 1. Get Current Version (Source of Truth)
            var currentVersion = await sessionService.GetVersionAsync(documentId);

            var op = operationDto.ToEntity();

            // 2. Transform if client version is behind
            // If op.Version (client's view) <= currentVersion (server's view), it means other ops happened
            if (op.Version <= currentVersion)
            {
                var missedOps = await sessionService.GetOperationsAfterVersionAsync(documentId, op.Version);

                var transformedOps = ot.TransformAgainstConcurrent(op, missedOps);
                op = transformedOps.First();
            }

            // 3. Increment Version and Persist

            var nextVersion = await sessionService.IncrementVersionAsync(documentId);
            op.Version = (int)nextVersion;

            await sessionService.AddOperationAsync(op);

            // Return back DTO
            return new OperationDto
            {
                 DocumentId = op.DocumentId,
                 UserId = op.UserId,
                 Type = op.Type.ToString(),
                 Position = op.Position,
                 Content = op.Content,
                 Version = op.Version,
                 ClientOperationId = operationDto.ClientOperationId
            };
        }
        finally
        {
            await lockHandle.DisposeAsync();
        }
    }
}