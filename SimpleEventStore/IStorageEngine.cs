using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventStore
{
    public interface IStorageEngine
    {
        Task AppendToStream(string streamId, IEnumerable<StorageEvent> events);

        Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead);

        Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwardsFromLast(string streamId, Predicate<StorageEvent> readFromHere);

        Task<IStorageEngine> Initialise();

        Task DeleteStream(string streamId);

        Task<bool> IsHealthy();
    }
}