using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace UmamusumeOCR
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string TitleText = "Umamusume OCR";
        private readonly Processor p;
        private Config gameConfig;
        private string lastUpdatedText = null;

        private IntPtr gameWindowHandler = IntPtr.MaxValue;
        private ITranslator translator;

        public MainWindow()
        {
            InitializeComponent();
            Title = TitleText;
            ReloadConfig(null, null);
            p = new(OcrFactory.BuildOcr(gameConfig.Ocr, gameConfig.OcrConfig));
            translator = TranslatorFactory.BuildTranslator(gameConfig.Translator, gameConfig.Language, gameConfig.TranslatorConfig);
            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_TickAsync);
            dispatcherTimer.Interval = new TimeSpan(300);
            dispatcherTimer.Start();

            StoryDialogueBtn.Click += CaptureAreaEventAsync;
            CenterDialogueBtn.Click += CaptureAreaEventAsync;
            ChoiceBtn.Click += CaptureAreaEventAsync;
            FullScreenBtn.Click += CaptureAreaEventAsync;

            ReloadConfigBtn.Click += ReloadConfig;
            ResetGameWindowHandlerBtn.Click += ResetGameWindowHandler;
            SaveGameWindowBtn.Click += OverwriteGameWindowArea;
            ResetGameWindowBtn.Click += ResetGameWindow;
        }

        private void ReloadConfig(object sender, RoutedEventArgs e)
        {
            gameConfig = Config.LoadGameConfig();
            StatusBlock.Text = "Config loaded";
        }

        private void ResetGameWindowHandler(object sender, EventArgs e)
        {
            gameWindowHandler = IntPtr.MaxValue;
        }

        private void OverwriteGameWindowArea(object sender, RoutedEventArgs e)
        {
            if (gameWindowHandler != IntPtr.MaxValue)
            {
                var windowRect = WindowsUtilities.GetWindowArea(gameWindowHandler);
                gameConfig.WindowArea = windowRect;

                if (gameConfig.GameArea.Width > windowRect.Width || gameConfig.GameArea.Height > windowRect.Height)
                {
                    var ratio = (double)Processor.ProcessingSize.Width / Processor.ProcessingSize.Height;
                    var gameAreaY = windowRect.Y + 57;
                    var gameAreaHeight = windowRect.Height - 60;
                    var gameAreaWidth = (int)(gameAreaHeight * ratio);
                    var gameAreaX = windowRect.X + (windowRect.Width - gameAreaWidth) / 2;
                    gameConfig.GameArea = new(gameAreaX, gameAreaY, gameAreaWidth, gameAreaHeight);
                }
                    
                gameConfig.SaveConfig();
                StatusBlock.Text = "Game window info saved";
            }
        }

        private void ResetGameWindow(object sender, RoutedEventArgs e)
        {
            if (gameWindowHandler != IntPtr.MaxValue)
            {
                WindowsUtilities.SetWindowPos(gameWindowHandler,
                                    gameConfig.WindowArea.X, gameConfig.WindowArea.Y,
                                    gameConfig.WindowArea.Width, gameConfig.WindowArea.Height);
                StatusBlock.Text = "Game window info saved";
            }
        }

        private async Task<bool> TranslateAndUpdateTextBlocksAsync(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;
            if (s == lastUpdatedText)
            {
                return true;
            }
            lastUpdatedText = s;

            UpdateTextBoxes(s, TextBox1, TextBox2);
            var translated = await translator.TranslateTextAsync(s);
            UpdateTextBoxes(translated, TextBox3, TextBox4);

            return true;
        }

        private async Task<bool> TranslateAndUpdateTextBlocksAsync(DialogueResult? dialogueResult)
        {
            if (dialogueResult is DialogueResult result)
            {
                var s = result.Dialogue;
                if (result.Speaker != null)
                {
                    s = result.Speaker + "\r\n" + s;
                }

                return await TranslateAndUpdateTextBlocksAsync(s);
            }
            return false;
        }

        private static void UpdateTextBoxes(string s, TextBox b1, TextBox b2)
        {
            if (string.IsNullOrEmpty(b1.Text))
            {
                b1.Text = s;
            }
            else
            {
                if (!string.IsNullOrEmpty(b2.Text))
                    b1.Text = b2.Text;
                b2.Text = s;
            }
        }

        private Bitmap CurrentGameBitmap()
        {
            var windowArea = WindowsUtilities.GetWindowArea(gameWindowHandler);
            var absoluteGameArea = new Rectangle()
            {
                X = windowArea.X + gameConfig.GameArea.X,
                Y = windowArea.Y + gameConfig.GameArea.Y,
                Width = gameConfig.GameArea.Width,
                Height = gameConfig.GameArea.Height
            };
            return BitmapUtilities.CaptureGame(absoluteGameArea, Processor.ProcessingSize);
        }


        private async void CaptureAreaEventAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                using Bitmap gameBm = CurrentGameBitmap();
                if (sender == StoryDialogueBtn)
                {
                    _ = TranslateAndUpdateTextBlocksAsync(await p.OCRStoryDialogueAsync(gameBm, true));
                }
                else if (sender == CenterDialogueBtn)
                {
                    _ = TranslateAndUpdateTextBlocksAsync(await p.OCRGameAreaAsync(gameBm, Processor.CenterDialogueArea, ProcessType.Center, true));
                }
                else if (sender == ChoiceBtn)
                {
                    _ = TranslateAndUpdateTextBlocksAsync(await p.OCRChoicesAsync(gameBm, true));
                }
                else if (sender == FullScreenBtn)
                {
                    _ = TranslateAndUpdateTextBlocksAsync(await p.OCRGameAreaAsync(gameBm, Processor.FullGameArea, ProcessType.Fullscreen, true));
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
            }
        }

        private async void DispatcherTimer_TickAsync(object sender, EventArgs e)
        {
            try
            {
                IntPtr handler = WindowsUtilities.GetActiveWindowHandler();

                if (gameWindowHandler == IntPtr.MaxValue)
                {
                    var targetTitle = WindowsUtilities.GetWindowTitle(handler)?.Trim();
                    if (targetTitle?.Contains(gameConfig.WindowTitle) == true && targetTitle != TitleText)
                    {
                        gameWindowHandler = handler;
                    }
                    StatusBlock.Text = "Game window detected";
                }

                if (handler == gameWindowHandler && WindowState == WindowState.Normal)
                {
                    using Bitmap gameBm = CurrentGameBitmap();

                    if (await TranslateAndUpdateTextBlocksAsync(await p.OCRChoicesAsync(gameBm)))
                    {
                        StatusBlock.Text = "Choices";
                        return;
                    }
                    else if (await TranslateAndUpdateTextBlocksAsync(await p.OCRStoryDialogueAsync(gameBm)))
                    {
                        StatusBlock.Text = "Story";
                        return;
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
            }
        }
    }
}
