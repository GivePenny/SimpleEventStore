using Microsoft.Azure.Documents.Client;
using NUnit.Framework;
using System.Threading.Tasks;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    [TestFixture]
    public class AzureDocumentDbStorageEngineHealthTests
    {
        private const string DatabaseName = "HealthTestsCosmosOnly";
        private const string CollectionName = "HealthTests";

        private DocumentClient client;
        private IStorageEngine storageEngine;

        public AzureDocumentDbStorageEngineHealthTests()
        {
            client = DocumentClientFactory.Create(DatabaseName);
            storageEngine = StorageEngineFactory.Create(DatabaseName,
                o =>
                {
                    o.CollectionName = CollectionName;
                    o.DefaultTimeToLiveSeconds = -1;
                    o.DocumentTimeToLiveSeconds = 10;
                })
                .Result;

            storageEngine.Initialise().Wait();
        }

        [Test]
        public async Task when_storage_engine_has_been_initialised_and_is_healthy_is_healthy_returns_true()
        {
            Assert.That(await storageEngine.IsHealthy(), Is.True);
        }

        [Test]
        public async Task when_storage_engine_has_been_initialised_and_is_not_healthy_is_healthy_returns_false()
        {
            // Simulate unhealthy storage engine
            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);
            await client.DeleteDocumentCollectionAsync(documentCollectionUri);

            Assert.That(await storageEngine.IsHealthy(), Is.False);
        }
    }
}
