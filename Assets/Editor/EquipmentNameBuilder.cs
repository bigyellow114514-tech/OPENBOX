using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EquipmentNameBuilder
{
    const string XlsxPath  = "Assets/Excel/EuipmentIcon.xlsx";
    const int    NumSlots   = 12;
    const int    NumBands   = 8;

    // ItemName 列索引（0-based）：C=2, F=5, I=8, L=11, O=14, R=17, U=20, X=23
    // 规律：首列 2，步长 3
    static int ItemNameCol(int band) => 2 + band * 3;

    static readonly string[] SlotNames =
    {
        "武器","副手","头盔","胸甲","腿甲","靴子",
        "护手","腰带","项链","主戒","副戒","圣物"
    };

    [MenuItem("OpenBox/Build Equipment Names from Excel")]
    static void Build()
    {
        string fullPath = Path.GetFullPath(XlsxPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[EquipmentNameBuilder] 找不到 {XlsxPath}");
            return;
        }

        string[] names = ParseItemNames(fullPath);
        if (names == null) return;

        var sys = Object.FindObjectOfType<EquipmentDropSystem>();
        if (sys == null)
        {
            Debug.LogError("[EquipmentNameBuilder] 场景中找不到 EquipmentDropSystem");
            return;
        }

        var so   = new SerializedObject(sys);
        var prop = so.FindProperty("itemNameTable");
        prop.arraySize = names.Length;
        for (int i = 0; i < names.Length; i++)
            prop.GetArrayElementAtIndex(i).stringValue = names[i];
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[EquipmentNameBuilder] 完成！已写入 {names.Length} 条装备名称，请 Ctrl+S 保存场景。");
        Selection.activeGameObject = sys.gameObject;
    }

    static string[] ParseItemNames(string path)
    {
        var result = new string[NumSlots * NumBands];

        try
        {
            using var zip = ZipFile.OpenRead(path);

            // 读取共享字符串表
            var sharedStrings = ReadSharedStrings(zip);

            // 读取 Sheet1
            var sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
            if (sheetEntry == null)
            {
                Debug.LogError("[EquipmentNameBuilder] 找不到 sheet1.xml");
                return null;
            }

            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XDocument  doc;
            using (var stream = sheetEntry.Open())
                doc = XDocument.Load(stream);

            var rows = doc.Descendants(ns + "row").ToList();

            // 前 3 行是表头（中文标签、类型、字段名），数据从第 4 行开始
            for (int rowIdx = 3; rowIdx < rows.Count; rowIdx++)
            {
                int slot = rowIdx - 3;
                if (slot >= NumSlots) break;

                var cells = rows[rowIdx].Elements(ns + "c").ToList();

                for (int band = 0; band < NumBands; band++)
                {
                    string name = GetCellValue(cells, ItemNameCol(band), ns, sharedStrings);
                    // 如果表格中该格未填写，回退到槽位名
                    result[slot * NumBands + band] = string.IsNullOrWhiteSpace(name)
                        ? SlotNames[slot]
                        : name;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EquipmentNameBuilder] 解析 xlsx 时出错：{e.Message}");
            return null;
        }

        return result;
    }

    static List<string> ReadSharedStrings(ZipArchive zip)
    {
        var list  = new List<string>();
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return list;

        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument  doc;
        using (var stream = entry.Open())
            doc = XDocument.Load(stream);

        foreach (var si in doc.Descendants(ns + "si"))
            list.Add(string.Concat(si.Descendants(ns + "t").Select(t => t.Value)));

        return list;
    }

    // 按列索引（0-based）取单元格值
    static string GetCellValue(List<XElement> cells, int colIdx, XNamespace ns, List<string> sharedStrings)
    {
        foreach (var cell in cells)
        {
            string cellRef = (string)cell.Attribute("r") ?? "";
            if (CellColIndex(cellRef) != colIdx) continue;

            string t = (string)cell.Attribute("t") ?? "";
            var    v = cell.Element(ns + "v");
            if (v == null) return "";

            if (t == "s")
            {
                int idx = int.Parse(v.Value);
                return idx < sharedStrings.Count ? sharedStrings[idx] : "";
            }
            return v.Value;
        }
        return "";
    }

    // 将列字母转为 0-based 索引（A→0, B→1, Z→25, AA→26 …）
    static int CellColIndex(string cellRef)
    {
        int idx = 0;
        foreach (char c in cellRef)
        {
            if (!char.IsLetter(c)) break;
            idx = idx * 26 + (c - 'A' + 1);
        }
        return idx - 1;
    }
}
