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

    private readonly Dictionary<XElement, int> totalDescendantsCount = new();
    private readonly JsonSerializerOptions options = new()
    {
        WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static void Main(string[] _)
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

        // count descendants number for each node recursively in O(n)
        CountDescendantsCount(structureRoot);

        // export all elements recursively in O(n)
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

    /// Exports the specified XML element and its descendants to a JSON-like structure using the given writer.
    /// This method recursively processes the element and its child elements to create a hierarchical representation
    /// of the data, including name and title information, while also calculating maximum name and title lengths encountered.
    /// <param name="writer">
    /// The StreamWriter instance that writes the exported data to an output stream.
    /// </param>
    /// <param name="element">
    /// The root XML element to be exported, along with all its child elements.
    /// </param>
    /// <param name="id">
    /// A reference to the current identifier, which will be incremented for each element processed recursively.
    /// </param>
    /// <param name="level">
    /// The current hierarchical depth level of the element in the structure.
    /// </param>
    /// <param name="parentId">
    /// The identifier of the parent element, or null if the element is at the top level.
    /// </param>
    /// <param name="prefix">
    /// An optional string representing the prefix for the hierarchical name; if null, the element's name will be used as the base.
    /// </param>
    /// <returns>
    /// A tuple containing two integers:
    /// 1. The maximum length of the hierarchical name encountered in the export process.
    /// 2. The maximum length of the title string encountered in the export process.
    /// </returns>
    private (int MaxNameLength, int MaxTitleLength) ExportElement(StreamWriter writer, XElement element, ref int id, int level, int? parentId, string? prefix = null)
    {
        // generate some title value when none is provided
        string title = element.Attribute(NameAttribute)?.Value ?? Guid.NewGuid().ToString();
        string nameString = MakePrefix(prefix, title);

        int thisId = id;
        int nameLength = nameString.Length;
        int titleLength = title.Length;
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

    /// Formats the specified XML element into a JSON-like serialized string representation.
    /// This method generates a JSON object for the provided XML element, including its name, title,
    /// descendant count, and related hierarchical metadata such as ID, level, and optional parent ID.
    /// <param name="element">
    /// The XML element to be formatted into a JSON-like structure.
    /// </param>
    /// <param name="name">
    /// The name of the element in the hierarchical structure, used as part of the JSON representation.
    /// This is the required field to be exported containing full path in the tree with nodes titles separated by ' > '.
    /// </param>
    /// <param name="title">
    /// The title of the XML element, the value of 'words' XML attribute in the original file.
    /// </param>
    /// <param name="id">
    /// The unique identifier of the element in the hierarchy generated as incremented integer number.
    /// The field is here to provide unique identification in database and to preserve the order for tree nodes.
    /// We could rely on natural order created by CLUSTERed index, but this is not guaranteed when using search filters in database,
    /// hence we are preserving tree nodes order explicitly.
    /// </param>
    /// <param name="level">
    /// The depth level of the element in the hierarchy, starting from 0 for the root.
    /// This field will help with tree navigation and grouping siblings tree nodes together, as these are not located one after another in general, just for leafs.
    /// </param>
    /// <param name="parentId">
    /// The unique identifier of the parent element, or null if the current element is the root.
    /// This field is not needed for current services, but it could be very usefull in the case of extending provided services without need to change data.
    /// </param>
    /// <returns>
    /// A JSON-like string representation of the specified XML element, containing its metadata and descendant information.
    /// </returns>
    private string FormatElement(XElement element, string? name, string title, int id, int level, int? parentId)
        => JsonSerializer.Serialize(new { name, title, size = totalDescendantsCount[element], id, level, parentId }, options);

    private string MakePrefix(string? prefix, string name)
        => $"{(prefix is null ? name : PreppendPrefix(prefix, name))}";

    private string PreppendPrefix(string prefix, string value)
        => $"{prefix} > {value}";
}