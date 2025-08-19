namespace Magnitree.Core.Scan;
using Magnitree.Core.Path;
using System.Collections;
using System.IO;

using Node = ITreeNode<Magnitree.Core.Path.IPathInfo>;

public class EvtArgScanProgress: EventArgs{
	//public str? Path{get;set;}
	public Node? TreeNode{get;set;}
	public u64 Depth{get;set;}
}

public class TraverseInfo{
	public u64 Depth{get;set;} = 0;
	[Obsolete]
	public Node? FirstUnvisitedChild{get;set;}
	public u64 FirstUnvisitedChildIdx{get;set;} = 0;
}

public class Scanner{
	/// <summary>
	/// ʃ入ʹ參ˋ循環中當前遍歷ʹ節點。緣遍歷旹有回溯、節點蜮褈
	/// </summary>
	public event EventHandler<EvtArgScanProgress>? OnScanCurrentNode;
	/// <summary>
	/// ʃ入ʹ參ˋ已算好大小之節點。節點不褈
	/// </summary>
	public event EventHandler<EvtArgScanProgress>? OnScanCompletedNode;
	public Node Tree{get;set;} = new TreeNode<IPathInfo>();

	public IPath StartPath{get;set;}

	Dictionary<Node, TraverseInfo> Node_TraverseInfo = new Dictionary<Node, TraverseInfo>();

	//Stack<ITreeNode<IPathInfo>> Stack{get;set;} = new Stack<ITreeNode<IPathInfo>>();

	Node MkNode(IPathInfo PathInfo){
		var R = new TreeNode<IPathInfo>(PathInfo);
		Node_TraverseInfo[R] = new TraverseInfo();
		return R;
	}

	TraverseInfo GetTraverseInfo(Node Node){
		return Node_TraverseInfo[Node];
	}

	public Node? GetFirstUnvisitedChild(Node Node){
		// foreach(var Child in Node.Children){
		// 	var TraverseInfo = GetTraverseInfo(Child);
		// 	if(!TraverseInfo.IsVisited){
		// 		return Child;
		// 	}
		// }
		// return null;

		var TraverseInfo = GetTraverseInfo(Node);
		//return TraverseInfo.FirstUnvisitedChild;
		var ChildIdx = (i32)TraverseInfo.FirstUnvisitedChildIdx;
		if(ChildIdx >= Node.Children.Count){
			return null;
		}
		var R = Node.Children[ChildIdx];
		return R;
	}

	void _OnScanCompletedNode(Node Node, TraverseInfo CurTraverseInfo){
		OnScanCompletedNode?.Invoke(
			this
			,new EvtArgScanProgress{
				TreeNode = Node,
				Depth = CurTraverseInfo.Depth
			}
		);
	}

	void _OnScanCurrentNode(Node Node, TraverseInfo CurTraverseInfo){
		OnScanCurrentNode?.Invoke(
			this
			,new EvtArgScanProgress{
				TreeNode = Node,
				Depth = CurTraverseInfo.Depth
			}
		);
	}

	public void Scan(){
		var CurNode = MkNode(new PathInfo(StartPath.AbsPosixPath));
		var Cur = CurNode;
		for(;;){
			var CurTraverseInfo = GetTraverseInfo(Cur);
			//CurTraverseInfo.IsVisited = true;
			//設 當前ʹ節ʹ深度潙父節ʹ深度 +1
			if(Cur.Parent != null){
				var ParentTraverseInfo = GetTraverseInfo(Cur.Parent);
				CurTraverseInfo.Depth = ParentTraverseInfo.Depth + 1;
			}
			
			_OnScanCurrentNode(Cur, CurTraverseInfo);
			
			if(Cur.Data.HasSizeBytes){
				if(Cur.Parent != null){
					Cur.Parent.Data.SizeBytes += Cur.Data.SizeBytes;
				}
			}

			if(Cur.Data.Type == EPathType.File){//當前ʹ節潙葉節(文件)
				_OnScanCompletedNode(Cur, CurTraverseInfo);
				Cur = Cur.Parent;
				if(Cur == null){break;}
				continue;
			}
			
			//當前節點潙文件夾且未初始化ᵣ諸孩
			if(Cur.Data.Type == EPathType.Dir && Cur.Children.Count == 0){
				var Children = ToolPath.LsFullPath(Cur.Data.AbsPosixPath);
				u64 fileIdx = 0;
				foreach(var file in Children){
					var PathInfo = new PathInfo(file);
					var Node = MkNode(PathInfo);
					CurTraverseInfo.FirstUnvisitedChildIdx = fileIdx;
					Cur.AddChild(Node);
					fileIdx++;
				}
				if(fileIdx == 0){//空文件夾
					Cur.Data.SizeBytes = 0;
					Cur.Data.HasSizeBytes = true;
					_OnScanCompletedNode(Cur, CurTraverseInfo);
					Cur = Cur.Parent;
					if(Cur == null){break;}
					continue;
				}else{//非空文件夾
					Cur = GetFirstUnvisitedChild(Cur);
					CurTraverseInfo.FirstUnvisitedChildIdx++;
					if(Cur == null){break;}
				}
			}else{//當前節點潙已遍歷過之文件夾、即回溯ʃ至
				//則取下個孩芝未遍歷者
				var UnvisitedChild = GetFirstUnvisitedChild(Cur);
				CurTraverseInfo.FirstUnvisitedChildIdx++;
				if(UnvisitedChild == null){//即該文件夾ʹ䀬ʹ孩ˋ皆已遍歷過
					Cur.Data.HasSizeBytes = true;
					_OnScanCompletedNode(Cur, CurTraverseInfo);
					Cur = Cur.Parent;
					if(Cur == null){break;}
				}else{
					Cur = UnvisitedChild;
				}
			}
		}
	}
	
	// public void Scan(){
	// 	var CurNode = MkNode(new PathInfo(StartPath.AbsPosixPath));
	// 	Stack.Push(CurNode);
	// 	for(;Stack.Count > 0;){
	// 		var Cur = Stack.Pop();
	// 		OnScan?.Invoke(this, new EvtArgScanProgress{Path=Cur.Data.AbsPosixPath});
	// 		if(Cur.Data.Type != EPathType.Dir){
	// 			continue;
	// 		}
	// 		var Children = ToolPath.LsFullPath(Cur.Data.AbsPosixPath);
			
	// 		var fileIdx = 0;
	// 		foreach(var file in Children){
	// 			var PathInfo = new PathInfo(file);
	// 			var Node = MkNode(PathInfo);
	// 			CurNode.AddChild(Node);
	// 			if(PathInfo.Type == EPathType.Dir){
	// 				Stack.Push(Node);
	// 			}else{//葉節
	// 				Node.Parent!.Data.SizeBytes += PathInfo.SizeBytes;
	// 			}
	// 			fileIdx++;
	// 		}
	// 		if(fileIdx == 0){
	// 			Cur.Data.SizeBytes = 0;
	// 			Cur.Data.HasSizeBytes = true;
	// 		}
	// 	}
	// 	Tree = CurNode;
	// }
}
/* 
Scan時邊遍歷樹邊算大小
如TreeSize 于視圖中實時ᵈ顯 當前既算好之部分
 */