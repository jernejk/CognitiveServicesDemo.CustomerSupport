using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CognitiveServicesDemo.CustomerSupport
{
    public static class Program
    {
        private static readonly bool _usePreviewVersion = true;
        private static readonly ConsoleColor _defaultColor = Console.ForegroundColor;
        private static readonly Dictionary<string, ConsoleColor> _sentimentToColor = new Dictionary<string, ConsoleColor>
        {
            { "positive", ConsoleColor.Green },
            { "neutral", ConsoleColor.Yellow },
            { "negative", ConsoleColor.Red },
            { "mixed", ConsoleColor.Gray },
        };

        private static async Task Main(string[] args)
        {
            if (!AreCognitiveServicesVariablesValid())
            {
                WriteLineInColor("Please configure Cognitive Services values in Constants.cs!", ConsoleColor.Red);
                Console.WriteLine("Press enter to exit...");
                Console.ReadLine();
                return;
            }

            string audioFile = args.FirstOrDefault();
            bool useMicrophone = string.IsNullOrWhiteSpace(audioFile);
            if (!useMicrophone)
            {
                bool isFileValid = true;
                Console.WriteLine($"Input file: {audioFile}");
                if (!File.Exists(audioFile))
                {
                    WriteLineInColor($"File was not found!", ConsoleColor.Red);
                    Console.WriteLine("Press enter to exit...");
                    Console.ReadLine();

                    isFileValid = false;
                }
                else if (!audioFile.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    WriteLineInColor($"Only .wav files are supported!", ConsoleColor.Red);

                    Console.WriteLine("Press enter to exit...");
                    Console.ReadLine();

                    isFileValid = false;
                }

                if (!isFileValid)
                {
                    // End program.
                    return;
                }
            }

            // Configure speech API and audio input (microphone or file).
            SpeechConfig speechConfig = SpeechConfig.FromSubscription(Constants.SpeechApiToken, Constants.SpeechApiRegion);
            AudioConfig audioInput = useMicrophone ?
                AudioConfig.FromDefaultMicrophoneInput() :
                AudioConfig.FromWavFileInput(args[0]);

            var stopProcessingFile = new TaskCompletionSource<int>();

            // Initialize Cognitive Services speech recognition service.
            // On demand, it will use the hardware microphone, send it to MS Cognitive Services
            // and give us the appropriate response.
            using (audioInput)
            using (var recognizer = new SpeechRecognizer(speechConfig, audioInput))
            {
                // Attempt to recognize speech once.
                // It will start capturing when it hears something and stop on first pause.
                recognizer.Recognized += async (_, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech &&!string.IsNullOrWhiteSpace(e.Result.Text))
                    {
                        await AnalyzeText(e.Result);

                        Console.WriteLine();

                        if (useMicrophone)
                        {
                            Console.WriteLine("Say something or press q to quit...");
                        }

                        WriteInColor("> ", ConsoleColor.DarkGray);
                    }
                };

                recognizer.Canceled += (s, e) =>
                {
                    stopProcessingFile.TrySetResult(0);
                };

                await recognizer.StartContinuousRecognitionAsync();

                // Have human interaction only for microphone scenario.
                if (useMicrophone)
                {
                    // Keep listening until user presses "q" unless we are processing a file.
                    Console.WriteLine();
                    Console.WriteLine("Say something or press q to quit...");
                    WriteInColor("> ", ConsoleColor.DarkGray);

                    var key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Q)
                    {
                        await recognizer.StopContinuousRecognitionAsync();
                    }
                }
                else
                {
                    Console.WriteLine($"Processing audio file \"{args[0]}\"...");
                    Console.WriteLine();

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopProcessingFile.Task });
                }

                Console.WriteLine();
                Console.WriteLine();

                await recognizer.StopContinuousRecognitionAsync();
            }
        }

        private static async Task AnalyzeText(SpeechRecognitionResult speechToTextResult)
        {
            WriteLineInColor(speechToTextResult.Text, ConsoleColor.Cyan);

            Console.WriteLine();
            WriteLineInColor("Text analysis...", ConsoleColor.DarkGray);

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
                    Console.Write("  Sentiment is ");
                    WriteInColor(sentimentResult.sentiment, _sentimentToColor[sentimentResult.sentiment]);
                    Console.WriteLine($" with scores:");

                    WriteValuesInColor("   - Positive:  ", $"{Math.Round(sentimentResult.documentScores.positive * 100, 2)}%", _sentimentToColor["positive"]);
                    WriteValuesInColor("   - Neutral:   ", $"{Math.Round(sentimentResult.documentScores.neutral * 100, 2)}%", _sentimentToColor["neutral"]);
                    WriteValuesInColor("   - Negative:  ", $"{Math.Round(sentimentResult.documentScores.negative * 100, 2)}%", _sentimentToColor["negative"]);
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
                WriteLineInColor("  No sentiment found", ConsoleColor.DarkYellow);
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
                WriteLineInColor("  No key phrases found", ConsoleColor.DarkYellow);
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
                        WriteLineInColor($"       {entity.wikipediaUrl}", ConsoleColor.Blue);
                    }
                }
            }
            else
            {
                WriteLineInColor("  No entities found", ConsoleColor.DarkYellow);
            }
        }

        private static async Task<AnalysedDocument> AnalyzeDocument(TextApiRequest sentimentDocument, string textAnalysisType)
        {
            // A preview version 3 only exists for sentiment analysis.
            string version = _usePreviewVersion && textAnalysisType == "sentiment" ? "3.0-preview" : "2.1";

            TextApiResponse textApiResponse;
            using (var client = new HttpClient())
            {
                string apiSubdomain = Constants.TextApiName;
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Constants.TextApiToken);

                string json = JsonConvert.SerializeObject(sentimentDocument);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"https://{apiSubdomain}.cognitiveservices.azure.com/text/analytics/v{version}/{textAnalysisType}", content);

                string responseJson = await response.Content.ReadAsStringAsync();
                textApiResponse = JsonConvert.DeserializeObject<TextApiResponse>(responseJson);
            }

            return textApiResponse?.documents?.FirstOrDefault();
        }

        private static void WriteInColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = _defaultColor;
        }

        private static void WriteLineInColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = _defaultColor;
        }

        private static void WriteValuesInColor(string title, string value, ConsoleColor color)
        {
            Console.Write(title);

            Console.ForegroundColor = color;
            Console.WriteLine(value);
            Console.ForegroundColor = _defaultColor;
        }

        private static bool AreCognitiveServicesVariablesValid()
        {
            return !Constants.SpeechApiRegion.StartsWith("<") && !Constants.SpeechApiToken.StartsWith("<")
                && !Constants.TextApiName.StartsWith("<") && !Constants.TextApiToken.StartsWith("<");
        }
    }
}
