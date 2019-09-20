using CognitiveServicesDemo.CustomerSupport.Persistance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CognitiveServicesDemo.CustomerSupport.Persistance
{
    public class AudioMetadataContext : DbContext
    {
        public AudioMetadataContext(DbContextOptions<AudioMetadataContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AudioDocument> AudioDocuments { get; set; }
        
        public virtual DbSet<AudioSnippetMetedata> AudioSnippetMetedatas { get; set; }
    }
}
