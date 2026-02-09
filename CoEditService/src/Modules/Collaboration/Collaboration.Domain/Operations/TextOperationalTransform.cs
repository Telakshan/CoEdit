using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueObjects;

namespace Collaboration.Domain.Operations;

public class TextOperationalTransform: IOperationalTransform
{
    public Operation Transform(Operation op1, Operation op2, OperationPriority priority)
    {

        var op1New = new Operation(
            op1.DocumentId,
            op1.UserId,
            op1.Type,
            op1.Position,
            op1.Content,
            op1.Version
        )
        {
            OperationId = op1.OperationId,
            Length = op1.Length,
            Timestamp = op1.Timestamp
        };

        if (op1.Type == OperationType.Insert && op2.Type == OperationType.Insert)
        {
            if (op1.Position < op2.Position || (op1.Position == op2.Position && priority == OperationPriority.Left))
            {
                // op1 is to the left, no change
            }
            else
            {
                op1New.Position += op2.Content?.Length ?? 0;
            }
        }
        else if (op1.Type == OperationType.Insert && op2.Type == OperationType.Delete)
        {
            if (op1.Position <= op2.Position)
            {
                // op1 is before deletion, no change
            }
            else if (op1.Position > op2.Position)
            {
              
                int deleteLength = op2.Length > 0 ? op2.Length : (op2.Content?.Length ?? 1);

                if (op1.Position >= op2.Position + deleteLength) {
                     op1New.Position -= deleteLength;
                } else {
                     op1New.Position = op2.Position;
                }
            }
        }
        else if (op1.Type == OperationType.Delete && op2.Type == OperationType.Insert)
        {
            if (op1.Position < op2.Position)
            {
            }
            else
            {
                op1New.Position += op2.Content?.Length ?? 0;
            }
        }
        else if (op1.Type == OperationType.Delete && op2.Type == OperationType.Delete)
        {
             int op2Len = op2.Length > 0 ? op2.Length : (op2.Content?.Length ?? 1);
             if (op1.Position >= op2.Position + op2Len)
             {
                 op1New.Position -= op2Len;
             }
             else if (op1.Position >= op2.Position)
             {
                 op1New.Type = OperationType.Retain;
             }
        }

        return op1New;
    }
    
    public string ApplyOperation(string content, Operation operation)
    {
        if (operation.Type == OperationType.Insert)
        {
            if (operation.Position > content.Length)
                return content + operation.Content;
            return content.Insert(operation.Position, operation.Content ?? string.Empty);
        }
        if (operation.Type == OperationType.Delete)
        {
            int length = operation.Length > 0 ? operation.Length : (operation.Content?.Length ?? 1);
            if (operation.Position >= content.Length) return content;
            if (operation.Position + length > content.Length)
                length = content.Length - operation.Position;

            return content.Remove(operation.Position, length);
        }

        return content;
    }
    
    public IEnumerable<Operation> TransformAgainstConcurrent(Operation operation, IEnumerable<Operation> concurrent)
    {
        var currentOp = operation;
        foreach (var pastOp in concurrent)
        {
            var priority = OperationPriority.Left;
            if (currentOp.UserId.CompareTo(pastOp.UserId) > 0)
                priority = OperationPriority.Right;

            currentOp = Transform(currentOp, pastOp, priority);
        }
        yield return currentOp;
    }
}