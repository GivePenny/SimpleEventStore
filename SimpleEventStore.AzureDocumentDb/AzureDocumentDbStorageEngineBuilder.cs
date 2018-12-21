using System;
using Microsoft.Azure.Documents.Client;

namespace SimpleEventStore.AzureDocumentDb
{
    public class AzureDocumentDbStorageEngineBuilder
    {
        private readonly DocumentClient client;
        private readonly CollectionOptions collectionOptions = new CollectionOptions();
        private readonly DatabaseOptions databaseOptions = new DatabaseOptions();
        private readonly LoggingOptions loggingOptions = new LoggingOptions();
        private ISerializationTypeMap typeMap = new DefaultSerializationTypeMap();

        public AzureDocumentDbStorageEngineBuilder(DocumentClient client)
        {
            Guard.IsNotNull(nameof(client), client);

            this.client = client;
        }

        public AzureDocumentDbStorageEngineBuilder(DocumentClient client, string databaseName)
        {
            Guard.IsNotNull(nameof(client), client);
            Guard.IsNotNullOrEmpty(nameof(databaseName), databaseName);

            this.client = client;
            databaseOptions.DatabaseName = databaseName;
        }

        public AzureDocumentDbStorageEngineBuilder UseCollection(Action<CollectionOptions> action)
        {
            Guard.IsNotNull(nameof(action), action);

            action(collectionOptions);
            return this;
        }

        public AzureDocumentDbStorageEngineBuilder UseDatabase(Action<DatabaseOptions> action)
        {
            Guard.IsNotNull(nameof(action), action);

            action(databaseOptions);
            return this;
        }

        public AzureDocumentDbStorageEngineBuilder UseLogging(Action<LoggingOptions> action)
        {
            Guard.IsNotNull(nameof(action), action);

            action(loggingOptions);
            return this;
        }

        public AzureDocumentDbStorageEngineBuilder UseTypeMap(ISerializationTypeMap typeMap)
        {
            Guard.IsNotNull(nameof(typeMap), typeMap);
            this.typeMap = typeMap;

            return this;
        }

        public IStorageEngine Build()
        {
            var engine = new AzureDocumentDbStorageEngine(client, databaseOptions, collectionOptions, loggingOptions, typeMap);
            return engine;
        }
    }
}
