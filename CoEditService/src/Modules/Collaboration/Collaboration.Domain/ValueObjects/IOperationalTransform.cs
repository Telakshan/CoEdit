using Collaboration.Domain.Entities;

namespace Collaboration.Domain.ValueObjects;

public interface IOperationalTransform
{
    Operation Transform(Operation op1, Operation op2, OperationPriority priority);
    string ApplyOperation(string content, Operation operation);
    IEnumerable<Operation> TransformAgainstConcurrent(Operation operation, IEnumerable<Operation> concurrent);
    Operation Compose(Operation op1, Operation op2);
    string Combine(string baseContent, IEnumerable<Operation> operations);
}