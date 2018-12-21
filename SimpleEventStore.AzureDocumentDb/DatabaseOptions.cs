namespace SimpleEventStore.AzureDocumentDb
{
    public class DatabaseOptions
    {
        public string DatabaseName { get; set; }

        public int? DatabaseRequestUnits { get; set; }
    }
}
