namespace Magnitree.Core;

public interface ITreeNode<T>{
	public ITreeNode<T>? Parent{get;set;}
	public IList<ITreeNode<T>> Children{get;set;}
	public T Data{get;set;}
}

// public interface ITreeNode<TNode, TData>
// 	where TNode : ITreeNode<TNode, TData>
// {
// 	public TNode? Parent{get;set;}
// 	public IList<TNode> Children{get;set;}
// 	public TData Data{get;set;}
// }
