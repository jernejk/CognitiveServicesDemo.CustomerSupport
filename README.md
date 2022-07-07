# Microsoft Cognitive Services Demo for Customer Support

This is created to show a proof of concept for customer support using Microsoft Cognitive Service.

## Scenario

Imagine we have thousands of hours of recordings of customer service and what we want to achieve is the following:

* Convert speech to text
* Get text sentiment
* Get key phrases
* Ideally add additional context like persons, corporations, etc.

## What the demo covers

We use Microsoft Cognitive Services to convert speech to text and do some text analysis to get some extra context out of text.

You can either use your microphone or put a `.wav` as parameter to the app for speech to text + analysis.
When using a microphone, the app will listen for your input and will start analyzing as soon as it detect a bit longer pause.

The text is then processed by Text Analysis v3.0 (also supports v2.1).

![Demonstration of the app when using a microphone](img/cognitive-services-demo-customer-support.png)
**Figure: Demonstration of the app.**

## Prerequisites

You'll need .NET Core 3.1 SDK to run the code. For testing you need either a microphone or a `.wav` audio file.

**Create following Cognitive Services in Azure Portal:**

* Cognitive Services Speech
* Cognitive Services Text Analysis

<img src="img/create-azure-speech-logo.png" width="134" alt="Create Microsoft Cognitive Services Speech" />
<img src="img/create-azure-text-analysis-logo.png" width="180" alt="Create Microsoft Cognitive Services Text Analysis" />

Copy the values from Speech and Text Analysis into the variables in `Programs.cs`.

![](img/copy-details-from-azure.png)
**Figure: Copy value from Azure Portal.**

## Run application (command line)

Run the application in `CognitiveServicesDemo.CustomerSupport` folder for microphone demo.

``` bash
dotnet run
```

To use a `.wav` as an input, add the `.wav` file as a parameter.

``` bash
dotnet run "[path-to-wav.wav]"
```

**Visual Studio:**

Open `CognitiveServicesDemo.CustomerSupport.sln` and press F5.

If you want to play a `.wav` file:

1. Go to `CognitiveServicesDemo.CustomerSupport` project
2. Find `launchSettings.json` under `Properties`
3. Add path to `commandLineArgs` variable and escape `"` and `\`
4. Press F5

Example of `launchSettings.json`:

``` js
{
  "profiles": {
    "CognitiveServicesDemo.CustomerSupport": {
      "commandName": "Project",
      "commandLineArgs": "\"C:\\DataJK\\Audio\\test.wav\""
    }
  }
}
```
