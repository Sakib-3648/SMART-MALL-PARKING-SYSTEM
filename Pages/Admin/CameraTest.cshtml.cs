
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;
using Tesseract;

namespace SMART_MALL_PARKING_SYSTEM.Pages.Admin
{
    public class CameraTestModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;

        public CameraTestModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [BindProperty]
        public IFormFile? PlateImage { get; set; }

        public string Message { get; set; } = "";
        public string DetectedPlate { get; set; } = "";
        public string RawOcrText { get; set; } = "";
        [BindProperty]
        public string ScannerMode { get; set; } = "Plate";

        public string GeneralTextResult { get; set; } = "";
        public string ProcessedImagePreview { get; set; } = "";

        public IActionResult OnGet()
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            return Page();
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("AdminLoggedIn")
                == "true";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            if (PlateImage == null || PlateImage.Length == 0)
            {
                Message = "Please select a vehicle plate photo.";
                return Page();
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (PlateImage.Length > maxFileSize)
            {
                Message = "Image must be smaller than 5 MB.";
                return Page();
            }

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (string.IsNullOrWhiteSpace(PlateImage.ContentType) ||
                !allowedTypes.Contains(
                    PlateImage.ContentType.ToLowerInvariant()))
            {
                Message = "Only JPG, PNG or WebP images are allowed.";
                return Page();
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await PlateImage.CopyToAsync(memoryStream);

                byte[] imageBytes = memoryStream.ToArray();
                // General Text Scanner - separate from existing plate OCR.
                if (ScannerMode == "General")
                {
                    string languagePath = Path.Combine(
                        _environment.ContentRootPath,
                        "tessdata");

                    using var generalEngine = new TesseractEngine(
                        languagePath,
                        "eng",
                        EngineMode.Default);

                    // Improve the image before OCR recognition.
                    using var generalOriginal =
                        SixLabors.ImageSharp.Image.Load<Rgba32>(imageBytes);

                    // Limit image dimensions for safety.
                    if ((long)generalOriginal.Width * generalOriginal.Height > 20_000_000)
                    {
                        Message = "Image dimensions are too large.";
                        return Page();
                    }

                    // Enhance handwriting and printed text.
                    using var generalEnhanced = generalOriginal.Clone(context =>
                    {
                        context.Grayscale();
                        context.Contrast(1.5f);
                    });

                    // Convert enhanced image to PNG.
                    using var generalStream = new MemoryStream();

                    generalEnhanced.SaveAsPng(generalStream);

                    byte[] generalBytes = generalStream.ToArray();

                    // Display the enhanced image for testing.
                    ProcessedImagePreview =
                        "data:image/png;base64," +
                        Convert.ToBase64String(generalBytes);

                    // Run OCR on the enhanced image.
                    using var generalImage = Pix.LoadFromMemory(generalBytes);

                    using var generalPage = generalEngine.Process(
                        generalImage,
                        PageSegMode.SingleLine);

                    GeneralTextResult = generalPage.GetText()?.Trim() ?? "";


                    Message = string.IsNullOrWhiteSpace(GeneralTextResult)
                        ? "No text detected. Try a clearer image."
                        : "General text scanning completed.";

                    return Page();
                }
                // Decode the image and verify its contents.
                using var original =
                    SixLabors.ImageSharp.Image.Load<Rgba32>(
                        imageBytes);

                // Prevent extremely large images.
                if ((long)original.Width * original.Height
                    > 20_000_000)
                {
                    Message = "Image dimensions are too large.";
                    return Page();
                }

                string tessdataPath = Path.Combine(
                    _environment.ContentRootPath,
                    "tessdata");

                using var engine = new TesseractEngine(
                    tessdataPath,
                    "eng",
                    EngineMode.Default);

                engine.SetVariable(
                    "user_defined_dpi",
                    "300");

                // Test 1: Original image.
                string originalResult = RunOcr(
                    engine,
                    original,
                    PageSegMode.Auto);

                // Test 2: Crop the middle of the image.
                // This is designed for the supplied test image,
                // where the plate is centered.
                int cropX = original.Width / 6;
                int cropY = original.Height / 4;
                int cropWidth = original.Width * 2 / 3;
                int cropHeight = original.Height / 2;

                var cropArea = new Rectangle(
                    cropX,
                    cropY,
                    cropWidth,
                    cropHeight);

                using var cropped = original.Clone(
                    context => context.Crop(cropArea));

                // Test 3: Enlarge, grayscale and increase contrast.
                using var enhanced = cropped.Clone(
                    context =>
                    {
                        context.Resize(
                            cropped.Width * 2,
                            cropped.Height * 2);

                        context.Grayscale();
                        context.Contrast(1.5f);
                    });
                using var previewStream = new MemoryStream();

                await enhanced.SaveAsPngAsync(previewStream);

                ProcessedImagePreview =
                    "data:image/png;base64," +
                    Convert.ToBase64String(previewStream.ToArray());
                string enhancedResult = RunOcr(
                    engine,
                    enhanced,
                    PageSegMode.SingleLine);

                // Test 4: Invert colors.
                using var inverted = enhanced.Clone(
                    context => context.Invert());

                string invertedResult = RunOcr(
                    engine,
                    inverted,
                    PageSegMode.SingleLine);

                // Show all OCR outputs for troubleshooting.
                RawOcrText =
                    "Original: " + ShowResult(originalResult) +
                    "\nEnhanced: " + ShowResult(enhancedResult) +
                    "\nInverted: " + ShowResult(invertedResult);

                // Prefer the enhanced result, then inverted,
                // then original if they contain plausible text.
                string[] results =
                {
                    enhancedResult,
                    invertedResult,
                    originalResult
                };

                foreach (string result in results)
                {
                    string normalized = Regex.Replace(
                        result.ToUpperInvariant(),
                        @"[^A-Z0-9]",
                        "");

                    if (Regex.IsMatch(
                        normalized,
                        @"^[A-Z0-9]{4,10}$"))
                    {
                        DetectedPlate = normalized;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(DetectedPlate))
                {
                    Message =
                        "OCR found a possible plate. " +
                        "Please verify the number manually.";
                }
                else
                {
                    Message =
                        "OCR could not identify a plate. " +
                        "Check the raw OCR results below.";
                }
            }
            catch (Exception ex)
            {
                Message = "OCR processing failed: " +
                    ex.GetType().Name;

                // Do not display internal exception details
                // on the public-facing page.
                RawOcrText = "";
            }

            return Page();
        }

        public IActionResult OnPostTestOcr()
        {
            if (!IsAdmin())
                return RedirectToPage("/Admin/Login");

            try
            {
                string tessdataPath = Path.Combine(
                    _environment.ContentRootPath,
                    "tessdata");

                using var engine = new TesseractEngine(
                    tessdataPath,
                    "eng",
                    EngineMode.Default);

                // Create a clean black-and-white test image
                // using a simple built-in bitmap font.
                const int scale = 12;
                string testText = "ABC1234";

                string[] letters =
                {
            "01110,10001,10001,11111,10001,10001,10001",
            "11110,10001,10001,11110,10001,10001,11110",
            "01111,10000,10000,10000,10000,10000,01111",
            "00100,01100,00100,00100,00100,00100,01110",
            "01110,10001,00001,00010,00100,01000,11111",
            "11110,00001,00001,01110,00001,00001,11110",
            "00010,00110,01010,10010,11111,00010,00010"
        };

                using var testImage =
      new SixLabors.ImageSharp.Image<Rgba32>(
          900,
          220,
          new Rgba32(255, 255, 255));

                for (int c = 0; c < testText.Length; c++)
                {
                    string[] rows = letters[c].Split(',');

                    for (int y = 0; y < rows.Length; y++)
                    {
                        for (int x = 0; x < rows[y].Length; x++)
                        {
                            if (rows[y][x] == '1')
                            {
                                for (int py = 0; py < scale; py++)
                                {
                                    for (int px = 0; px < scale; px++)
                                    {
                                        int imageX = 35 + c * 120 +
                                            x * scale + px;

                                        int imageY = 60 + y * scale + py;

                                        if (imageX < testImage.Width &&
                                            imageY < testImage.Height)
                                        {
                                            testImage[imageX, imageY] =
     new Rgba32(0, 0, 0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                using var outputStream = new MemoryStream();
                testImage.SaveAsPng(outputStream);

                byte[] imageBytes = outputStream.ToArray();

                ProcessedImagePreview =
                    "data:image/png;base64," +
                    Convert.ToBase64String(imageBytes);

                using var pix = Pix.LoadFromMemory(imageBytes);

                using var page = engine.Process(
                    pix,
                    PageSegMode.SingleLine);

                RawOcrText = page.GetText()?.Trim() ?? "";

                Message = string.IsNullOrWhiteSpace(RawOcrText)
                    ? "Diagnostic failed: No text detected."
                    : "Diagnostic completed. Check the OCR result.";

                ScannerMode = "Plate";
            }
            catch (Exception ex)
            {
                Message = "Diagnostic error: " +
                    ex.GetType().Name;
            }

            return Page();
        }

        private static string RunOcr(
            TesseractEngine engine,
            SixLabors.ImageSharp.Image<Rgba32> input,
            PageSegMode mode)
        {
            using var outputStream = new MemoryStream();

            input.SaveAsPng(outputStream);

            byte[] bytes = outputStream.ToArray();

            using var pix = Pix.LoadFromMemory(bytes);

            using var page = engine.Process(pix, mode);

            return page.GetText()?.Trim() ?? "";
        }

        private static string ShowResult(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "(No text detected)"
                : value.Trim();
        }
    }
}
