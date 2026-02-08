using Collaboration.Core.Entities;

namespace Collaboration.Core.Operations;

public interface IOperationTransform
{
    IEnumerable<Operation> TransformAgainstConcurrent(Operation operation, IEnumerable<Operation> concurrent);
}