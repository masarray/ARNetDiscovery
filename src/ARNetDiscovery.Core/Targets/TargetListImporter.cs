using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ARNetDiscovery.Core.Targets;

public sealed class TargetListImporter
{
    private static readonly Regex CellRefRegex = new("^([A-Z]+)([0-9]+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public TargetImportResult Import(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Import path is empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("Target list file does not exist.", path);

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".xlsx" => ImportXlsx(path),
            ".csv" => ImportDelimited(path, ','),
            ".txt" => ImportTextList(path),
            _ => throw new NotSupportedException($"Unsupported target list format '{extension}'. Use .xlsx, .csv, or .txt.")
        };
    }

    private TargetImportResult ImportXlsx(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetInfos = ReadWorkbookSheets(archive);
        var warnings = new List<string>();

        foreach (var sheet in sheetInfos.OrderByDescending(s => string.Equals(s.Name, "IP", StringComparison.OrdinalIgnoreCase)))
        {
            var rows = ReadWorksheetRows(archive, sheet, sharedStrings);
            var result = TryParseTableRows(rows, path, sheet.Name, warnings);
            if (result.Targets.Count > 0)
                return result;
        }

        warnings.Add("No target rows with valid IPv4 address were found in the workbook.");
        return new TargetImportResult
        {
            Title = Path.GetFileNameWithoutExtension(path),
            SourcePath = path,
            Targets = Array.Empty<TargetDeviceRecord>(),
            Warnings = warnings
        };
    }

    private TargetImportResult ImportDelimited(string path, char delimiter)
    {
        var rows = File.ReadAllLines(path, Encoding.UTF8)
            .Select(line => SplitCsvLine(line, delimiter).ToArray())
            .ToArray();
        return TryParseTableRows(rows, path, Path.GetFileNameWithoutExtension(path), new List<string>());
    }

    private TargetImportResult ImportTextList(string path)
    {
        var targets = new List<TargetDeviceRecord>();
        var warnings = new List<string>();
        var lineNumber = 0;
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            lineNumber++;
            var ipText = ExtractFirstIpv4(line);
            if (ipText is null || !IPAddress.TryParse(ipText, out var ip))
                continue;

            targets.Add(new TargetDeviceRecord
            {
                IpAddress = ip,
                DeviceName = line.Replace(ipText, string.Empty).Trim(' ', '-', ',', ';', '\t'),
                ExpectedType = "Imported Target",
                Source = Path.GetFileName(path),
                SourceRow = lineNumber
            });
        }

        var unique = DeduplicateTargets(targets, warnings);
        return new TargetImportResult
        {
            Title = Path.GetFileNameWithoutExtension(path),
            SourcePath = path,
            Targets = unique,
            Warnings = warnings
        };
    }

    private TargetImportResult TryParseTableRows(IReadOnlyList<IReadOnlyList<string?>> rows, string path, string sheetName, List<string> warnings)
    {
        var headerIndex = FindHeaderRow(rows);
        if (headerIndex < 0)
        {
            warnings.Add($"Sheet '{sheetName}' skipped: could not find IP ADDRESS header.");
            return new TargetImportResult { Title = Path.GetFileNameWithoutExtension(path), SourcePath = path, Targets = Array.Empty<TargetDeviceRecord>(), Warnings = warnings };
        }

        var header = rows[headerIndex];
        var map = BuildHeaderMap(header);
        if (!map.TryGetValue("IPADDRESS", out var ipCol) && !map.TryGetValue("IP", out ipCol))
        {
            warnings.Add($"Sheet '{sheetName}' skipped: header found but IP ADDRESS column was not detected.");
            return new TargetImportResult { Title = Path.GetFileNameWithoutExtension(path), SourcePath = path, Targets = Array.Empty<TargetDeviceRecord>(), Warnings = warnings };
        }

        var targets = new List<TargetDeviceRecord>();
        AddInfrastructureRowsAboveHeader(rows, headerIndex, path, sheetName, targets);

        string currentBay = string.Empty;
        string currentBayType = string.Empty;
        string currentNo = string.Empty;

        for (var i = headerIndex + 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var ipText = Get(row, ipCol);
            if (!TryParseStrictIpv4(ipText, out var ip))
                continue;

            var bay = Get(row, map, "NAMABAY");
            var bayType = Get(row, map, "JENISBAY");
            var no = Get(row, map, "NO");
            if (!string.IsNullOrWhiteSpace(bay)) currentBay = bay;
            if (!string.IsNullOrWhiteSpace(bayType)) currentBayType = bayType;
            if (!string.IsNullOrWhiteSpace(no)) currentNo = no;

            var record = new TargetDeviceRecord
            {
                IpAddress = ip,
                DeviceName = Get(row, map, "NAMAIED"),
                BayName = string.IsNullOrWhiteSpace(bay) ? currentBay : bay,
                BayType = string.IsNullOrWhiteSpace(bayType) ? currentBayType : bayType,
                Panel = Get(row, map, "PANEL"),
                ExpectedType = Get(row, map, "JENISPERALATAN"),
                DeviceNo = Get(row, map, "NODEVICE"),
                Remark = Get(row, map, "REMARK"),
                Source = $"{Path.GetFileName(path)} · {sheetName}",
                SourceRow = i + 1
            };

            if (string.IsNullOrWhiteSpace(record.DeviceName))
                record = record with { DeviceName = !string.IsNullOrWhiteSpace(record.ExpectedType) ? record.ExpectedType : record.Ip };
            if (string.IsNullOrWhiteSpace(record.BayName))
                record = record with { BayName = currentBay };
            if (string.IsNullOrWhiteSpace(record.BayType))
                record = record with { BayType = currentBayType };
            if (string.IsNullOrWhiteSpace(record.DeviceNo))
                record = record with { DeviceNo = currentNo };

            targets.Add(record);
        }

        var unique = DeduplicateTargets(targets, warnings);
        return new TargetImportResult
        {
            Title = BuildTitle(path, sheetName, rows),
            SourcePath = path,
            Targets = unique,
            Warnings = warnings
        };
    }

    private static void AddInfrastructureRowsAboveHeader(IReadOnlyList<IReadOnlyList<string?>> rows, int headerIndex, string path, string sheetName, List<TargetDeviceRecord> targets)
    {
        for (var i = 0; i < headerIndex; i++)
        {
            var row = rows[i];
            for (var c = 0; c < row.Count; c++)
            {
                var label = Get(row, c);
                if (string.IsNullOrWhiteSpace(label) || !label.Contains("NTP", StringComparison.OrdinalIgnoreCase))
                    continue;

                for (var n = c + 1; n < Math.Min(row.Count, c + 4); n++)
                {
                    if (TryParseStrictIpv4(Get(row, n), out var ip))
                    {
                        targets.Add(new TargetDeviceRecord
                        {
                            IpAddress = ip,
                            DeviceName = label.Trim(),
                            ExpectedType = "NTP Server",
                            Source = $"{Path.GetFileName(path)} · {sheetName}",
                            SourceRow = i + 1,
                            Remark = "Infrastructure row detected above target table."
                        });
                        break;
                    }
                }
            }
        }
    }

    private static IReadOnlyList<TargetDeviceRecord> DeduplicateTargets(IEnumerable<TargetDeviceRecord> targets, List<string> warnings)
    {
        var unique = new Dictionary<string, TargetDeviceRecord>(StringComparer.Ordinal);
        foreach (var target in targets)
        {
            if (unique.ContainsKey(target.Ip))
            {
                warnings.Add($"Duplicate IP {target.Ip} skipped. Keeping first record '{unique[target.Ip].DeviceName}'.");
                continue;
            }
            unique[target.Ip] = target;
        }
        return unique.Values.OrderBy(t => ToSortKey(t.IpAddress)).ToArray();
    }

    private static int FindHeaderRow(IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        for (var r = 0; r < rows.Count; r++)
        {
            var normalized = rows[r].Select(NormalizeHeader).ToArray();
            if (normalized.Contains("IPADDRESS") || normalized.Contains("IP"))
            {
                if (normalized.Contains("NAMAIED") || normalized.Contains("JENISPERALATAN") || normalized.Contains("PANEL"))
                    return r;
            }
        }
        return -1;
    }

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string?> row)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < row.Count; i++)
        {
            var key = NormalizeHeader(row[i]);
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                map[key] = i;
        }
        return map;
    }

    private static string NormalizeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var chars = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToUpperInvariant();
    }

    private static string Get(IReadOnlyList<string?> row, IReadOnlyDictionary<string, int> map, string key)
        => map.TryGetValue(key, out var index) ? Get(row, index) : string.Empty;

    private static string Get(IReadOnlyList<string?> row, int index)
        => index >= 0 && index < row.Count ? (row[index] ?? string.Empty).Trim() : string.Empty;

    private static bool TryParseStrictIpv4(string? text, out IPAddress ip)
    {
        ip = IPAddress.None;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var clean = text.Trim();
        if (!Regex.IsMatch(clean, "^\\d{1,3}(\\.\\d{1,3}){3}$")) return false;
        if (!IPAddress.TryParse(clean, out var parsed)) return false;
        if (parsed.GetAddressBytes().Length != 4) return false;
        ip = parsed;
        return true;
    }

    private static string? ExtractFirstIpv4(string text)
    {
        var match = Regex.Match(text, "\\b\\d{1,3}(?:\\.\\d{1,3}){3}\\b");
        if (!match.Success) return null;
        return IPAddress.TryParse(match.Value, out var ip) && ip.GetAddressBytes().Length == 4 ? match.Value : null;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return Array.Empty<string>();
        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return doc.Descendants(ns + "si")
            .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
            .ToArray();
    }

    private static IReadOnlyList<SheetInfo> ReadWorkbookSheets(ZipArchive archive)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        using var workbookStream = archive.GetEntry("xl/workbook.xml")?.Open() ?? throw new InvalidDataException("Workbook XML not found.");
        var workbook = XDocument.Load(workbookStream);

        using var relStream = archive.GetEntry("xl/_rels/workbook.xml.rels")?.Open() ?? throw new InvalidDataException("Workbook relationships not found.");
        var rels = XDocument.Load(relStream)
            .Root!
            .Elements(packageRelNs + "Relationship")
            .ToDictionary(e => (string)e.Attribute("Id")!, e => (string)e.Attribute("Target")!, StringComparer.Ordinal);

        var sheets = new List<SheetInfo>();
        foreach (var sheet in workbook.Descendants(ns + "sheet"))
        {
            var name = (string?)sheet.Attribute("name") ?? "Sheet";
            var id = (string?)sheet.Attribute(relNs + "id") ?? string.Empty;
            if (!rels.TryGetValue(id, out var target)) continue;
            var targetPath = target.Replace('\\', '/').TrimStart('/');
            if (!targetPath.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                targetPath = "xl/" + targetPath;
            sheets.Add(new SheetInfo(name, targetPath));
        }
        return sheets;
    }

    private static IReadOnlyList<IReadOnlyList<string?>> ReadWorksheetRows(ZipArchive archive, SheetInfo sheet, IReadOnlyList<string> sharedStrings)
    {
        var entry = archive.GetEntry(sheet.Path);
        if (entry is null) return Array.Empty<IReadOnlyList<string?>>();

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = new List<List<string?>>();

        foreach (var rowElement in doc.Descendants(ns + "row"))
        {
            var rowIndex = ParseInt((string?)rowElement.Attribute("r")) - 1;
            if (rowIndex < 0) rowIndex = rows.Count;
            while (rows.Count <= rowIndex) rows.Add(new List<string?>());
            var row = rows[rowIndex];

            foreach (var cell in rowElement.Elements(ns + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? string.Empty;
                var colIndex = ParseColumnIndex(reference);
                if (colIndex < 0) continue;
                while (row.Count <= colIndex) row.Add(null);
                row[colIndex] = ReadCellValue(cell, ns, sharedStrings);
            }
        }

        return rows;
    }

    private static string ReadCellValue(XElement cell, XNamespace ns, IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        if (type == "inlineStr")
            return string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));

        var value = cell.Element(ns + "v")?.Value ?? string.Empty;
        if (type == "s" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) && index >= 0 && index < sharedStrings.Count)
            return sharedStrings[index];

        return value;
    }

    private static int ParseColumnIndex(string cellReference)
    {
        var match = CellRefRegex.Match(cellReference);
        if (!match.Success) return -1;
        var letters = match.Groups[1].Value;
        var index = 0;
        foreach (var ch in letters)
        {
            index *= 26;
            index += ch - 'A' + 1;
        }
        return index - 1;
    }

    private static int ParseInt(string? value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : 0;

    private static IEnumerable<string> SplitCsvLine(string line, char delimiter)
    {
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == delimiter && !inQuotes)
            {
                yield return sb.ToString();
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }
        yield return sb.ToString();
    }

    private static string BuildTitle(string path, string sheetName, IReadOnlyList<IReadOnlyList<string?>> rows)
    {
        var title = rows.SelectMany(r => r).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v) && v.Contains("IED", StringComparison.OrdinalIgnoreCase) && v.Contains("IP", StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(title) ? $"{Path.GetFileNameWithoutExtension(path)} · {sheetName}" : title.Trim();
    }

    private static uint ToSortKey(IPAddress address)
    {
        var b = address.GetAddressBytes();
        return b.Length == 4 ? ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3] : 0;
    }

    private sealed record SheetInfo(string Name, string Path);
}
