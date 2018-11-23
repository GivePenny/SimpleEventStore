using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleEventStore
{
    public class EventStore : IEventStore
    {
        private readonly IStorageEngine engine;

        public EventStore(IStorageEngine engine)
        {
            this.engine = engine;
        }

        public Task AppendToStream(string streamId, int expectedVersion, params EventData[] events)
        {
            Guard.IsNotNullOrEmpty(nameof(streamId), streamId);

            var storageEvents = new List<StorageEvent>();
            var eventVersion = expectedVersion;

            for (var i = 0; i < events.Length; i++)
            {
                storageEvents.Add(new StorageEvent(streamId, events[i], ++eventVersion));
            }

            return engine.AppendToStream(streamId, storageEvents);
        }

        public Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwards(string streamId)
        {
            Guard.IsNotNullOrEmpty(nameof(streamId), streamId);

            return engine.ReadStreamForwards(streamId, 1, int.MaxValue);
        }

        public Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead)
        {
            Guard.IsNotNullOrEmpty(nameof(streamId), streamId);

            return engine.ReadStreamForwards(streamId, startPosition, numberOfEventsToRead);
        }

        public Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwardsFromLast(string streamId, Predicate<StorageEvent> readFromHere)
        {
            Guard.IsNotNullOrEmpty(nameof(streamId), streamId);
            Guard.IsNotNull(nameof(readFromHere), readFromHere);

            return engine.ReadStreamForwardsFromLast(streamId, readFromHere);
        }

        public Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwardsFromLast<TSnapshot>(string streamId)
        {
            return ReadStreamForwardsFromLast(
                streamId,
                storageEvent => storageEvent.EventBody is TSnapshot);
        }

        public Task DeleteStream(string streamId)
        {
            Guard.IsNotNullOrEmpty(nameof(streamId), streamId);

            return engine.DeleteStream(streamId);
        }
    }
}