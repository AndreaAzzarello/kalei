// Smoke test of every 1.0 library. It runs on the TRIMMED binary: trimming can break reflection without any build warning,
// so a library only passes if it also works here. Output: one line per library, PASS or FAIL with the exception.
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using nietras.SeparatedValues;

var failures = 0;
void Check(string name, Func<string> body)
{
    try { Console.WriteLine($"PASS {name}: {body()}"); }
    catch (Exception e) { failures++; Console.WriteLine($"FAIL {name}: {e.GetType().Name}: {e.Message.Split('\n')[0]}"); }
}

Check("NetVips", () =>
{
    using var img = NetVips.Image.Black(64, 64).NewFromImage(100, 150, 200);
    var avif = img.WriteToBuffer(".avif");
    using var back = NetVips.Image.NewFromBuffer(avif);
    return $"AVIF {avif.Length} bytes, read back {back.Width}x{back.Height}, avg {back.Avg():F0}";
});

Check("Sep", () =>
{
    using var reader = Sep.Reader().FromText("a,b\n1,x\n2,y\n");
    var rows = 0; var last = "";
    foreach (var row in reader) { rows++; last = row["b"].ToString(); }
    using var writer = reader.Spec.Writer().ToText();
    using (var row = writer.NewRow()) { row["a"].Set("1"); row["b"].Set("z"); }
    return $"read {rows} rows (last {last}), wrote {writer.ToString().Trim().Replace("\n", "|")}";
});

Check("CsvHelper", () =>
{
    using var csv = CsvHelper.CsvReader.Create(new StringReader("a,b\n1,x\n2,y\n"));
    var idx = csv.GetFieldIndex("b"); // reads the header
    var rows = 0; var last = "";
    while (csv.Read()) { rows++; last = csv.GetString(idx); }
    var sw = new StringWriter();
    using (var w = CsvHelper.CsvWriter.Create(sw)) { w.WriteField("a"); w.WriteField("b"); w.EndRecord(); }
    return $"read {rows} rows (last {last}), wrote {sw.ToString().Trim()}";
});

Check("System.Text.Json", () =>
{
    using var doc = JsonDocument.Parse("""[{"a":1,"b":"x"},{"a":2,"b":"y"}]""");
    var ms = new MemoryStream();
    using (var w = new Utf8JsonWriter(ms)) { w.WriteStartArray(); foreach (var e in doc.RootElement.EnumerateArray()) e.WriteTo(w); w.WriteEndArray(); }
    var typed = JsonSerializer.Serialize(new Dictionary<string, int> { ["k"] = 1 }, SmokeJson.Default.DictionaryStringInt32);
    return $"{doc.RootElement.GetArrayLength()} objects, rewrote {ms.Length} bytes, source-generated {typed}";
});

Check("System.Xml", () =>
{
    var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
    using var r = XmlReader.Create(new StringReader("<r><i a='1'>x</i></r>"), settings);
    var doc = XDocument.Load(r);
    var blocked = false;
    try { XmlReader.Create(new StringReader("<!DOCTYPE r [<!ENTITY e 'x'>]><r>&e;</r>"), settings).Read(); } catch (XmlException) { blocked = true; }
    return $"{doc.Root!.Elements().Count()} element, DTD refused: {blocked}";
});

Check("YamlDotNet (YamlStream, no type reflection)", () =>
{
    var ys = new YamlDotNet.RepresentationModel.YamlStream();
    ys.Load(new StringReader("a: 1\nb:\n  - x\n  - y\n"));
    var root = (YamlDotNet.RepresentationModel.YamlMappingNode)ys.Documents[0].RootNode;
    var outDoc = new YamlDotNet.RepresentationModel.YamlStream(new YamlDotNet.RepresentationModel.YamlDocument(
        new YamlDotNet.RepresentationModel.YamlMappingNode(new YamlDotNet.RepresentationModel.YamlScalarNode("k"), new YamlDotNet.RepresentationModel.YamlScalarNode("v"))));
    var sw = new StringWriter();
    outDoc.Save(sw, false);
    return $"{root.Children.Count} keys read, wrote {sw.ToString().Trim().Split('\n')[0]}";
});

Check("XLSX: own minimal writer + Sylvan.Data.Excel reader", () =>
{
    // Minimal writer: every text is an explicit string cell (S9), numbers are plain numbers, no formulas ever.
    static byte[] WriteXlsx(string[][] rows)
    {
        var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            void Entry(string name, string xml) { using var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); w.Write(xml); }
            Entry("[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Entry("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Entry("xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Entry("xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            foreach (var row in rows)
            {
                sb.Append("<row>");
                foreach (var cell in row) sb.Append("<c t=\"inlineStr\"><is><t>").Append(System.Security.SecurityElement.Escape(cell)).Append("</t></is></c>");
                sb.Append("</row>");
            }
            Entry("xl/worksheets/sheet1.xml", sb.Append("</sheetData></worksheet>").ToString());
        }
        return ms.ToArray();
    }
    var bytes = WriteXlsx([["a", "b"], ["=1+1", "x"], ["2", "y"]]);
    using var edr = Sylvan.Data.Excel.ExcelDataReader.Create(new MemoryStream(bytes), Sylvan.Data.Excel.ExcelWorkbookType.ExcelXml);
    var cells = 0; var formulaLike = "";
    while (edr.Read()) { cells += edr.FieldCount; if (edr.GetString(0).StartsWith('=')) formulaLike = edr.GetString(0); }
    return $"XLSX {bytes.Length} bytes written by hand, read {cells} cells; cell stayed text: '{formulaLike}'";
});

Check("Markdig", () => Markdig.Markdown.ToHtml("# Title\n\n*a* **b**").Trim().Replace("\n", ""));

Check("ReverseMarkdown", () => new ReverseMarkdown.Converter().Convert("<h1>Title</h1><p><b>x</b></p>").Trim().Replace("\n", "|"));

Check("HtmlSanitizer", () => new Ganss.Xss.HtmlSanitizer().Sanitize("<p onclick='x()'>hi</p><script>alert(1)</script>"));

Console.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILED");
return failures;

[System.Text.Json.Serialization.JsonSerializable(typeof(Dictionary<string, int>))]
internal partial class SmokeJson : System.Text.Json.Serialization.JsonSerializerContext { }
