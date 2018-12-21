using Microsoft.Azure.Documents;

namespace SimpleEventStore.AzureDocumentDb
{
    public class CollectionOptions
    {
        public CollectionOptions()
        {
            ConsistencyLevel = ConsistencyLevel.Session;
            CollectionRequestUnits = 400;
            CollectionName = "Commits";
        }

        public string CollectionName { get; set; }

        public ConsistencyLevel ConsistencyLevel { get; set; }

        public int? CollectionRequestUnits { get; set; }

        public int? DefaultTimeToLiveSeconds { get; set; }

        public int? DocumentTimeToLiveSeconds { get; set; }
    }
}