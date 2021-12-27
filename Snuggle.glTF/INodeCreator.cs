namespace Snuggle.glTF;

public interface INodeCreator {
    public (Node Node, int Id) CreateNode(Root root);
}
