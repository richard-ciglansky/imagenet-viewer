namespace ImageNetData;

public class ImageNetNode
{
    /// <summary>
    /// Unique identifier generated to preserve order of nodes.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Hierarchical name of the node, separated by ' > '.
    /// It is stored in the database despite redundancy to speed up searching in database.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Title of the node, trailing part of the <see cref="Name"/> property.
    /// It is stored explicitly despite the fact, that it can be derived from <see cref="Name"/> property, to simplify node handling.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Total number of descendants for the node.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Unique identifier of the parent node, or null if the node is a root.
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// Hierarchical depth level of the node.
    /// </summary>
    public int Level { get; set; }
};