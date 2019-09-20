namespace CognitiveServicesDemo.CustomerSupport.Persistance.Domain
{
    public class AudioSnippetMetedata
    {
        public int Id { get; set; }
        public int AudioId { get; set; }
        public string Text { get; set; }
        public string ResultId { get; set; }
        public double Offset { get; set; }

        public string Sentiment { get; set; }
        public float PositiveSentiment { get; set; }
        public float NeutralSentiment { get; set; }
        public float NegativeSentiment { get; set; }

        public string SentimentJson { get; set; }
        public string KeyPhrasesJson { get; set; }
        public string NamedEntitiesJson { get; set; }
    }
}
