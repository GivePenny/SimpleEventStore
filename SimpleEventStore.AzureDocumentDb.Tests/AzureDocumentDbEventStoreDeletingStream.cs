using System.Threading.Tasks;
using NUnit.Framework;
using SimpleEventStore.Tests;

namespace SimpleEventStore.AzureDocumentDb.Tests
{
    [TestFixture]
    public class AzureDocumentDbEventStoreDeletingStream : EventStoreDeletingStream
    {
        protected override Task<IStorageEngine> CreateStorageEngine()
        {
            return StorageEngineFactory.Create("DeletingStreamTests");
        }
    }
}
