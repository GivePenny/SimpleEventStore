using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace SimpleEventStore.AzureDocumentDb
{
    internal class AzureDocumentDbStorageEngine : IStorageEngine
    {
        private const string AppendStoredProcedureName = "appendToStream";
        private const string DeleteStoredProcedureName = "deleteStream";
        private const string ConcurrencyConflictErrorKey = "Concurrency conflict.";

        private readonly DocumentClient client;
        private readonly string databaseName;
        private readonly CollectionOptions collectionOptions;
        private readonly Uri commitsLink;
        private readonly Uri appendStoredProcedureLink;
        private readonly Uri deleteStoredProcedureLink;
        private readonly LoggingOptions loggingOptions;
        private readonly ISerializationTypeMap typeMap;

        internal AzureDocumentDbStorageEngine(DocumentClient client, string databaseName, CollectionOptions collectionOptions, LoggingOptions loggingOptions, ISerializationTypeMap typeMap)
        {
            this.client = client;
            this.databaseName = databaseName;
            this.collectionOptions = collectionOptions;
            commitsLink = UriFactory.CreateDocumentCollectionUri(databaseName, collectionOptions.CollectionName);
            appendStoredProcedureLink = UriFactory.CreateStoredProcedureUri(databaseName, collectionOptions.CollectionName, AppendStoredProcedureName);
            deleteStoredProcedureLink = UriFactory.CreateStoredProcedureUri(databaseName, collectionOptions.CollectionName, DeleteStoredProcedureName);
            this.loggingOptions = loggingOptions;
            this.typeMap = typeMap;
        }

        public async Task<IStorageEngine> Initialise()
        {
            await CreateDatabaseIfItDoesNotExist();
            await CreateCollectionIfItDoesNotExist();
            await CreateStoredProcedureIfItDoesNotExist(AppendStoredProcedureName, "appendToStream.js");
            await CreateStoredProcedureIfItDoesNotExist(DeleteStoredProcedureName, "deleteStream.js");

            return this;
        }

        public async Task AppendToStream(string streamId, IEnumerable<StorageEvent> events)
        {
            var documents = events
                .Select(
                    document => DocumentDbStorageEvent.FromStorageEvent(
                        document,
                        typeMap,
                        collectionOptions.DocumentTimeToLiveSeconds))
                .ToList();

            try
            {
                var result = await client.ExecuteStoredProcedureAsync<dynamic>(
                    appendStoredProcedureLink,
                    new RequestOptions
                    {
                        PartitionKey = new PartitionKey(streamId),
                        ConsistencyLevel = collectionOptions.ConsistencyLevel
                    },
                    documents);

                loggingOptions.OnSuccess(ResponseInformation.FromWriteResponse(nameof(AppendToStream), result));
            }
            catch (DocumentClientException exception)
            {
                if (exception.Error.Message.Contains(ConcurrencyConflictErrorKey))
                {
                    throw new ConcurrencyException(exception.Error.Message, exception);
                }

                throw;
            }
        }

        public async Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwards(string streamId, int startPosition, int numberOfEventsToRead)
        {
            var endPosition = numberOfEventsToRead == int.MaxValue ? int.MaxValue : startPosition + numberOfEventsToRead;

            var eventsQuery = client.CreateDocumentQuery<DocumentDbStorageEvent>(commitsLink)
                .Where(x => x.StreamId == streamId && x.EventNumber >= startPosition && x.EventNumber <= endPosition)
                .OrderBy(x => x.EventNumber)
                .AsDocumentQuery();

            var events = new List<StorageEvent>();

            while (eventsQuery.HasMoreResults)
            {
                var response = await eventsQuery.ExecuteNextAsync<DocumentDbStorageEvent>();
                loggingOptions.OnSuccess(ResponseInformation.FromReadResponse(nameof(ReadStreamForwards), response));

                foreach (var e in response)
                {
                    events.Add(e.ToStorageEvent(typeMap));
                }
            }

            return events.AsReadOnly();
        }

        public async Task<IReadOnlyCollection<StorageEvent>> ReadStreamForwardsFromLast(string streamId, Predicate<StorageEvent> readFromHere)
        {
            var eventsQuery = client.CreateDocumentQuery<DocumentDbStorageEvent>(commitsLink)
                .Where(x => x.StreamId == streamId)
                .OrderByDescending(x => x.EventNumber)
                .AsDocumentQuery();

            var eventsInReverseOrder = new List<StorageEvent>();
            var finished = false;

            while (!finished && eventsQuery.HasMoreResults)
            {
                var response = await eventsQuery.ExecuteNextAsync<DocumentDbStorageEvent>();
                loggingOptions.OnSuccess(ResponseInformation.FromReadResponse(nameof(ReadStreamForwardsFromLast), response));

                foreach (var e in response)
                {
                    var storageEvent = e.ToStorageEvent(typeMap);
                    eventsInReverseOrder.Add(storageEvent);

                    if (readFromHere(storageEvent))
                    {
                        finished = true;
                        break;
                    }
                }
            }

            return eventsInReverseOrder
                .Reverse<StorageEvent>()
                .ToList()
                .AsReadOnly();
        }

        public async Task DeleteStream(string streamId)
        {
            while (true)
            {
                var result = await client.ExecuteStoredProcedureAsync<dynamic>(
                        deleteStoredProcedureLink,
                        new RequestOptions { PartitionKey = new PartitionKey(streamId), ConsistencyLevel = collectionOptions.ConsistencyLevel },
                        streamId);

                if ((bool)result.Response.continuation)
                {
                    continue;
                }

                loggingOptions.OnSuccess(ResponseInformation.FromWriteResponse(nameof(DeleteStream), result));

                break;
            }
        }

        private async Task CreateDatabaseIfItDoesNotExist()
        {
            await client.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });
        }

        private async Task CreateCollectionIfItDoesNotExist()
        {
            var databaseUri = UriFactory.CreateDatabaseUri(databaseName);

            var collection = new DocumentCollection
            {
                Id = collectionOptions.CollectionName,
                DefaultTimeToLive = collectionOptions.DefaultTimeToLiveSeconds
            };
            collection.PartitionKey.Paths.Add("/streamId");
            collection.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            collection.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/body/*" });
            collection.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/metadata/*" });

            var requestOptions = new RequestOptions
            {
                OfferThroughput = collectionOptions.CollectionRequestUnits
            };

            await client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, collection, requestOptions);
        }

        private async Task CreateStoredProcedureIfItDoesNotExist(string procedureName, string resourceName)
        {
            var query = client.CreateStoredProcedureQuery(commitsLink)
                .Where(x => x.Id == procedureName)
                .AsDocumentQuery();

            if (!(await query.ExecuteNextAsync<StoredProcedure>()).Any())
            {
                await client.CreateStoredProcedureAsync(commitsLink, new StoredProcedure
                {
                    Id = procedureName,
                    Body = Resources.GetString(resourceName)
                });
            }
        }
    }
}
