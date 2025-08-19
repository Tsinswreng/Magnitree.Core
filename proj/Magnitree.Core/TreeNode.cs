namespace Magnitree.Core;

public struct TreeNode<T>: ITreeNode<T>{
	public TreeNode(){
		
	}
	public TreeNode(T Data){
		this.Data = Data;
	}
	public ITreeNode<T>? Parent{get;set;}
	public IList<ITreeNode<T>> Children{get;set;} = new List<ITreeNode<T>>();
	public T Data{get;set;} = default!;

}

public static class ExtnTreeNode{
	public static nil AddChild<T>(
		this ITreeNode<T> z, ITreeNode<T> child
	){
		z.Children.Add(child);
		child.Parent = z;
		return NIL;
	}
}