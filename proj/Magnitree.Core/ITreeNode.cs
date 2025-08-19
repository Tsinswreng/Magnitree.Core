namespace Magnitree.Core;

public interface ITreeNode<T>{
	public ITreeNode<T>? Parent{get;set;}
	public IList<ITreeNode<T>> Children{get;set;}
	public T Data{get;set;}
	
}
