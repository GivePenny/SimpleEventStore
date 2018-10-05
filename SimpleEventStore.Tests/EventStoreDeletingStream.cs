using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SimpleEventStore.Tests.Events;

namespace SimpleEventStore.Tests
{
    [TestFixture]
    public abstract class EventStoreDeletingStream : EventStoreTestBase
    {
        [Test]
        public async Task when_deleting_stream_all_events_in_stream_are_deleted()
        {
            var streamId = Guid.NewGuid().ToString();
            var subject = await GetEventStore();
            var @event = new EventData(Guid.NewGuid(), new OrderCreated(streamId));
            
            await subject.AppendToStream(streamId, 0, @event);

            await subject.DeleteStream(streamId);

            var stream = await subject.ReadStreamForwards(streamId);
            Assert.That(stream.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task when_deleting_stream_events_in_other_streams_are_preserved()
        {
            var deleteStreamId = Guid.NewGuid().ToString();
            var keepStreamId = Guid.NewGuid().ToString();
            var subject = await GetEventStore();

            await subject.AppendToStream(keepStreamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(keepStreamId)));
            await subject.AppendToStream(deleteStreamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(deleteStreamId)));

            await subject.DeleteStream(deleteStreamId);

            var stream = await subject.ReadStreamForwards(keepStreamId);
            Assert.That(stream.Count, Is.EqualTo(1));
        }
    }
}
