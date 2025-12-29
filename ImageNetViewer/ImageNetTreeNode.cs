namespace ImageNetViewer;

public class ImageNetTreeNode
{
    public required string Name { get; set; }
    public int Size { get; set; } = 0;
    public ImageNetTreeNode[]? Children { get; set; }
}