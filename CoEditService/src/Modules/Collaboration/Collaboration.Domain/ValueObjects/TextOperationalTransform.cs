using Collaboration.Domain.Entities;

namespace Collaboration.Domain.ValueObjects;

public class TextOperationalTransform : IOperationalTransform
{
    public Operation Transform(Operation op1, Operation op2, OperationPriority priority)
    {
        var op1New = CloneOperation(op1);

        if (op1New.Type == OperationType.Retain)
        {
            return op1New;
        }

        if (op1New.Type == OperationType.Delete && GetDeleteLength(op1New) <= 0)
        {
            return ToNoOp(op1New);
        }

        if (op2.Type == OperationType.Retain)
        {
            return op1New;
        }

        return (op1.Type, op2.Type) switch
        {
            (OperationType.Insert, OperationType.Insert) => TransformInsertAgainstInsert(op1New, op2, priority),
            (OperationType.Insert, OperationType.Delete) => TransformInsertAgainstDelete(op1New, op2, priority),
            (OperationType.Delete, OperationType.Insert) => TransformDeleteAgainstInsert(op1New, op2, priority),
            (OperationType.Delete, OperationType.Delete) => TransformDeleteAgainstDelete(op1New, op2, priority),
            _ => op1New
        };
    }

    public string ApplyOperation(string content, Operation operation)
    {
        var safeContent = content ?? string.Empty;
        var safePosition = Math.Max(0, operation.Position);

        if (operation.Type == OperationType.Insert)
        {
            var insertText = operation.Content ?? string.Empty;
            if (insertText.Length == 0)
            {
                return safeContent;
            }

            var insertPosition = Math.Min(safePosition, safeContent.Length);
            return safeContent.Insert(insertPosition, insertText);
        }

        if (operation.Type == OperationType.Delete)
        {
            var deletePosition = Math.Min(safePosition, safeContent.Length);
            var length = GetDeleteLength(operation);
            if (length <= 0 || deletePosition >= safeContent.Length)
            {
                return safeContent;
            }

            if (deletePosition + length > safeContent.Length)
            {
                length = safeContent.Length - deletePosition;
            }

            return safeContent.Remove(deletePosition, length);
        }

        return safeContent;
    }

    public IEnumerable<Operation> TransformAgainstConcurrent(Operation operation, IEnumerable<Operation> concurrent)
    {
        var currentOp = operation;
        var orderedConcurrent = concurrent
            .Where(pastOp => pastOp.DocumentId == operation.DocumentId)
            .OrderBy(pastOp => pastOp.Version)
            .ThenBy(pastOp => pastOp.Timestamp)
            .ThenBy(pastOp => pastOp.OperationId);

        foreach (var pastOp in orderedConcurrent)
        {
            var priority = DeterminePriority(currentOp, pastOp);
            currentOp = Transform(currentOp, pastOp, priority);
        }

        yield return currentOp;
    }

    public string Combine(string baseContent, IEnumerable<Operation> operations)
    {
        return operations.Aggregate(baseContent, ApplyOperation);
    }

    public Operation Compose(Operation op1, Operation op2)
    {
        if (op1.Type == OperationType.Insert && op2.Type == OperationType.Insert)
        {
            if (op2.Position == op1.Position + (op1.Content?.Length ?? 0))
            {
                var result = CloneOperation(op1);
                result.Content = op1.Content + op2.Content;
                result.Version = op2.Version;
                return result;
            }
        }

        if (op1.Type == OperationType.Delete && op2.Type == OperationType.Delete)
        {
            if (op2.Position + GetDeleteLength(op2) == op1.Position)
            {
                var result = CloneOperation(op2);
                result.Length = GetDeleteLength(op1) + GetDeleteLength(op2);
                result.Version = op2.Version;
                return result;
            }
        }

        return op2;
    }

    private static Operation CloneOperation(Operation operation)
    {
        return new Operation(
            operation.DocumentId,
            operation.UserId,
            operation.Type,
            operation.Position,
            operation.Content,
            operation.Version,
            operation.Length
        )
        {
            OperationId = operation.OperationId,
            Timestamp = operation.Timestamp
        };
    }

    private static Operation TransformInsertAgainstInsert(Operation current, Operation past, OperationPriority priority)
    {
        var p1 = current.Position;
        var p2 = past.Position;
        var l2 = GetInsertLength(past);

        if (p1 < p2 || (p1 == p2 && priority == OperationPriority.Left))
        {
            return current;
        }

        current.Position += l2;
        return current;
    }

    private static Operation TransformInsertAgainstDelete(Operation current, Operation past, OperationPriority priority)
    {
        var p1 = current.Position;
        var p2 = past.Position;
        var l2 = GetDeleteLength(past);

        if (p1 < p2 || (p1 == p2 && priority == OperationPriority.Left))
        {
            return current;
        }

        if (p1 >= p2 + l2)
        {
            current.Position -= l2;
            return current;
        }

        // Insert at the exact boundary of the delete range with Right priority:
        // The insert adds new text at the delete start position. It survives because
        // it targets a position boundary, not content being deleted.
        if (p1 == p2)
        {
            return current;
        }

        // Insert strictly inside the deleted range (p2 < p1 < p2 + l2).
        // A single-operation OT cannot preserve the insert while maintaining
        // convergence (TP1), so the insert is swallowed.
        return ToNoOp(current);
    }

    private static Operation TransformDeleteAgainstInsert(Operation current, Operation past, OperationPriority priority)
    {
        var p1 = current.Position;
        var l1 = GetDeleteLength(current);
        var p2 = past.Position;
        var l2 = GetInsertLength(past);

        if (p2 <= p1)
        {
            // Insert at or before delete start — shift delete right to preserve original target
            current.Position += l2;
            return current;
        }

        if (p2 > p1 && p2 < p1 + l1)
        {
            // Insert is strictly inside the delete range — extend delete to cover inserted text
            current.Length += l2;
            return current;
        }

        return current;
    }

    private static Operation TransformDeleteAgainstDelete(Operation current, Operation past, OperationPriority priority)
    {
        var cl = GetDeleteLength(current);
        var pl = GetDeleteLength(past);
        if (cl <= 0) return ToNoOp(current);
        if (pl <= 0) return current;

        var c1 = current.Position;
        var c2 = c1 + cl;
        var p1 = past.Position;
        var p2 = p1 + pl;

        if (p2 <= c1)
        {
            current.Position -= pl;
        }
        else if (p1 >= c2)
        {
            // After, no change.
        }
        else
        {
            // Overlap
            if (p1 <= c1 && p2 >= c2) return ToNoOp(current);
            
            if (p1 < c1)
            {
                current.Position = p1;
            }
            
            var intersectStart = Math.Max(c1, p1);
            var intersectEnd = Math.Min(c2, p2);
            current.Length -= (intersectEnd - intersectStart);
            
            if (current.Length <= 0) return ToNoOp(current);
        }

        return current;
    }

    private static OperationPriority DeterminePriority(Operation currentOp, Operation pastOp)
    {
        if (currentOp.OperationId != Guid.Empty &&
            pastOp.OperationId != Guid.Empty)
        {
            if (currentOp.OperationId == pastOp.OperationId) return OperationPriority.Left;
            return currentOp.OperationId.CompareTo(pastOp.OperationId) < 0 
                ? OperationPriority.Left 
                : OperationPriority.Right;
        }

        if (currentOp.UserId != pastOp.UserId)
        {
            return currentOp.UserId.CompareTo(pastOp.UserId) < 0
                ? OperationPriority.Left
                : OperationPriority.Right;
        }

        return OperationPriority.Left;
    }

    private static int GetInsertLength(Operation operation)
    {
        return operation.Content?.Length ?? 0;
    }

    private static int GetDeleteLength(Operation operation)
    {
        if (operation.Length > 0) return operation.Length;
        return operation.Content?.Length ?? 0;
    }

    private static Operation ToNoOp(Operation operation)
    {
        return new Operation(
            operation.DocumentId,
            operation.UserId,
            OperationType.Retain,
            operation.Position,
            null,
            operation.Version,
            0
        )
        {
            OperationId = operation.OperationId,
            Timestamp = operation.Timestamp
        };
    }
}