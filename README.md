# UmamusumeOCR

A tool that automatically or manually OCRs and translates texts from Umamusume running on Windows DMM player or Android Emulator. It does not modify any game file and should be totally safe.

一个用来OCR并翻译windows端赛马娘（dmm player或者安卓模拟器皆可）的工具

## Download 下载
Release on the right side of this page -> v1.X -> publish.zip

页面右边 Releases -> v1.x -> publish.zip

## Requirement:
+ .Net 5.0 runtime X86: https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-5.0.4-windows-x86-installer
+ not a very old version Windows and Japanese Language Package for local Japanese OCR
+ Umamusume that runs on Windows, not your phone (obviously)

## How to use it:
Run UmamusumeOCR.exe to generate the config.json file if you are runing this first time

Change the value of WindowTitle in the config file to match the window title of your Android Emulator (just a part of the title is fine, case sensitive) if you are not running DMM Player version, e.g.:
"WindowTitle": "Bluestacks",

Click Reload Config Button, then start Umamusume from DMM player or your Emulator.

If UmamusumeOCR is not minimized and the game window is active, the status bar under will become "Game window detected" (Not working in v1.0)

Click Save Game Window Info Button to capture the game window area, then click Full-Screen Button. If everything is fine, some texts and translations should come out. A FullScreen.bmp image file should also be generated under the UmamusumeOCR.exe folder. Check that image and modify "GameArea" in Config to make Full Screen capture matches the exact game area (no title bar, etc.). "GameArea" is in the following format: X, Y, Width, Height, which X and Y are relative to the Top Left of your Game Window. Reload Config again.

Go to any Dialogue in the game, and the text and translation should automatically come out.

If you closed the game window, click Recapture Game Window. If you resized the game window and do not want to reconfigure game area again, click Reset Game Window (may need running UmamusumeOCR as Administrator)

Enjoy!

## OCR settings:
### WinRT OCR
The default free OCR engine comes with Windows RT APIs that has acceptable accuracy. Requires a not a very old Windows and Japanese Language Package installed.

In config file: `"Ocr": "winrtOCR"`

### Azure
Dedicated OCR services provided by Azure Computer Vision. It does not require anything on Windows but $$ (or maybe a free trial account?) and some setup. It has a fast average response time and slightly better accuracy but sometimes freezes the program if your network is not stable. Go to https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/ for more details.

In config file: 

```
  "Ocr": "azure",
  "OcrConfig": "AzureConfigs/AzureOCR.json" (or the path to the file you store the keys and endpoint)
```

In AzureConfigs/AzureOCR.json:

```
{
  "Key": the key you find from azure computer vision service key page,  
  "Endpoint": endpoint you find from azure computer vision service key page  
}
```

### Google
Dedicated OCR services provided by Google Computer Vision. It does not require anything on Windows, but $$ (NO FREE TRIAL ACCOUNT) and some setup. The accuracy is amazing, but I don't want to spend more $$ on this, so this is not available for now.

## Translator settings:
### Google
The default translator uses a public translate.google.com API. Nothing to worried about if google services are available to you.

### Azure
Dedicated translation servies provided by Azure. Support custom glossaries built-in the Glossaries folder.

In config file: 

```
  "Translator": "azure",
  "TranslatorConfig": "AzureConfigs/AzureTranslator.json" (or the path to the file you store the keys and endpoint)
```

In AzureConfigs/AzureTranslator.json:

```
{
  "Key": the key you find from azure translation service key page,  
  "Endpoint": endpoint you find from azure translation service key page,
   "Location": location you find from azure translation service key page
}
```


 






