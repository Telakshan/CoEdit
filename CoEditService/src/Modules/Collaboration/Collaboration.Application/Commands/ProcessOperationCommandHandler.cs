using CoEdit.Common.Domain.Shared;
using Collaboration.Application.DataTransferObjects;
using Collaboration.Application.Services;
using Collaboration.Domain.Abstract;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Operations;
using MediatR;

namespace Collaboration.Application.Commands;

public class ProcessOperationCommandHandler(
    ISessionStateService sessionService,
    IOperationalTransform ot,
    IDistributedLockService lockService,
    IAuthorizationService authorizationService)
    : ICommandHandler<ProcessOperationCommand, OperationDto>
{
    public async Task<Result<OperationDto>> Handle(ProcessOperationCommand request, CancellationToken cancellationToken)
    {
        var operationDto = request.Operation;

        if (!await authorizationService.CanAccessAsync(operationDto.UserId, operationDto.DocumentId, AccessLevel.Editor))
        {
            return Result.Failure<OperationDto>("You do not have permission to edit this document.");
        }

        var validationResult = ValidateAndCreateOperation(operationDto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<OperationDto>(validationResult.Error);
        }

        var op = validationResult.Value;
        var baseVersion = op.Version;
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
            return Result.Failure<OperationDto>("Could not acquire lock for document operation processing.");
        }

        try
        {
            // 1. Get Current Version (Source of Truth)
            var currentVersion = await sessionService.GetVersionAsync(documentId);
            var existingOperation = await sessionService.GetOperationByIdAsync(documentId, operationDto.ClientOperationId);
            if (existingOperation is not null)
            {
                return Result.Success(MapToDto(existingOperation, operationDto.ClientOperationId, baseVersion));
            }

            if (op.Version > currentVersion)
            {
                return Result.Failure<OperationDto>(
                    $"Operation base version {op.Version} is ahead of server version {currentVersion}.");
            }

            // 2. Transform only if client version is behind.
            // Equal versions mean there are no missed operations to transform against.
            if (op.Version < currentVersion)
            {
                var missedOps = await sessionService.GetOperationsAfterVersionAsync(documentId, op.Version) ?? [];
                if (!HasCompleteReplayWindow(op.Version, currentVersion, missedOps))
                {
                    return Result.Failure<OperationDto>(
                        $"Operation base version {op.Version} is too old for server replay at version {currentVersion}. Request document resync.");
                }

                var transformedOp = ot.TransformAgainstConcurrent(op, missedOps).FirstOrDefault();
                if (transformedOp == null)
                {
                    return Result.Failure<OperationDto>("Operation transform failed.");
                }

                op = transformedOp;
            }

            // 3. Apply transformed operation to authoritative session content.
            var currentContent = await sessionService.GetDocumentContentAsync(documentId) ?? string.Empty;
            var updatedContent = ot.ApplyOperation(currentContent, op);
            if (updatedContent is null)
            {
                updatedContent = currentContent;
            }

            // 4. Increment Version and Persist operation + new content.

            var nextVersion = await sessionService.IncrementVersionAsync(documentId);
            op.SetVersion((int)nextVersion);

            await sessionService.AddOperationAsync(op);
            await sessionService.SetDocumentContentAsync(documentId, updatedContent);

            // Return back DTO
            return Result.Success(MapToDto(op, operationDto.ClientOperationId, baseVersion));
        }
        finally
        {
            await lockHandle.DisposeAsync();
        }
    }

    private static Result<Operation> ValidateAndCreateOperation(OperationDto operationDto)
    {
        var baseVersion = ResolveBaseVersion(operationDto);

        if (operationDto.DocumentId == Guid.Empty)
        {
            return Result.Failure<Operation>("DocumentId is required.");
        }

        if (operationDto.UserId == Guid.Empty)
        {
            return Result.Failure<Operation>("UserId is required.");
        }

        if (operationDto.ClientOperationId == Guid.Empty)
        {
            return Result.Failure<Operation>("ClientOperationId is required.");
        }

        if (!Enum.TryParse<OperationType>(operationDto.Type, true, out var operationType))
        {
            return Result.Failure<Operation>($"Unsupported operation type '{operationDto.Type}'.");
        }

        if (operationDto.Position < 0)
        {
            return Result.Failure<Operation>("Position cannot be negative.");
        }

        if (baseVersion < 0)
        {
            return Result.Failure<Operation>("BaseVersion cannot be negative.");
        }

        if (operationType == OperationType.Insert && string.IsNullOrEmpty(operationDto.Content))
        {
            return Result.Failure<Operation>("Insert operation requires non-empty content.");
        }

        if (operationDto.Content is not null && operationDto.Content.Length > 1024 * 1024)
        {
            return Result.Failure<Operation>("Operation content exceeds the 1MB size limit.");
        }

        if ((operationType == OperationType.Delete || operationType == OperationType.Retain) &&
            operationDto.Length <= 0)
        {
            return Result.Failure<Operation>($"{operationType} operation requires explicit positive Length.");
        }

        if ((operationType == OperationType.Delete || operationType == OperationType.Retain) &&
            !string.IsNullOrEmpty(operationDto.Content))
        {
            return Result.Failure<Operation>($"{operationType} operation must not include Content.");
        }

        var normalizedContent = operationType == OperationType.Insert
            ? operationDto.Content ?? string.Empty
            : operationDto.Content;

        var op = new Operation(
            operationDto.DocumentId,
            operationDto.UserId,
            operationType,
            operationDto.Position,
            normalizedContent,
            baseVersion,
            operationDto.Length)
        {
            OperationId = operationDto.ClientOperationId
        };

        return Result.Success(op);
    }

    private static int ResolveBaseVersion(OperationDto operationDto)
    {
        // Backward compatibility for clients still sending Version as the client base.
        return operationDto.BaseVersion == 0 && operationDto.Version > 0
            ? operationDto.Version
            : operationDto.BaseVersion;
    }

    private static bool HasCompleteReplayWindow(int baseVersion, long currentVersion, IReadOnlyCollection<Operation> missedOps)
    {
        var expectedCountLong = currentVersion - baseVersion;
        if (expectedCountLong <= 0)
        {
            return true;
        }

        if (expectedCountLong > int.MaxValue)
        {
            return false;
        }

        var expectedCount = (int)expectedCountLong;
        if (missedOps.Count != expectedCount)
        {
            return false;
        }

        var expectedVersion = baseVersion + 1;
        foreach (var version in missedOps.Select(operation => operation.Version).OrderBy(version => version))
        {
            if (version != expectedVersion)
            {
                return false;
            }

            expectedVersion++;
        }

        return true;
    }

    private static OperationDto MapToDto(Operation operation, Guid clientOperationId, int baseVersion)
    {
        return new OperationDto
        {
            DocumentId = operation.DocumentId,
            UserId = operation.UserId,
            ClientOperationId = clientOperationId,
            Type = operation.Type.ToString(),
            Position = operation.Position,
            Length = operation.Length,
            Content = operation.Content,
            BaseVersion = baseVersion,
            Version = operation.Version,
        };
    }
}