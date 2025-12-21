namespace ImageNetData;

public class ImageNetNode
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Title { get; set; }
    public int Size { get; set; }
    public int? ParentId { get; set; }
    public int Level { get; set; }
};