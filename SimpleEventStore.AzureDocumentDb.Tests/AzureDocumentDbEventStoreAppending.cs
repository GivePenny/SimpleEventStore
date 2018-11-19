using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using NUnit.Framework;
using SimpleEventStore.Tests;
using SimpleEventStore.Tests.Events;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    [TestFixture]
    public class AzureDocumentDbEventStoreAppending : EventStoreAppending
    {
        protected override Task<IStorageEngine> CreateStorageEngine()
        {
            return StorageEngineFactory.Create("AppendingTests");
        }

        [Test]
        public async Task when_document_ttl_is_configured_then_documents_have_that_ttl_set()
        {
            const string DatabaseName = "AppendingTestsCosmosOnly";

            var client = DocumentClientFactory.Create(DatabaseName);
            var collectionName = "TtlTests_" + Guid.NewGuid();
            var storageEngine = await StorageEngineFactory.Create(DatabaseName,
                o =>
                {
                    o.CollectionName = collectionName;
                    o.DefaultTimeToLiveSeconds = -1;
                    o.DocumentTimeToLiveSeconds = 10;
                });

            await storageEngine.Initialise();

            var streamId = Guid.NewGuid().ToString();
            var store = new EventStore(storageEngine);
            await store.AppendToStream(streamId, 0, new EventData(Guid.NewGuid(), new OrderCreated(streamId)));

            var commitsLink = UriFactory.CreateDocumentCollectionUri(DatabaseName, collectionName);
            var eventsQuery = client.CreateDocumentQuery<DocumentDbStorageEvent>(commitsLink)
                .Where(x => x.StreamId == streamId)
                .OrderBy(x => x.EventNumber)
                .AsDocumentQuery();

            var response = await eventsQuery.ExecuteNextAsync<DocumentDbStorageEvent>();
            Assert.That(response.First().TimeToLiveSeconds, Is.EqualTo(10));
        }
    }
}
