using System;
using System.Collections.Generic;

namespace SimpleEventStore.InMemory
{
    public class InMemoryEventStreamChanged : EventArgs
    {
        public InMemoryEventStreamChanged(IReadOnlyList<StorageEvent> newEvents)
        {
            NewEvents = newEvents;
        }

        public IReadOnlyList<StorageEvent> NewEvents { get; }
    }
}
