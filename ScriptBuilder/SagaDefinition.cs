using System.Collections.Generic;

namespace NServiceBus.SqlPersistence
{
    public class SagaDefinition
    {
        public string Name { get; set; }
        public List<string> MappedProperties = new List<string>();
        public List<string> UniqueProperties = new List<string>();
    }
}