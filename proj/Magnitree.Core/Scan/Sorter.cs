namespace Magnitree.Core.Scan;

using Magnitree.Core.Path;
using Node = ITreeNode<Magnitree.Core.Path.IPathInfo>;

public class Sorter{
	//public Node Tree{get;set;} = new TreeNode<IPathInfo>();

	public Scanner Scanner{get;set;}//TODO


	public void EventHandler(obj? Sender, EvtArgScanProgress e){
		Task.Run(()=>{
			
		});
	}

	public nil Init(){
		Scanner.OnScanCompletedNode += EventHandler;
		return NIL;
	}


	public void Sort(){
		
	}
}
