using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

public sealed class ExcelTable
{
    public IReadOnlyList<ExcelRow> Rows => _rows;

    readonly List<ExcelRow> _rows;

    ExcelTable(List<ExcelRow> rows)
    {
        _rows = rows;
    }

    public static ExcelTable Load(string path, string worksheetPath = "xl/worksheets/sheet1.xml")
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var zip = new ZipArchive(file, ZipArchiveMode.Read);
        List<string> sharedStrings = ReadSharedStrings(zip);

        var sheetEntry = zip.GetEntry(worksheetPath);
        if (sheetEntry == null)
            throw new FileNotFoundException(worksheetPath);

        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument doc;
        using (var stream = sheetEntry.Open())
            doc = XDocument.Load(stream);

        var rows = new List<ExcelRow>();
        foreach (var row in doc.Descendants(ns + "row"))
        {
            var values = new Dictionary<int, string>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                int col = CellColIndex((string)cell.Attribute("r") ?? "");
                if (col < 0) continue;

                values[col] = GetCellValue(cell, ns, sharedStrings);
            }
            rows.Add(new ExcelRow(values));
        }

        return new ExcelTable(rows);
    }

    public string GetValue(int rowIndex, int colIndex)
    {
        return rowIndex >= 0 && rowIndex < _rows.Count
            ? _rows[rowIndex].Get(colIndex)
            : "";
    }

    public Dictionary<string, int> ReadHeader(int rowIndex)
    {
        var header = new Dictionary<string, int>();
        if (rowIndex < 0 || rowIndex >= _rows.Count) return header;

        foreach (var pair in _rows[rowIndex].Cells)
        {
            string key = pair.Value;
            if (!string.IsNullOrWhiteSpace(key))
                header[key.Trim()] = pair.Key;
        }

        return header;
    }

    static List<string> ReadSharedStrings(ZipArchive zip)
    {
        var list = new List<string>();
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return list;

        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument doc;
        using (var stream = entry.Open())
            doc = XDocument.Load(stream);

        foreach (var si in doc.Descendants(ns + "si"))
            list.Add(string.Concat(si.Descendants(ns + "t").Select(t => t.Value)));

        return list;
    }

    static string GetCellValue(XElement cell, XNamespace ns, List<string> sharedStrings)
    {
        string type = (string)cell.Attribute("t") ?? "";

        if (type == "inlineStr")
            return string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));

        var value = cell.Element(ns + "v");
        if (value == null) return "";

        if (type == "s" && int.TryParse(value.Value, out int sharedIndex))
            return sharedIndex >= 0 && sharedIndex < sharedStrings.Count ? sharedStrings[sharedIndex] : "";

        return value.Value;
    }

    static int CellColIndex(string cellRef)
    {
        int idx = 0;
        foreach (char c in cellRef)
        {
            if (!char.IsLetter(c)) break;
            idx = idx * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
        }
        return idx - 1;
    }
}

public sealed class ExcelRow
{
    public IReadOnlyDictionary<int, string> Cells => _cells;

    readonly Dictionary<int, string> _cells;

    public ExcelRow(Dictionary<int, string> cells)
    {
        _cells = cells;
    }

    public string Get(int colIndex)
    {
        return _cells.TryGetValue(colIndex, out string value) ? value : "";
    }

    public string Get(Dictionary<string, int> columns, string key)
    {
        return columns.TryGetValue(key, out int col) ? Get(col) : "";
    }

    public float GetFloat(Dictionary<string, int> columns, string key, float defaultValue = 0f)
    {
        string raw = Get(columns, key);
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : defaultValue;
    }

    public int GetInt(Dictionary<string, int> columns, string key, int defaultValue = 0)
    {
        string raw = Get(columns, key);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : defaultValue;
    }
}
