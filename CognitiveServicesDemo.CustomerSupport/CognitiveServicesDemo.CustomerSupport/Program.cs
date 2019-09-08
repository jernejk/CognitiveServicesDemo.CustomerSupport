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
        private static string _textApiName = "<ENTER-TEXT-ANALYSIS-RESOURCE-NAME>";
        private static string _textApiToken = "<ENTER-TEXT-ANALYSIS-RESOURCE-TOKEN>";
        private static bool _usePreviewVersion = true;

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

                        Console.WriteLine();
                        Console.WriteLine("Text analysis...");

                        // Prepare document for different text analysis APIs.
                        // All of the requests will take in exactly the same request body.
                        var documentRequest = new TextApiRequest
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
                        AnalysedDocument sentimentResult = await AnalyzeDocument(documentRequest, "sentiment");
                        if (sentimentResult != null)
                        {
                            // We get back score representing sentiment.
                            if (_usePreviewVersion)
                            {
                                // We are getting a more accurate representation of how positive, negative and neutral the text is.
                                Console.WriteLine($"  Sentiment is {sentimentResult.sentiment} with scores:");
                                Console.WriteLine($"   - Positive:  {Math.Round(sentimentResult.documentScores.positive * 100, 2)}");
                                Console.WriteLine($"   - Neutral:   {Math.Round(sentimentResult.documentScores.neutral * 100, 2)}");
                                Console.WriteLine($"   - Negative:  {Math.Round(sentimentResult.documentScores.negative * 100, 2)}");
                            }
                            else
                            {
                                // We only get how potentially positive the text is in Sentiment analysis v2.
                                double score = sentimentResult.score;

                                // Try to determine if message is positive, negative or neutral.
                                string sentiment = score >= 0.75 ? "positive" : (score < 0.25 ? "negative" : "neutral");
                                Console.WriteLine($"  Sentiment is {sentiment} ({Math.Round(score * 100)}%)");
                            }

                        }
                        else
                        {
                            Console.WriteLine("  No sentiment found");
                        }

                        Console.WriteLine();

                        AnalysedDocument keyPhrasesResult = await AnalyzeDocument(documentRequest, "keyPhrases");
                        if (keyPhrasesResult?.keyPhrases?.Any() == true)
                        {
                            Console.WriteLine($"  Key phrases:");
                            foreach (var keyPhrase in keyPhrasesResult.keyPhrases)
                            {
                                Console.WriteLine($"   - {keyPhrase}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("  No key phrases found");
                        }

                        Console.WriteLine();

                        AnalysedDocument namedEntitiesResult = await AnalyzeDocument(documentRequest, "entities");
                        if (namedEntitiesResult?.entities?.Any() == true)
                        {
                            Console.WriteLine("  Entities:");
                            foreach (var entity in namedEntitiesResult.entities)
                            {
                                Console.WriteLine($"   - {entity.name} ({entity.type})");
                                if (!string.IsNullOrWhiteSpace(entity.wikipediaUrl))
                                {
                                    Console.ForegroundColor = ConsoleColor.Blue;
                                    Console.WriteLine($"       {entity.wikipediaUrl}");
                                    Console.ForegroundColor = ConsoleColor.Gray;
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

        private static async Task<AnalysedDocument> AnalyzeDocument(TextApiRequest sentimentDocument, string textAnalysisType)
        {
            // A preview version 3 only exists for sentiment analysis.
            string version = _usePreviewVersion && textAnalysisType == "sentiment" ? "3.0-preview" : "2.1";

            TextApiResponse textApiResponse;
            using (var client = new HttpClient())
            {
                string apiSubdomain = _textApiName;
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _textApiToken);

                string json = JsonConvert.SerializeObject(sentimentDocument);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"https://{apiSubdomain}.cognitiveservices.azure.com/text/analytics/v{version}/{textAnalysisType}", content);

                string responseJson = await response.Content.ReadAsStringAsync();
                textApiResponse = JsonConvert.DeserializeObject<TextApiResponse>(responseJson);
            }

            return textApiResponse?.documents?.FirstOrDefault();
        }
    }
}
