using System;
using System.Collections;
using System.Threading.Tasks;

namespace CoEdit.Shared.Kernel.Contracts;

public interface IHistoryService
{
    Task SaveDeltasAsync(IEnumerable<DocumentDeltaDto> deltas);
}

public record DocumentDeltaDto(Guid DocumentId, string Delta, DateTime Timestamp);