namespace Snuggle.glTF;

public interface INodeCreator {
    // ReSharper disable once UnusedMemberInSuper.Global
    public (Node Node, int Id) CreateNode(Root root);
}
