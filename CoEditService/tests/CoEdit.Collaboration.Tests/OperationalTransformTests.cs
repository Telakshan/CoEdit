using Collaboration.Domain.Entities;
using Collaboration.Domain.Operations;
using Collaboration.Domain.ValueObjects;
using Xunit;

namespace CoEdit.Collaboration.Tests;

public class OperationalTransformTests
{
    private readonly IOperationalTransform _ot = new TextOperationalTransform();

    [Fact]
    public void Transform_Insert_After_Insert_Shifts_Position()
    {
        // Setup
        var docId = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var op1 = new Operation(docId, user1, OperationType.Insert, 5, "World", 0);
        var op2 = new Operation(docId, user2, OperationType.Insert, 0, "Hello ", 0); // Happened first but concurrent

        // Act
        var transformedOp1 = _ot.Transform(op1, op2, OperationPriority.Left);

        // Assert
        Assert.Equal(11, transformedOp1.Position); // 5 + 6
        Assert.Equal("World", transformedOp1.Content);
    }
    
    
    [Fact]
    public void Transform_Insert_Before_Insert_Unchanged()
    {
        var docId = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        var op1 = new Operation(docId, user1, OperationType.Insert, 0, "Hello ", 0);
        var op2 = new Operation(docId, user2, OperationType.Insert, 5, "World", 0);

        var transformedOp1 = _ot.Transform(op1, op2, OperationPriority.Left);

        Assert.Equal(0, transformedOp1.Position);
    }

    [Fact]
    public void Apply_Insert_Works()
    {
        var content = "Hello";
        var op = new Operation(Guid.NewGuid(), Guid.NewGuid(), OperationType.Insert, 5, " World", 0);

        var result = _ot.ApplyOperation(content, op);

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Apply_Delete_Works()
    {
        var content = "Hello World";
        var op = new Operation(Guid.NewGuid(), Guid.NewGuid(), OperationType.Delete, 5, null, 0);
        op.Length = 6; // Delete " World"

        var result = _ot.ApplyOperation(content, op);

        Assert.Equal("Hello", result);
    }
}