using System;

namespace CognitiveServicesDemo.CustomerSupport.Persistance.Domain
{
    public class AudioDocument
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime CreatedOn { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
