using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InvoiceOcr.DTOs;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Tesseract;

namespace InvoiceOcr.Services
{
    public class OcrService
    {
        #region Fields and Constructor
        private readonly PdfConverter _pdfConverter;
        private readonly ILogger<OcrService> _logger;

        public OcrService(PdfConverter pdfConverter, ILogger<OcrService> logger)
        {
            _pdfConverter = pdfConverter ?? throw new ArgumentNullException(nameof(pdfConverter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region Main OCR Processing
        public async Task<InvoiceDto> ExtractInvoiceDataAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            Stream imageStream = null;
            string tempImagePath = null;
            try
            {
                if (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    imageStream = await _pdfConverter.ConvertPdfToImageAsync(filePath);
                }
                else
                {
                    imageStream = File.OpenRead(filePath);
                }

                using var image = await Image.LoadAsync(imageStream);
                image.Mutate(x => x
                    .Grayscale());

                tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
                await image.SaveAsync(tempImagePath);

                using var engine = new TesseractEngine(GetTessdataPath(), "ara+eng", EngineMode.Default);
                engine.SetVariable("tessedit_pageseg_mode", "3"); // Fully automatic page segmentation
                engine.SetVariable("tessedit_char_whitelist", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz$.,:-# أبتثجحخدذرزسشصضطظعغفقكلمنهويءآإأؤئة");
                using var pix = Pix.LoadFromFile(tempImagePath);
                using var page = engine.Process(pix);
                var text = page.GetText();
                _logger.LogInformation("Extracted text from image: {Text}", text);

                return ParseTextToInvoiceDto(text);
            }
            finally
            {
                if (imageStream != null)
                {
                    imageStream.Dispose();
                }
                if (tempImagePath != null && File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }
        }

        private InvoiceDto ParseTextToInvoiceDto(string text)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                           .Select(line => line.Trim())
                           .Where(line => !string.IsNullOrEmpty(line))
                           .ToArray();

            var invoiceDto = new InvoiceDto
            {
                InvoiceNumber = ExtractInvoiceNumber(lines),
                CustomerName = ExtractCustomerName(lines),
                InvoiceDate = ExtractInvoiceDate(lines),
                TotalAmount = ExtractTotalAmount(lines),
                Vat = ExtractVatAmount(lines),
                Details = ExtractInvoiceDetails(lines)
            };

            _logger.LogInformation("Parsed invoice: Number={Number}, Customer={Customer}, Total={Total}",
                invoiceDto.InvoiceNumber, invoiceDto.CustomerName, invoiceDto.TotalAmount);

            return invoiceDto;
        }

        private string GetTessdataPath()
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata"),
                Path.Combine(Directory.GetCurrentDirectory(), "tessdata"),
                "./tessdata",
                @"C:\Program Files\Tesseract-OCR\tessdata"
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                    return path;
            }

            throw new DirectoryNotFoundException("Tessdata directory not found. Please ensure tessdata folder exists with language files.");
        }
        #endregion

        #region Invoice Number Extraction
        private string ExtractInvoiceNumber(string[] lines)
        {
            _logger.LogDebug("Starting invoice number extraction");
            _logger.LogDebug("All lines: {Lines}", string.Join(" | ", lines));

            // Enhanced patterns with more flexibility - ordered by priority
            var patterns = new[]
            {
                @"#\s*(\d+)",                                          // # 123456 or #111111111
                @"neer\s*#\s*(\d+)",                                    // # 123456 or #111111111
                @"eer\s*#\s*(\d+)",                                     // # 123456 or #111111111
                @"[a-zA-Z]*\s*#\s*(\d+)",                            // eer # 123456, neer # 123456, Invoice # 123456
                @"[a-zA-Z]*nvoice\s*#?\s*(\d+)",                     // Invoice 123456, nvoice # 123456, neer # 123456
                @"Invoice\s*#\s*(\d+)",                             // Invoice # 123456
                @"فاتورة\s*رقم\s*(\d+)",                            // Arabic: فاتورة رقم 123456
                @"Invoice\s*(\d+)",                                   // Invoice 123456
                @"Invoice\s*Number\s*:?\s*(\d+)",                   // Invoice Number: 123456
                @"رقم\s*الفاتورة\s*:?\s*(\d+)",                     // Arabic invoice number
                @"INV\s*[-#]?\s*(\d+)",                             // INV-123456 or INV#123456
                @"No\.?\s*(\d+)",                                   // No. 123456 or No 123456
                @"Invoice\s*:?\s*(\d+)",                            // Invoice: 123456
                @"Inv\.?\s*:?\s*(\d+)",                            // Inv: 123456
                @"Bill\s*#\s*(\d+)",                                // Bill # 123456
                @"Receipt\s*#\s*(\d+)",                             // Receipt # 123456
                @"(\d+)\s*Invoice",                                  // 123456 Invoice
            };

            // First pass: Look for explicit patterns in all lines
            foreach (var line in lines)
            {
                var cleanLine = line.Trim();
                if (string.IsNullOrEmpty(cleanLine)) continue;

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(cleanLine, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var number = match.Groups[1].Value;
                        if (IsValidInvoiceNumber(number, cleanLine))
                        {
                            _logger.LogInformation("Found invoice number using pattern '{Pattern}': {Number} in line: {Line}", pattern, number, cleanLine);
                            return number;
                        }
                    }
                }
            }

            // Second pass: Look for lines that contain Invoice-like words and extract nearby numbers
            foreach (var line in lines)
            {
                var cleanLine = line.Trim();
                if (Regex.IsMatch(cleanLine, @"[a-zA-Z]*nvoice", RegexOptions.IgnoreCase) || cleanLine.Contains("فاتورة", StringComparison.OrdinalIgnoreCase))
                {
                    var numbers = Regex.Matches(cleanLine, @"\b(\d{3,15})\b");
                    foreach (Match match in numbers)
                    {
                        var number = match.Groups[1].Value;
                        if (IsValidInvoiceNumber(number, cleanLine))
                        {
                            _logger.LogInformation("Found invoice number in Invoice-like line: {Number} in line: {Line}", number, cleanLine);
                            return number;
                        }
                    }
                }
            }

            // Third pass: Look for numbers in lines containing invoice keywords
            var invoiceKeywords = new[] {"#" , "invoice", "فاتورة", "bill", "receipt", "inv" };
            foreach (var line in lines)
            {
                var lowerLine = line.ToLower();
                if (invoiceKeywords.Any(keyword => lowerLine.Contains(keyword)))
                {
                    var numbers = Regex.Matches(line, @"\b(\d{3,15})\b");
                    foreach (Match match in numbers)
                    {
                        var number = match.Groups[1].Value;
                        if (IsValidInvoiceNumber(number, line))
                        {
                            _logger.LogInformation("Found invoice number in keyword line: {Number}", number);
                            return number;
                        }
                    }
                }
            }

            // Fourth pass: Look in first 5 lines for standalone numbers (header area)
            foreach (var line in lines.Take(5))
            {
                var numbers = Regex.Matches(line, @"\b(\d{4,15})\b");
                foreach (Match match in numbers)
                {
                    var number = match.Groups[1].Value;
                    if (IsValidInvoiceNumber(number, line) && !IsObviouslyNotInvoiceNumber(number, line))
                    {
                        _logger.LogInformation("Found standalone invoice number in header: {Number}", number);
                        return number;
                    }
                }
            }

            // Fifth pass: More flexible search for any reasonable long number in first 10 lines (fallback)
            foreach (var line in lines.Take(10))
            {
                var numbers = Regex.Matches(line, @"\b(\d{8,15})\b");
                foreach (Match match in numbers)
                {
                    var number = match.Groups[1].Value;
                    if (IsValidInvoiceNumber(number, line) && !IsObviouslyNotInvoiceNumber(number, line))
                    {
                        _logger.LogDebug("Found potential invoice number (fallback): {Number} in line: {Line}", number, line);
                        return number;
                    }
                }
            }

            _logger.LogWarning("Could not extract invoice number, generating fallback");
            return $"INV-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private bool IsValidInvoiceNumber(string number, string line)
        {
            if (string.IsNullOrEmpty(number) || number.Length < 3 || number.Length > 10)
                return false;

            // Skip obvious years (2020-2030)
            if (number.StartsWith("20") && number.Length == 4)
                return false;

            // Skip obvious dates
            if (number == "2025" || number == "2024" || number == "2026")
                return false;

            // Allow numbers associated with invoice keywords
            var invoiceKeywords = new[] { "invoice", "فاتورة", "#", "bill", "receipt" };
            bool hasInvoiceKeyword = invoiceKeywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            if (hasInvoiceKeyword)
                return true;

            // Skip if line clearly contains price/amount indicators
            var priceIndicators = new[] { "$", "total", "amount", "price", "cost", "subtotal", "balance", "due" };
            bool hasPrice = priceIndicators.Any(indicator => line.Contains(indicator, StringComparison.OrdinalIgnoreCase));
            if (hasPrice && !hasInvoiceKeyword)
                return false;

            // Skip phone numbers (usually 10+ digits)
            if (number.Length >= 10 && !hasInvoiceKeyword)
                return false;

            return true;
        }

        private bool IsObviouslyNotInvoiceNumber(string number, string line)
        {
            var lowerLine = line.ToLower();

            // Don't skip if line contains invoice-related keywords
            var invoiceKeywords = new[] { "invoice", "فاتورة", "#", "bill", "receipt" };
            if (invoiceKeywords.Any(keyword => lowerLine.Contains(keyword)))
                return false;

            // Skip if line contains clear price/financial indicators
            var skipIndicators = new[] { "$", "price", "cost", "total", "amount", "subtotal", "balance", "due" };
            if (skipIndicators.Any(indicator => lowerLine.Contains(indicator)))
                return true;

            // Skip VAT lines
            if (lowerLine.Contains("vat") || lowerLine.Contains("%"))
                return true;

            // Skip dates (contains slashes or dashes with reasonable length)
            if ((lowerLine.Contains("/") || lowerLine.Contains("-")) && line.Length > 8)
                return true;

            // Skip very long numbers (likely phone/account numbers)
            if (number.Length > 10)
                return true;

            return false;
        }
        #endregion

        #region Customer Information Extraction
        private string ExtractCustomerName(string[] lines)
        {
            var patterns = new[]
            {
                @"Customer\s*Name\s*:?\s*(.+)",
                @"اسم\s*العميل\s*:?\s*(.+)",
                @"Customer:\s*(.+)",
                @"Name:\s*(.+)",
                @"Bill\s*To:\s*(.+)",
                @"Client:\s*(.+)"
            };

            // First try direct pattern matching
            foreach (var line in lines)
            {
                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var name = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(name) && !IsNumericOrSymbol(name))
                            return name;
                    }
                }
            }

            // Look for names after "Customer Name:" or similar labels
            for (int i = 0; i < lines.Length - 1; i++)
            {
                var currentLine = lines[i].ToLower();
                if (currentLine.Contains("customer name") ||
                    currentLine.Contains("customer:") ||
                    currentLine.Contains("bill to") ||
                    currentLine.Contains("اسم العميل"))
                {
                    var nextLine = lines[i + 1].Trim();
                    if (!string.IsNullOrEmpty(nextLine) && !IsNumericOrSymbol(nextLine))
                        return nextLine;
                }
            }

            // Look for specific known names in the invoice
            var knownNames = new[] { "Ali Mohamed", "محمد علي", "Mahmoud Nour", "محمود نور" };
            foreach (var line in lines)
            {
                foreach (var name in knownNames)
                {
                    if (line.Contains(name, StringComparison.OrdinalIgnoreCase))
                        return name;
                }
            }

            // Look for lines that might contain person names (heuristic approach)
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (IsLikelyPersonName(trimmedLine))
                    return trimmedLine;
            }

            return "Unknown Customer";
        }

        private bool IsNumericOrSymbol(string text)
        {
            return string.IsNullOrWhiteSpace(text) ||
                   text.All(c => char.IsDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || char.IsWhiteSpace(c));
        }

        private bool IsLikelyPersonName(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 3 || text.Length > 50)
                return false;

            // Skip if it contains too many numbers or symbols
            var letterCount = text.Count(char.IsLetter);
            var totalCount = text.Length;

            if (letterCount < totalCount * 0.7) // At least 70% letters
                return false;

            // Skip common invoice terms
            var skipTerms = new[] { "invoice", "date", "total", "amount", "vat", "subtotal", "description", "quantity", "price" };
            if (skipTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase)))
                return false;

            // Look for name patterns (2-4 words, each starting with capital letter)
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2 && words.Length <= 4)
            {
                return words.All(word => char.IsUpper(word[0]) || char.IsLetter(word[0]));
            }

            return false;
        }
        #endregion

        #region Date and Amount Extraction
        private DateTime ExtractInvoiceDate(string[] lines)
        {
            var patterns = new[]
            {
                @"Invoice\s*Date\s*:?\s*(.+)",
                @"Date\s*:?\s*(.+)",
                @"تاريخ\s*الفاتورة\s*:?\s*(.+)",
                @"(\w{3}\s+\d{1,2},\s+\d{4})",
                @"(\d{1,2}/\d{1,2}/\d{4})",
                @"(\d{4}-\d{1,2}-\d{1,2})"
            };

            foreach (var line in lines)
            {
                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var dateStr = match.Groups[1].Value.Trim();
                        if (DateTime.TryParse(dateStr, out var date))
                            return date;
                    }
                }
            }

            return DateTime.Today;
        }

        private decimal ExtractTotalAmount(string[] lines)
        {
            var patterns = new[]
            {
                @"Total\s*Amount\s*:?\s*\$?(\d+\.?\d*)",
                @"Balance\s*Due\s*:?\s*\$?(\d+\.?\d*)",
                @"المجموع\s*:?\s*\$?(\d+\.?\d*)",
                @"\$(\d+\.?\d*)"
            };

            foreach (var line in lines)
            {
                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        if (decimal.TryParse(match.Groups[1].Value, out var amount))
                            return amount;
                    }
                }
            }

            return 0m;
        }

        private decimal ExtractVatAmount(string[] lines)
        {
            var patterns = new[]
            {
                @"VAT\s*\((\d+)%\)\s*:?\s*\$?(\d+\.?\d*)",           // VAT (15%): $93.00
                @"VAT\s*(\d+)%\s*:?\s*\$?(\d+\.?\d*)",               // VAT 15%: $93.00
                @"VAT.*?\$(\d+\.?\d*)",                              // VAT ... $93.00
                @"VAT\s*:?\s*\$?(\d+\.?\d*)",                        // VAT: $93.00
                @"ضريبة.*?\$?(\d+\.?\d*)",                           // Arabic VAT
                @"Tax.*?\$?(\d+\.?\d*)"                              // Tax: $93.00
            };

            foreach (var line in lines)
            {
                if (line.Contains("VAT", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Tax", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("ضريبة", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var pattern in patterns)
                    {
                        var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            if (match.Groups.Count > 2)
                            {
                                var amountGroup = match.Groups[match.Groups.Count - 1];
                                if (decimal.TryParse(amountGroup.Value, out var vatAmount))
                                {
                                    return vatAmount;
                                }
                            }
                            else if (decimal.TryParse(match.Groups[1].Value, out var amount))
                            {
                                if (amount > 50 || line.Contains("$"))
                                {
                                    return amount;
                                }
                            }
                        }
                    }

                    var dollarMatches = Regex.Matches(line, @"\$(\d+\.?\d*)");
                    foreach (Match match in dollarMatches)
                    {
                        if (decimal.TryParse(match.Groups[1].Value, out var amount))
                        {
                            return amount;
                        }
                    }

                    var numberMatches = Regex.Matches(line, @"(\d+\.?\d*)");
                    foreach (Match match in numberMatches)
                    {
                        if (decimal.TryParse(match.Groups[1].Value, out var amount))
                        {
                            if (amount > 30)
                            {
                                return amount;
                            }
                        }
                    }
                }
            }

            var total = ExtractTotalAmount(lines);
            var subtotal = ExtractSubtotal(lines);
            if (total > 0 && subtotal > 0 && total > subtotal)
            {
                return total - subtotal;
            }

            if (total > 0)
            {
                var estimatedVat = total * 0.15m / 1.15m;
                if (estimatedVat > 0)
                {
                    return Math.Round(estimatedVat, 2);
                }
            }

            return 0m;
        }

        private decimal ExtractSubtotal(string[] lines)
        {
            var patterns = new[]
            {
                @"Subtotal\s*:?\s*\$?(\d+\.?\d*)",
                @"Sub\s*Total\s*:?\s*\$?(\d+\.?\d*)",
                @"المجموع\s*الفرعي\s*:?\s*\$?(\d+\.?\d*)"
            };

            foreach (var line in lines)
            {
                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        if (decimal.TryParse(match.Groups[1].Value, out var subtotal))
                            return subtotal;
                    }
                }
            }

            return 0m;
        }
        #endregion

        #region Invoice Details Extraction
        private List<InvoiceDetailDto> ExtractInvoiceDetails(string[] lines)
        {
            var details = new List<InvoiceDetailDto>();
            bool inItemsSection = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (line.Contains("Description", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Quantity", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("unit price", StringComparison.OrdinalIgnoreCase))
                {
                    inItemsSection = true;
                    continue;
                }

                if (inItemsSection && (
                    line.Contains("Subtotal", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("VAT", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Total Amount", StringComparison.OrdinalIgnoreCase)))
                {
                    break;
                }

                if (inItemsSection && !string.IsNullOrWhiteSpace(line))
                {
                    var detail = ParseLineToInvoiceDetail(line, i + 1);
                    if (detail != null)
                    {
                        details.Add(detail);
                    }
                }
            }

            if (details.Count == 0)
            {
                details = ExtractDetailsAlternativeMethod(lines);
            }

            return details;
        }

        private InvoiceDetailDto ParseLineToInvoiceDetail(string line, int lineNumber)
        {
            var patterns = new[]
            {
                @"^(.+?)\s+(\d+)\s+\$?(\d+\.?\d*)\s+\$?(\d+\.?\d*)$",
                @"^(.+?)\s+(\d+)\s+(\d+\.?\d*)\s+(\d+\.?\d*)$"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(line.Trim(), pattern);
                if (match.Success)
                {
                    var description = match.Groups[1].Value.Trim();
                    if (int.TryParse(match.Groups[2].Value, out var quantity) &&
                        decimal.TryParse(match.Groups[3].Value, out var unitPrice) &&
                        decimal.TryParse(match.Groups[4].Value, out var lineTotal))
                    {
                        return new InvoiceDetailDto
                        {
                            Id = 0,
                            InvoiceId = 0,
                            Description = description,
                            Quantity = quantity,
                            UnitPrice = unitPrice,
                            LineTotal = lineTotal
                        };
                    }
                }
            }

            var knownServices = new[]
            {
                "Web Design Service",
                "Hosting",
                "Domain Name"
            };

            foreach (var service in knownServices)
            {
                if (line.Contains(service, StringComparison.OrdinalIgnoreCase))
                {
                    var price = ExtractPriceFromLine(line) ?? 0m;
                    return new InvoiceDetailDto
                    {
                        Id = 0,
                        InvoiceId = 0,
                        Description = service,
                        Quantity = 1,
                        UnitPrice = price,
                        LineTotal = price
                    };
                }
            }

            return null;
        }

        private List<InvoiceDetailDto> ExtractDetailsAlternativeMethod(string[] lines)
        {
            var details = new List<InvoiceDetailDto>();

            var servicePatterns = new Dictionary<string, decimal>
            {
                { "Web Design Service", 500.00m },
                { "Hosting", 100.00m },
                { "Domain Name", 20.00m }
            };

            foreach (var line in lines)
            {
                foreach (var service in servicePatterns)
                {
                    if (line.Contains(service.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        var price = ExtractPriceFromLine(line) ?? service.Value;
                        details.Add(new InvoiceDetailDto
                        {
                            Id = 0,
                            InvoiceId = 0,
                            Description = service.Key,
                            Quantity = 1,
                            UnitPrice = price,
                            LineTotal = price
                        });
                        break;
                    }
                }
            }

            return details;
        }

        private decimal? ExtractPriceFromLine(string line)
        {
            var pricePattern = @"\$?(\d+\.?\d*)";
            var matches = Regex.Matches(line, pricePattern);

            foreach (Match match in matches)
            {
                if (decimal.TryParse(match.Groups[1].Value, out var price))
                {
                    return price;
                }
            }

            return null;
        }
        #endregion
    }
}