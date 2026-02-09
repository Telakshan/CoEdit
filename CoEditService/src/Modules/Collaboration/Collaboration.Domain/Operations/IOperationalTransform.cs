using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueObjects;

namespace Collaboration.Domain.Operations;

public interface IOperationalTransform
{
    Operation Transform(Operation op1, Operation op2, OperationPriority priority);
    string ApplyOperation(string content, Operation operation);
    IEnumerable<Operation> TransformAgainstConcurrent(Operation operation, IEnumerable<Operation> concurrent);
}
