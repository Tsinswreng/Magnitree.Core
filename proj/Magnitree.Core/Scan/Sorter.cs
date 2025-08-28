namespace Magnitree.Core.Scan;

using Magnitree.Core.Path;
using Node = ITreeNode<Magnitree.Core.Path.IPathInfo>;

public class Sorter{
	//public Node Tree{get;set;} = new TreeNode<IPathInfo>();

	public Scanner Scanner{get;set;}//TODO

	public IList<Node> ResultNodes{get;set;} = new List<Node>();
	public void OnScanCompletedNode(obj? Sender, EvtArgScanProgress e){
		Task.Run(()=>{
			if(e.TreeNode!= null){
				ResultNodes.Add(e.TreeNode);
			}
		});
	}

	public nil Init(){
		Scanner.OnScanCompletedNode += OnScanCompletedNode;
		return NIL;
	}

	public void Sort(){

	}
}
