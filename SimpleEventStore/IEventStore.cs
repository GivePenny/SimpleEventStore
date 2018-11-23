using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventStore
{
    public interface IEventStore
    {
        Task AppendToStream(string streamId, int expectedVersion, params EventData[] events);
        Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwards(string streamId);
        Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead);
        Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwardsFromLast(string streamId, Predicate<StorageEvent> readFromHere);
        Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwardsFromLast<TSnapshot>(string streamId);
        Task DeleteStream(string streamId);
    }
}
