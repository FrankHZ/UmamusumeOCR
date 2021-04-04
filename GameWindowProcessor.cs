using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace UmamusumeOCR
{
    public enum ProcessType
    {
        StoryDialogue,
        Choices,
        Center,
        Fullscreen,
        Speaker
    }

    struct DialogueAreas
    {
        public Rectangle DialogueArea;
        public Rectangle? SpeakerArea;
    }

    public struct DialogueResult
    {
        public string Speaker;
        public string Dialogue;
    }

    public class Processor
    {
        public static readonly Size ProcessingSize = new()
        {
            Width = 1165,
            Height = 2072
        };

        public static readonly Rectangle FullGameArea = new(0, 0, ProcessingSize.Width, ProcessingSize.Height);
        public static readonly Rectangle CenterDialogueArea = new(10, 615, 1050, 800);

        private readonly Dictionary<ProcessType, Bitmap> LastThumbNail = new(Enum.GetNames(typeof(ProcessType)).Length);
        private readonly Dictionary<ProcessType, Rectangle> LastArea = new(Enum.GetNames(typeof(ProcessType)).Length);
        private ProcessType LastEventType = ProcessType.Fullscreen;
        private readonly StoryDialogueProcessor _dialogueProcessor = new();
        private readonly ChoiceProcessor _choiceProcessor = new();

        private readonly IOcr _ocrEngine;

        public Processor(IOcr ocrEngine)
        {
            foreach (ProcessType t in Enum.GetValues(typeof(ProcessType)))
            {
                LastThumbNail[t] = null;
                LastArea[t] = new Rectangle();
            }
            _ocrEngine = ocrEngine;
        }

        public async Task<string> OCRGameAreaAsync(Bitmap game, Rectangle area, ProcessType type, bool save, bool reverseColor = false)
        {
            using var bitmap = BitmapUtilities.CaptureAreaFromBitmap(game, area);
            if (reverseColor)
            {
                bitmap.ReverseWhiteText();
            }
            return await OCRBitmapAsync(bitmap, area, type, save);
        }

        private async Task<string> OCRBitmapAsync(Bitmap bitmap, Rectangle area, ProcessType type, bool save)
        {
            Bitmap thumbNail = null;
            if (type != ProcessType.Choices)
            {
                thumbNail = new Bitmap(bitmap, area.Width / 12, area.Height / 12);
            }
            else
            {
                thumbNail = new Bitmap(bitmap, area.Width / 2, area.Height / 2);
            }

            if (!save &&
                (LastEventType == type ||
                    (LastEventType == ProcessType.Choices || LastEventType == ProcessType.Speaker) && type == ProcessType.StoryDialogue))
            {

                if (LastArea[type] == area)
                {
                    if (BitmapUtilities.IsSimilar(thumbNail, LastThumbNail[type]))
                    {
                        thumbNail.Dispose();
                        return null;
                    }
                }
            }

            if (LastThumbNail[type] is not null)
            {
                LastThumbNail[type].Dispose();
            }
            LastThumbNail[type] = thumbNail;
            LastArea[type] = area;

            if (save)
            {
                bitmap.Save($"{type}.png", ImageFormat.Png);
            }
            LastEventType = type;

            var combineLines = type == ProcessType.StoryDialogue;
            return await _ocrEngine.ExtractTextAsync(bitmap, combineLines);
        }

        public async Task<DialogueResult?> OCRStoryDialogueAsync(Bitmap game, bool save = false)
        {
            if (_dialogueProcessor.CheckStoryDialogue(game) is DialogueAreas areas)
            {
                var dialogue = await OCRGameAreaAsync(game, areas.DialogueArea, ProcessType.StoryDialogue, save);
                if (dialogue is null)
                {
                    return null;
                }

                var result = new DialogueResult
                {
                    Dialogue = dialogue
                };

                if (areas.SpeakerArea is Rectangle speakerArea)
                {
                    result.Speaker = (await OCRGameAreaAsync(game, speakerArea, ProcessType.Speaker, save, true));
                    if (result.Speaker is string s)
                    {
                        result.Speaker = s.Trim();
                    }
                }
                return result;
            }
            return null;
        }

        public async Task<string> OCRChoicesAsync(Bitmap game, bool save = false)
        {
            using var bm = _choiceProcessor.CombineChoices(game);
            return bm is null ? null : await OCRBitmapAsync(bm, _choiceProcessor.BuildArea(bm), ProcessType.Choices, save);
        }


        private class StoryDialogueProcessor
        {
            static readonly int StoryDialogueX = 85;
            static readonly int StoryDialogueWidth = 965;
            static readonly int StoryDialogueHeight = 235;

            static readonly Rectangle StoryDialogueCheckingArea1 = new(ProcessingSize.Width / 3 * 2, ProcessingSize.Height - 500, 1, 300);

            public DialogueAreas? CheckStoryDialogue(Bitmap game)
            {
                var area = StoryDialogueCheckingArea1;
                using var bitmap = BitmapUtilities.CaptureAreaFromBitmap(game, area);
                var btnColor = bitmap.GetPixel(0, bitmap.Height - 1);
                var btnH = 0;
                var whiteCount = 0;

                for (var h = 0; h < area.Height; h++)
                {
                    var c = bitmap.GetPixel(0, h);
                    if (BitmapUtilities.IsWhite(c))
                    {
                        whiteCount++;
                    }
                    else
                    {
                        if (whiteCount > 65)
                        {
                            btnH = h + 2;
                            if (btnH >= bitmap.Height)
                            {
                                return null;
                            }
                            if (CheckBotAndTop(game, area, btnH))
                            {
                                break;
                            }
                            else
                            {
                                btnH = 0;
                            }
                        }
                        whiteCount = 0;
                    }
                }
                if (btnH == 0)
                {
                    return null;
                }

                if (btnH > 263 && btnH < 268)
                {
                    btnH = 266;
                }

                var iconArea = new Rectangle(ProcessingSize.Width - 155, area.Y + btnH - 15, 1, 15);
                using var iconBitmap = BitmapUtilities.CaptureAreaFromBitmap(game, iconArea);

                if (Enumerable.Range(0, iconArea.Height).Select(i => iconBitmap.GetPixel(0, i)).Any(c => BitmapUtilities.IsWhite(c)))
                {
                    return null;
                }

                var result = new DialogueAreas
                {
                    DialogueArea = new Rectangle(StoryDialogueX, area.Y + btnH - 332 + 55, StoryDialogueWidth, StoryDialogueHeight)
                };

                var speakerCheckArea = new Rectangle(140, area.Y + btnH - 332 + 40, 15, 1);
                using var speakerCheckAreaBitmap = BitmapUtilities.CaptureAreaFromBitmap(game, speakerCheckArea);
                if (Enumerable.Range(0, speakerCheckAreaBitmap.Width).Select(i => speakerCheckAreaBitmap.GetPixel(i, 0)).All(c => !BitmapUtilities.IsWhite(c)))
                {
                    result.SpeakerArea = new Rectangle(100, area.Y + btnH - 333 - 20, 450, 60);
                }

                return result;
            }


            private bool CheckBotAndTop(Bitmap game, Rectangle area, int btnH)
            {
                var btnArea = new Rectangle(ProcessingSize.Width / 2 - 200, area.Y + btnH, 400, 1);
                using var btnBitmap = BitmapUtilities.CaptureAreaFromBitmap(game, btnArea);
                var btnColor = btnBitmap.GetPixel(0, 0);

                if (!Enumerable.Range(0, btnArea.Width).Select(i => btnBitmap.GetPixel(i, 0)).All(c => BitmapUtilities.IsSimilar(c, btnColor)))
                {
                    return false;
                }

                var top = area.Y + btnH - 332;
                var topArea1 = new Rectangle(area.X, top, 1, 6);
                using var topBitmap = BitmapUtilities.CaptureAreaFromBitmap(game, topArea1);
                var topColor = topBitmap.GetPixel(0, 0);
                var top2Color = topBitmap.GetPixel(0, 1);
                var topWhite = topBitmap.GetPixel(0, 5);


                if (!(BitmapUtilities.IsSimilar(topColor, btnColor) || BitmapUtilities.IsSimilar(top2Color, btnColor)) ||
                    !BitmapUtilities.IsWhite(topWhite))
                {
                    return false;
                }

                //var topArea2 = new Rectangle(GameConfig.DefaultWindowWidth / 2 - 50, top, 100, 2);
                //using var topBitmap2 = BitmapUtilities.CaptureAreaFromBitmap(game, topArea2);
                //if (!Enumerable.Range(0, topArea2.Width)
                //    .All(i =>
                //        BitmapUtilities.IsSimilar(topBitmap2.GetPixel(i, 0), btnColor) ||
                //        BitmapUtilities.IsSimilar(topBitmap2.GetPixel(i, 1), btnColor)))
                //{
                //    return false;
                //}

                return true;
            }
        }

        private class ChoiceProcessor
        {
            static readonly Rectangle ChoiceDarkArea = new(1100, 1900, 5, 1);
            static readonly Rectangle ChoiceDarkArea2 = new(100, 1900, 5, 1);
            static readonly Rectangle ChoiceDarkArea3 = new(600, 1700, 5, 1);
            static readonly Rectangle Choice1LeftArea = new(35, 1300, 12, 1);

            static readonly Rectangle C1Area = new(115, 1300, 960, 80);
            static readonly Rectangle C1IconCheckingArea = new(62, 1342, 40, 1);
            static readonly Rectangle C1CharacterCheckingArea = new(115 + 50 - 5, 1300 + 24, 10, 30);
            static readonly Color CharacterColor = Color.FromArgb(121, 64, 22);

            readonly Rectangle[] ChoicesAreas = new Rectangle[5];
            readonly Rectangle[] IconCheckingAreas = new Rectangle[5];
            readonly Rectangle[] CharacterCheckingAreas = new Rectangle[5];

            public ChoiceProcessor()
            {
                ChoicesAreas[^1] = C1Area;
                IconCheckingAreas[^1] = C1IconCheckingArea;
                CharacterCheckingAreas[^1] = C1CharacterCheckingArea;
                for (var i = ChoicesAreas.Length - 2; i >= 0; i--)
                {
                    var yShift = -180 * (ChoicesAreas.Length - i - 1);
                    ChoicesAreas[i] = new(C1Area.X, C1Area.Y + yShift, C1Area.Width, C1Area.Height);
                    IconCheckingAreas[i] = new(C1IconCheckingArea.X, C1IconCheckingArea.Y + yShift, C1IconCheckingArea.Width, C1IconCheckingArea.Height);
                    CharacterCheckingAreas[i] =
                        new(C1CharacterCheckingArea.X, C1CharacterCheckingArea.Y + yShift, C1CharacterCheckingArea.Width, C1CharacterCheckingArea.Height);
                }
            }

            private bool CheckChoiceDark(Bitmap game)
            {
                var darkArea = ChoiceDarkArea;
                using var darkAeraBm = BitmapUtilities.CaptureAreaFromBitmap(game, darkArea);

                for (int j = 0; j < darkAeraBm.Height; j++)
                {
                    for (int i = 0; i < darkAeraBm.Width; i++)
                    {
                        var c = darkAeraBm.GetPixel(i, j);
                        if (c.R + c.G + c.B > 127 * 3)
                        {
                            return false;
                        }
                    }
                }

                darkArea = ChoiceDarkArea2;
                using var darkAeraBm2 = BitmapUtilities.CaptureAreaFromBitmap(game, darkArea);
                for (int j = 0; j < darkAeraBm2.Height; j++)
                {
                    for (int i = 0; i < darkAeraBm2.Width; i++)
                    {
                        var c = darkAeraBm2.GetPixel(i, j);
                        if (c.R + c.G + c.B > 127 * 3)
                        {
                            return false;
                        }
                    }
                }

                darkArea = ChoiceDarkArea3;
                using var darkAeraBm3 = BitmapUtilities.CaptureAreaFromBitmap(game, darkArea);
                for (int j = 0; j < darkAeraBm3.Height; j++)
                {
                    for (int i = 0; i < darkAeraBm3.Width; i++)
                    {
                        var c = darkAeraBm3.GetPixel(i, j);
                        if (c.R + c.G + c.B > 127 * 3)
                        {
                            return false;
                        }
                    }
                }

                darkArea = Choice1LeftArea;
                using var darkAeraBm4 = BitmapUtilities.CaptureAreaFromBitmap(game, darkArea);
                var left = darkAeraBm4.GetPixel(0, 0);
                var right = darkAeraBm4.GetPixel(darkArea.Width - 1, 0);
                if (!BitmapUtilities.IsDark(left) || !BitmapUtilities.IsWhite(right) || !CheckChoice(game, C1IconCheckingArea, C1CharacterCheckingArea))
                {
                    return false;
                }
                return true;
            }

            private bool CheckChoice(Bitmap game, Rectangle iconArea, Rectangle characterArea)
            {
                using var bitmap = BitmapUtilities.CaptureAreaFromBitmap(game, iconArea);

                var c0 = bitmap.GetPixel(0, 0);
                var c1 = bitmap.GetPixel(iconArea.Width - 1, 0);
                var c2 = bitmap.GetPixel(iconArea.Width / 2, 0);
                if (!BitmapUtilities.IsSimilar(c0, c1) && BitmapUtilities.IsSimilar(c2, Color.White))
                {
                    return false;
                }
                using var charBitmap = BitmapUtilities.CaptureAreaFromBitmap(game, characterArea);

                for (int j = 0; j < characterArea.Height; j++)
                {
                    for (int i = 0; i < characterArea.Width; i++)
                    {
                        var c = charBitmap.GetPixel(i, j);
                        if (c.IsSimilar(CharacterColor, 5))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public Bitmap CombineChoices(Bitmap game)
            {
                if (!CheckChoiceDark(game))
                {
                    return null;
                }
                var choicesExist = Enumerable.Range(0, 5)
                    .Select(i => CheckChoice(game, IconCheckingAreas[i], CharacterCheckingAreas[i]))
                    .ToArray();
                var bms = new List<Bitmap>();

                if (!choicesExist.Any(a => a))
                {
                    return null;
                }

                for (var i = 0; i < choicesExist.Length; i++)
                {
                    if (choicesExist[i])
                    {
                        bms.Add(BitmapUtilities.CaptureAreaFromBitmap(game, ChoicesAreas[i]));
                    }
                }
                var resultBm = BitmapUtilities.CombineBitmaps(bms);
                bms.ForEach(bm => bm.Dispose());
                return resultBm;
            }

            public Rectangle BuildArea(Bitmap bitmap)
            {
                return new Rectangle(C1Area.X, C1Area.Y, bitmap.Width, bitmap.Height);
            }
        }
    }
}
