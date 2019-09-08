using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CognitiveServicesDemo.CustomerSupport
{
    public static class Program
    {
        private static string _speechApiRegion = "<ENTER-SPEECH-RECOGNITION-REGION>";
        private static string _speechApiToken = "<ENTER-SPEECH-RECOGNITION-TOKEN>";
        private static string textApiName = "<ENTER-TEXT-ANALYSIS-RESOURCE-NAME>";
        private static string textApiToken = "<ENTER-TEXT-ANALYSIS-RESOURCE-TOKEN>";
        private static bool usePreviewVersion = true;

        private static async Task Main(string[] args)
        {
            SpeechConfig speechConfig = SpeechConfig.FromSubscription(_speechApiToken, _speechApiRegion);

            // Initialize Cognitive Services speech recognition service.
            // On demand, it will use the hardware microphone, send it to MS Cognitive Services
            // and give us the appropriate response.
            using (var recognizer = new SpeechRecognizer(speechConfig))
            {
                // Keep listening until user presses "q".
                while (true)
                {
                    Console.WriteLine("Say something...");
                    Console.WriteLine();

                    // Attempt to recognize speech once.
                    // It will start capturing when it hears something and stop on first pause.
                    SpeechRecognitionResult speechToTextResult = await recognizer.RecognizeOnceAsync();
                    if (speechToTextResult.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"{speechToTextResult.Text}");

                        // Prepare document for different text analysis APIs.
                        // All of the requests will take in exactly the same request body.
                        var textDocument = new TextApiRequest
                        {
                            documents = new Document[]
                            {
                                new Document
                                {
                                    language = "en",
                                    id = "1",
                                    text = speechToTextResult.Text
                                }
                            }
                        };

                        // Get sentiment analysis via Cognitive Services Text Analysis APIs.
                        TextApiRequest sentimentResult = await GetTextAnalysis(textDocument, "sentiment");
                        if (sentimentResult?.documents?.Any() == true)
                        {
                            // We get back score representing sentiment.
                            double score;
                            string sentiment;
                            if (usePreviewVersion)
                            {
                                // We are getting a more accurate representation of how positive, negative and neutral the text is.
                                score = sentimentResult.documents[0].documentScores.positive;
                                sentiment = sentimentResult.documents[0].sentiment;
                            }
                            else
                            {
                                // We only get how potentially positive the text is.
                                score = sentimentResult.documents[0].score;

                                // Try to determine if message is positive, negative or neutral.
                                sentiment = score >= 0.75 ? "positive" : (score < 0.25 ? "negative" : "neutral");
                            }

                            Console.WriteLine($"  The statement is {sentiment} ({Math.Round(score * 100)}%)");
                        }
                        else
                        {
                            Console.WriteLine("  No sentiment found");
                        }

                        TextApiRequest keyPhrasesResult = await GetTextAnalysis(textDocument, "keyPhrases");
                        if (keyPhrasesResult?.documents?.Any(d => d.keyPhrases?.Any() == true) == true)
                        {
                            Console.WriteLine($"  Key phrases: {string.Join(", ", keyPhrasesResult.documents[0].keyPhrases)}");
                        }
                        else
                        {
                            Console.WriteLine("  No key phrases found");
                        }

                        TextApiRequest namedEntitiesResult = await GetTextAnalysis(textDocument, "entities");
                        if (namedEntitiesResult?.documents?.Any(d => d.entities?.Any() == true) == true)
                        {
                            Console.WriteLine("  Entities:");
                            foreach (var entity in namedEntitiesResult.documents.SelectMany(d => d.entities))
                            {
                                Console.WriteLine($"   - {entity.name} ({entity.type})");
                                if (!string.IsNullOrWhiteSpace(entity.wikipediaUrl))
                                {
                                    Console.WriteLine($"     {entity.wikipediaUrl}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("  No entities found");
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine("Press q to quit or enter to continue...");

                    var key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Q)
                    {
                        break;
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
        }

        private static async Task<TextApiRequest> GetTextAnalysis(TextApiRequest sentimentDocument, string textAnalysisType)
        {
            // A preview version 3 only exists for sentiment analysis.
            string version = usePreviewVersion && textAnalysisType == "sentiment" ? "3.0-preview" : "2.1";

            TextApiRequest textAnalysis;
            using (var client = new HttpClient())
            {
                string apiSubdomain = textApiName;
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", textApiToken);

                string json = JsonConvert.SerializeObject(sentimentDocument);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"https://{apiSubdomain}.cognitiveservices.azure.com/text/analytics/v{version}/{textAnalysisType}", content);

                string responseJson = await response.Content.ReadAsStringAsync();
                textAnalysis = JsonConvert.DeserializeObject<TextApiRequest>(responseJson);
            }

            return textAnalysis;
        }
    }
}
