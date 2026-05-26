using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using HRM.backend.src.HRM.Application.DTOs.TasksTraining;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Services;
using Microsoft.AspNetCore.Http;

namespace HRM.backend.src.HRM.Application.Services.TasksTraining
{
    public class ExcelKpiParserService : IExcelKpiParserService
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".xlsx",
            ".csv"
        };

        public async Task<List<KpiImportRowDto>> ParseToDtoListAsync(IFormFile file, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File KPI không được để trống.");

            var extension = Path.GetExtension(file.FileName);
            if (!SupportedExtensions.Contains(extension))
                throw new ArgumentException("Chỉ hỗ trợ file .xlsx hoặc .csv.");

            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);
            stream.Position = 0;

            return extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                ? ParseCsv(stream)
                : ParseXlsx(stream);
        }

        private static List<KpiImportRowDto> ParseCsv(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var rows = new List<List<string>>();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                    rows.Add(line.Split(',').Select(cell => cell.Trim().Trim('"')).ToList());
            }

            return MapRows(rows);
        }

        private static List<KpiImportRowDto> ParseXlsx(Stream stream)
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var sharedStrings = ReadSharedStrings(archive);
            var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                ?? throw new ArgumentException("Không tìm thấy sheet dữ liệu đầu tiên trong file Excel.");

            using var sheetStream = sheetEntry.Open();
            var document = XDocument.Load(sheetStream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            var rows = document.Descendants(ns + "row")
                .Select(row =>
                {
                    var cells = new SortedDictionary<int, string>();
                    foreach (var cell in row.Elements(ns + "c"))
                    {
                        var reference = cell.Attribute("r")?.Value ?? string.Empty;
                        var columnIndex = GetColumnIndex(reference);
                        if (columnIndex < 0) continue;

                        cells[columnIndex] = ReadCellValue(cell, sharedStrings, ns);
                    }

                    if (!cells.Any()) return new List<string>();
                    var maxIndex = cells.Keys.Max();
                    var values = new List<string>();
                    for (var i = 0; i <= maxIndex; i++)
                        values.Add(cells.TryGetValue(i, out var value) ? value.Trim() : string.Empty);
                    return values;
                })
                .Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value)))
                .ToList();

            return MapRows(rows);
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return new List<string>();

            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            return document.Descendants(ns + "si")
                .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
                .ToList();
        }

        private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings, XNamespace ns)
        {
            var type = cell.Attribute("t")?.Value;
            var value = cell.Element(ns + "v")?.Value ?? string.Empty;

            if (type == "s" && int.TryParse(value, out var index) && index >= 0 && index < sharedStrings.Count)
                return sharedStrings[index];

            if (type == "inlineStr")
                return string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));

            return value;
        }

        private static int GetColumnIndex(string cellReference)
        {
            var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
            if (string.IsNullOrWhiteSpace(letters)) return -1;

            var index = 0;
            foreach (var letter in letters.ToUpperInvariant())
                index = index * 26 + (letter - 'A' + 1);
            return index - 1;
        }

        private static List<KpiImportRowDto> MapRows(List<List<string>> rows)
        {
            if (rows.Count < 2)
                throw new ArgumentException("File KPI phải có dòng tiêu đề và ít nhất một dòng dữ liệu.");

            var headers = rows[0]
                .Select((value, index) => new { Key = NormalizeHeader(value), Index = index })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .GroupBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.First().Index);

            var employeeCodeIndex = FindHeader(headers, "employeecode", "manv", "manhanvien", "employeeid");
            var kpiNameIndex = FindHeader(headers, "kpiname", "tenchitieu", "chitieu", "tenkpi");
            var weightIndex = FindHeader(headers, "weightpercent", "weight", "trongso", "tytrong");

            if (employeeCodeIndex < 0 || kpiNameIndex < 0 || weightIndex < 0)
                throw new ArgumentException("File KPI thiếu cột bắt buộc: Mã NV, Tên chỉ tiêu, Trọng số.");

            var kpiCodeIndex = FindHeader(headers, "kpicode", "machitieu", "makpi");
            var descriptionIndex = FindHeader(headers, "description", "mota", "ghichu");
            var targetIndex = FindHeader(headers, "targetvalue", "target", "muctieu", "chitieuvalue");
            var unitIndex = FindHeader(headers, "unit", "donvi");

            return rows
                .Skip(1)
                .Select((row, index) => new KpiImportRowDto
                {
                    RowNumber = index + 2,
                    EmployeeCode = GetCell(row, employeeCodeIndex),
                    KpiCode = kpiCodeIndex >= 0 ? GetCell(row, kpiCodeIndex) : null,
                    KpiName = GetCell(row, kpiNameIndex),
                    WeightPercent = ParseInt(GetCell(row, weightIndex)),
                    Description = descriptionIndex >= 0 ? GetCell(row, descriptionIndex) : null,
                    TargetValue = targetIndex >= 0 ? ParseDecimal(GetCell(row, targetIndex)) : null,
                    Unit = unitIndex >= 0 ? GetCell(row, unitIndex) : null
                })
                .Where(row => !string.IsNullOrWhiteSpace(row.EmployeeCode) ||
                              !string.IsNullOrWhiteSpace(row.KpiName) ||
                              row.WeightPercent > 0)
                .ToList();
        }

        private static int FindHeader(Dictionary<string, int> headers, params string[] names)
        {
            foreach (var name in names)
            {
                if (headers.TryGetValue(name, out var index))
                    return index;
            }
            return -1;
        }

        private static string GetCell(List<string> row, int index)
        {
            return index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;
        }

        private static int ParseInt(string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
                return (int)decimalValue;
            return 0;
        }

        private static decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }

        private static string NormalizeHeader(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            var replacements = new Dictionary<char, char>
            {
                ['á'] = 'a', ['à'] = 'a', ['ả'] = 'a', ['ã'] = 'a', ['ạ'] = 'a',
                ['ă'] = 'a', ['ắ'] = 'a', ['ằ'] = 'a', ['ẳ'] = 'a', ['ẵ'] = 'a', ['ặ'] = 'a',
                ['â'] = 'a', ['ấ'] = 'a', ['ầ'] = 'a', ['ẩ'] = 'a', ['ẫ'] = 'a', ['ậ'] = 'a',
                ['é'] = 'e', ['è'] = 'e', ['ẻ'] = 'e', ['ẽ'] = 'e', ['ẹ'] = 'e',
                ['ê'] = 'e', ['ế'] = 'e', ['ề'] = 'e', ['ể'] = 'e', ['ễ'] = 'e', ['ệ'] = 'e',
                ['í'] = 'i', ['ì'] = 'i', ['ỉ'] = 'i', ['ĩ'] = 'i', ['ị'] = 'i',
                ['ó'] = 'o', ['ò'] = 'o', ['ỏ'] = 'o', ['õ'] = 'o', ['ọ'] = 'o',
                ['ô'] = 'o', ['ố'] = 'o', ['ồ'] = 'o', ['ổ'] = 'o', ['ỗ'] = 'o', ['ộ'] = 'o',
                ['ơ'] = 'o', ['ớ'] = 'o', ['ờ'] = 'o', ['ở'] = 'o', ['ỡ'] = 'o', ['ợ'] = 'o',
                ['ú'] = 'u', ['ù'] = 'u', ['ủ'] = 'u', ['ũ'] = 'u', ['ụ'] = 'u',
                ['ư'] = 'u', ['ứ'] = 'u', ['ừ'] = 'u', ['ử'] = 'u', ['ữ'] = 'u', ['ự'] = 'u',
                ['ý'] = 'y', ['ỳ'] = 'y', ['ỷ'] = 'y', ['ỹ'] = 'y', ['ỵ'] = 'y',
                ['đ'] = 'd'
            };

            var builder = new StringBuilder();
            foreach (var ch in normalized)
            {
                if (replacements.TryGetValue(ch, out var replacement))
                    builder.Append(replacement);
                else if (char.IsLetterOrDigit(ch))
                    builder.Append(ch);
            }

            return builder.ToString();
        }
    }
}
