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

Check("YamlDotNet", () =>
{
    var obj = new YamlDotNet.Serialization.DeserializerBuilder().Build().Deserialize<object>("a: 1\nb:\n  - x\n  - y\n");
    var text = new YamlDotNet.Serialization.SerializerBuilder().Build().Serialize(new Dictionary<string, object> { ["k"] = "v" });
    return $"{obj.GetType().Name}, wrote {text.Trim()}";
});

Check("MiniExcel", () =>
{
    var rows = new List<Dictionary<string, object>> { new() { ["a"] = 1, ["b"] = "x" }, new() { ["a"] = 2, ["b"] = "y" } };
    var ms = new MemoryStream();
    MiniExcelLibs.MiniExcel.SaveAs(ms, rows);
    ms.Position = 0;
    var n = 0;
    foreach (IDictionary<string, object> row in MiniExcelLibs.MiniExcel.Query(ms, useHeaderRow: true)) n += row.Count;
    return $"XLSX {ms.Length} bytes, read {n} cells";
});

Check("Markdig", () => Markdig.Markdown.ToHtml("# Title\n\n*a* **b**").Trim().Replace("\n", ""));

Check("ReverseMarkdown", () => new ReverseMarkdown.Converter().Convert("<h1>Title</h1><p><b>x</b></p>").Trim().Replace("\n", "|"));

Check("HtmlSanitizer", () => new Ganss.Xss.HtmlSanitizer().Sanitize("<p onclick='x()'>hi</p><script>alert(1)</script>"));

Console.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILED");
return failures;

[System.Text.Json.Serialization.JsonSerializable(typeof(Dictionary<string, int>))]
internal partial class SmokeJson : System.Text.Json.Serialization.JsonSerializerContext { }
