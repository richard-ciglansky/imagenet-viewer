using System.Runtime.InteropServices.ComTypes;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;

namespace Importer;

class Program
{
    public const string InputStream = "ImageNetStructure.xml";
    public const string OutputStream = "ImageNetStructure.json";

    public readonly string NodeName = "synset";
    public readonly string NameAttribute = "words";

    private Dictionary<XElement, int> totalDescendantsCount = new();
    private JsonSerializerOptions options = new()
    {
        WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static void Main(string[] args)
    {
        new Program().Import(InputStream);
    }

    public void Import(string path)
    {
        XDocument structure = XDocument.Load(path);
        XElement? element = structure.Root;

        if (element is null)
            return;

        XElement? structureRoot = element.Element(NodeName);
        if (structureRoot is null)
            return;

        CountDescendantsCount(structureRoot);
        using ( StreamWriter writer = new StreamWriter(OutputStream, append: false))
        {
            int id = 0;
            writer.WriteLine("[");
            var(maxNameLength, maxTitleLength) = ExportElement(writer, structureRoot, ref id, 0, null);
            writer.WriteLine("]");

            Console.Out.WriteLine($"Max name length: {maxNameLength}");
            Console.Out.WriteLine($"Max title length: {maxTitleLength}");
        }
    }

    /// Counts and stores the total number of descendants for the specified XML element.
    /// This method recursively calculates the number of descendant elements named as the predefined node
    /// for the specified element and stores the result in the <see cref="totalDescendantsCount"/> dictionary.
    /// <param name="element">
    /// The XML element for which the descendants count is calculated.
    /// </param>
    /// <returns>
    /// The total number of descendants for the given element, including all levels of child elements.
    /// </returns>
    private int CountDescendantsCount(XElement element)
        => totalDescendantsCount[element] = element.Elements(NodeName).Sum(xElement => CountDescendantsCount(xElement) + 1);

    /// Exports the given XML element and its descendants to a JSON-like structure using the provided writer.
    /// This method recursively processes all child elements and writes structured data to the output stream,
    /// maintaining a name hierarchy and calculating the longest name within the hierarchy.
    /// <param name="writer">The StreamWriter instance used to write the exported data.</param>
    /// <param name="element">The XML element to be exported, along with its descendants.</param>
    /// <param name="prefix">
    /// An optional string representing the current name hierarchy prefix;
    /// if null, the name of the current element will be used as the root.
    /// </param>
    /// <returns>
    /// An integer representing the maximum length of the hierarchical name string encountered
    /// during the export process for the given element and its descendants.
    /// </returns>
    private (int MaxNameLength, int MaxTitleLength) ExportElement(StreamWriter writer, XElement element, ref int id, int level, int? parentId, string? prefix = null)
    {
        string? title = element.Attribute(NameAttribute)?.Value;
        string? nameString = MakePrefix(prefix, title);

        int thisId = id;
        int nameLength = nameString?.Length ?? 0;
        int titleLength = title?.Length ?? 0;
        writer.Write(FormatElement(element, nameString, title, id, level, parentId));
        writer.WriteLine(",");
        foreach (XElement child in element.Elements(NodeName))
        {
            id++;
            var (childNameLength, childTitleLength) = ExportElement(writer, child, ref id, level + 1, thisId, nameString);
            nameLength = Math.Max(nameLength, childNameLength);
            titleLength = Math.Max(titleLength, childTitleLength);
        }

        return (nameLength, titleLength);
    }

    /// Formats the specified XML element into a JSON-like string representation.
    /// This method converts the given element and its associated name into a serialized JSON object,
    /// including the name and the precomputed count of its descendants.
    /// <param name="element">
    /// The XML element to be serialized into the JSON-like representation.
    /// </param>
    /// <param name="name">
    /// An optional string representing the hierarchical name of the element; if null, the default name will be used.
    /// </param>
    /// <returns>
    /// A string containing the JSON-like representation of the provided XML element.
    /// </returns>
    private string FormatElement(XElement element, string? name, string title, int id, int level, int? parentId)
        => JsonSerializer.Serialize(new { name, title, size = totalDescendantsCount[element], id, level, parentId }, options);

    private string? MakePrefix(string? prefix, string name)
        => $"{(prefix is null ? name : PreppendPrefix(prefix, name))}";

    private string PreppendPrefix(string prefix, string value)
        => $"{prefix} > {value}";
}