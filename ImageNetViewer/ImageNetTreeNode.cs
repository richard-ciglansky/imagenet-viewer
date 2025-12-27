namespace ImageNetViewer;

public class ImageNetTreeNode
{
    public string Name { get; set; }
    public int Size { get; set; }
    public ImageNetTreeNode[] Children { get; set; }
}