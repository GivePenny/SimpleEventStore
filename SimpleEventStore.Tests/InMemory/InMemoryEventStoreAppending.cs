using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SimpleEventStore.InMemory;
using SimpleEventStore.Tests.Events;

namespace SimpleEventStore.Tests.InMemory
{
    [TestFixture]
    public class InMemoryEventStoreAppending : EventStoreAppending
    {
        private InMemoryEventStreamChanged lastReceivedInMemoryEventStreamChangedEvent;
        private readonly TestMetadata metadata = new TestMetadata { Value = "Hello" };

        protected override Task<IStorageEngine> CreateStorageEngine()
        {
            var storageEngine = new InMemoryStorageEngine();
            storageEngine.OnStreamChanged += StorageEngineOnStreamChanged;
            return Task.FromResult((IStorageEngine)storageEngine);
        }

        private void StorageEngineOnStreamChanged(object sender, InMemoryEventStreamChanged e)
        {
            lastReceivedInMemoryEventStreamChangedEvent = e;
        }

        [Test]
        public async Task WhenAppendingToANewStreamAStreamChangedEventIsPublished()
        {
            var streamId = Guid.NewGuid().ToString();
            var subject = await GetEventStore();
            var @event = new EventData(Guid.NewGuid(), new OrderCreated(streamId), metadata);

            await subject.AppendToStream(streamId, 0, @event);

            Assert.IsNotNull(lastReceivedInMemoryEventStreamChangedEvent);
            Assert.AreEqual(1, lastReceivedInMemoryEventStreamChangedEvent.NewEvents.Count);

            var newEvent = lastReceivedInMemoryEventStreamChangedEvent.NewEvents.Single();
            Assert.AreEqual(@event.EventId, newEvent.EventId);
            Assert.AreEqual(@event.Body, newEvent.EventBody);
            Assert.AreEqual(@event.Metadata, newEvent.Metadata);
            Assert.AreEqual(1, newEvent.EventNumber);
        }

        [Test]
        public async Task WhenAppendingToAnExistingStreamAStreamChangedEventIsPublished()
        {
            var streamId = Guid.NewGuid().ToString();
            var subject = await GetEventStore();
            await subject.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(streamId), metadata));
            var @event = new EventData(Guid.NewGuid(), new OrderDispatched(streamId), metadata);

            await subject.AppendToStream(streamId, 1, @event);

            Assert.IsNotNull(lastReceivedInMemoryEventStreamChangedEvent);
            Assert.AreEqual(1, lastReceivedInMemoryEventStreamChangedEvent.NewEvents.Count);

            var newEvent = lastReceivedInMemoryEventStreamChangedEvent.NewEvents.Single();
            Assert.AreEqual(@event.EventId, newEvent.EventId);
            Assert.AreEqual(@event.Body, newEvent.EventBody);
            Assert.AreEqual(@event.Metadata, newEvent.Metadata);
            Assert.AreEqual(2, newEvent.EventNumber);
        }

        [Test]
        public async Task WhenAppendingToANewStreamWithMultipleEventsAStreamChangedEventIsPublished()
        {
            var streamId = Guid.NewGuid().ToString();
            var subject = await GetEventStore();
            var events = new[]
            {
                new EventData(Guid.NewGuid(), new OrderCreated(streamId), metadata),
                new EventData(Guid.NewGuid(), new OrderDispatched(streamId), metadata)
            };

            await subject.AppendToStream(streamId, 0, events);

            Assert.IsNotNull(lastReceivedInMemoryEventStreamChangedEvent);
            Assert.AreEqual(2, lastReceivedInMemoryEventStreamChangedEvent.NewEvents.Count);

            var firstNewEvent = lastReceivedInMemoryEventStreamChangedEvent.NewEvents.First();
            Assert.AreEqual(events[0].EventId, firstNewEvent.EventId);
            Assert.AreEqual(events[0].Body, firstNewEvent.EventBody);
            Assert.AreEqual(events[0].Metadata, firstNewEvent.Metadata);
            Assert.AreEqual(1, firstNewEvent.EventNumber);

            var lastNewEvent = lastReceivedInMemoryEventStreamChangedEvent.NewEvents.Last();
            Assert.AreEqual(events[1].EventId, lastNewEvent.EventId);
            Assert.AreEqual(events[1].Body, lastNewEvent.EventBody);
            Assert.AreEqual(events[1].Metadata, lastNewEvent.Metadata);
            Assert.AreEqual(2, lastNewEvent.EventNumber);
        }

        [Test]
        public async Task WhenAppendingToAnExistingStreamWithMultipleEventsAStreamChangedEventIsPublished()
        {
            var streamId = Guid.NewGuid().ToString();
            var subject = await GetEventStore();
            await subject.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(streamId), metadata));
            var events = new[]
            {
                new EventData(Guid.NewGuid(), new OrderCreated(streamId), metadata),
                new EventData(Guid.NewGuid(), new OrderDispatched(streamId), metadata)
            };

            await subject.AppendToStream(streamId, 1, events);

            Assert.IsNotNull(lastReceivedInMemoryEventStreamChangedEvent);
            Assert.AreEqual(2, lastReceivedInMemoryEventStreamChangedEvent.NewEvents.Count);

            var firstNewEvent = lastReceivedInMemoryEventStreamChangedEvent.NewEvents.First();
            Assert.AreEqual(events[0].EventId, firstNewEvent.EventId);
            Assert.AreEqual(events[0].Body, firstNewEvent.EventBody);
            Assert.AreEqual(events[0].Metadata, firstNewEvent.Metadata);
            Assert.AreEqual(2, firstNewEvent.EventNumber);

            var lastNewEvent = lastReceivedInMemoryEventStreamChangedEvent.NewEvents.Last();
            Assert.AreEqual(events[1].EventId, lastNewEvent.EventId);
            Assert.AreEqual(events[1].Body, lastNewEvent.EventBody);
            Assert.AreEqual(events[1].Metadata, lastNewEvent.Metadata);
            Assert.AreEqual(3, lastNewEvent.EventNumber);
        }
    }
}