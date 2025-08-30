namespace Magnitree.Core.Scan;
using Magnitree.Core.Path;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;

using Node = ScanNode<Magnitree.Core.Path.IPathInfo>;

public class EvtArgScanProgress: EventArgs{
	public Node? TreeNode{get;set;}
	public u64 Depth{get;set;}
}

public class EvtArgIgnorePath: EventArgs{
	public IPathInfo? PathInfo{get;set;}
}

public class EvtArgException:EventArgs{
	public Exception? Exception{get;set;}
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
	public event EventHandler<EvtArgIgnorePath>? OnIgnorePath;
	public event EventHandler<EvtArgException>? OnException;

	public CfgScanner Cfg{get;set;}

	public Node Tree{get;set;} = new ScanNode<IPathInfo>();

	public IPath StartPath{get;set;}
	#pragma warning disable CS8618
	protected Scanner(){}

	public static async Task<Scanner> MkAsy(CfgScanner Cfg, CT Ct){
		var R = new Scanner();
		R.Cfg = Cfg;
		R.StartPath = await PathInfo.MkAsy(Cfg.RootDir, Ct);
		if(!R.StartPath.IsDir()){
			throw new ArgumentException($"DirPath:{Cfg.RootDir} is not a directory.");
			// throw new ArgumentException($"DirPath:{Cfg.RootDir} is not a directory."
			// +"Only Normal Directory is supported, links are not supported."
			// );
		}
		return R;
	}

	bool TestIgnore(IPathInfo PathInfo){
		if(!( Cfg.IsIgnored?.Invoke(PathInfo)??false )){
			return false;
		}
		OnIgnorePath?.Invoke(this, new EvtArgIgnorePath{PathInfo = PathInfo});
		return true;
	}



	//Dictionary<Node, TraverseInfo> Node_TraverseInfo = new Dictionary<Node, TraverseInfo>();

	// 自建 專門ʹ數據結構、內置TraverseInfo 免查表
	//Dictionary<IPathInfo, TraverseInfo> Node_TraverseInfo = new Dictionary<IPathInfo, TraverseInfo>();

	//Stack<ITreeNode<IPathInfo>> Stack{get;set;} = new Stack<ITreeNode<IPathInfo>>();

	ConcurrentDictionary<IPathInfo, Node> CacheOfPathInfo_Node = new ConcurrentDictionary<IPathInfo, Node>();

	Node MkNode(IPathInfo PathInfo){
		return CacheOfPathInfo_Node.GetOrAdd(PathInfo, p => new ScanNode<IPathInfo>(p));
	}

	IScanNodeExtraInfo GetTraverseInfo(ITreeNode<IPathInfo> TreeNode){
		if(TreeNode is Node Node){
			return Node;
		}
		throw new Exception("TreeNode is not a ScanNode.");
	}


	IScanNodeExtraInfo GetTraverseInfo(Node Node){
		//return Node_TraverseInfo[Node.Data];
		return Node;
	}

	public Node? GetFirstUnvisitedChild(Node Node){
		var TraverseInfo = GetTraverseInfo(Node);
		//return TraverseInfo.FirstUnvisitedChild;
		var ChildIdx = (i32)TraverseInfo.FirstUnvisitedChildIdx;
		if(ChildIdx >= Node.Children.Count){
			return null;
		}
		var R = Node.Children[ChildIdx];
		return R as Node; //
	}

	void _OnScanCompletedNode(Node Node, IScanNodeExtraInfo CurTraverseInfo){
		OnScanCompletedNode?.Invoke(
			this
			,new EvtArgScanProgress{
				TreeNode = Node,
				Depth = CurTraverseInfo.Depth
			}
		);
	}

	void _OnScanCurrentNode(Node Node, IScanNodeExtraInfo CurTraverseInfo){
		OnScanCurrentNode?.Invoke(
			this
			,new EvtArgScanProgress{
				TreeNode = Node,
				Depth = CurTraverseInfo.Depth
			}
		);
	}
	void _OnException(Exception Ex){
		OnException?.Invoke(this, new EvtArgException{Exception = Ex});
	}

	/// <summary>
	/// 多线程并行扫描目录树
	/// </summary>
	public async Task<nil> Scan(CT Ct){
		var rootPathInfo = await PathInfo.MkAsy(StartPath.AbsPosixPath, Ct);
		var rootNode = MkNode(rootPathInfo);
		rootNode.Parent = null;
		Tree = rootNode;
		await ScanNodeParallel(rootNode, 0, Ct);
		return NIL;
	}

	private async Task ScanNodeParallel(Node node, u64 depth, CT Ct){
		if (TestIgnore(node.Data)) return;
		var traverseInfo = GetTraverseInfo(node);
		traverseInfo.Depth = depth;
		_OnScanCurrentNode(node, traverseInfo);

		if (node.Data.Type == EPathType.File){
			UpdParentSizeThreadSafe(node);
			_OnScanCompletedNode(node, traverseInfo);
			return;
		}

		if (node.Data.Type == EPathType.Dir && node.Children.Count == 0){
			List<string> children;
			try{
				children = ToolPath.LsFullPath(node.Data.AbsPosixPath).ToList();
			}catch(Exception e){
				_OnException(e);
				return;
			}
			if(children.Count == 0){
				lock(node){ node.Data.HasBytesSize = true; }
				_OnScanCompletedNode(node, traverseInfo);
				UpdParentSizeThreadSafe(node);
				return;
			}

			var tasks = new List<Task>();
			u64 idx = 0;
			foreach(var file in children){
				var pathInfo = await PathInfo.MkAsy(file, Ct);
				var childNode = MkNode(pathInfo);
				lock(node){ node.AddChild(childNode); }
				tasks.Add(ScanNodeParallel(childNode, depth + 1, Ct));
				idx++;
			}
			await Task.WhenAll(tasks);
			lock(node){ node.Data.HasBytesSize = true; }
			_OnScanCompletedNode(node, traverseInfo);
			UpdParentSizeThreadSafe(node);
			return;
		}
		// 已遍历过的目录（理论上不会到这里）
	}

	private void UpdParentSizeThreadSafe(Node node){
		if(node.Data.HasBytesSize && node.Parent != null){
			lock(node.Parent){
				node.Parent.Data.BytesSize += node.Data.BytesSize;
			}
		}
	}

	//TODO 多綫程並行
	public async Task<nil> ScanOld(CT Ct){
		var CurPathInfo = await PathInfo.MkAsy(StartPath.AbsPosixPath, Ct);
		var CurNode = MkNode(CurPathInfo);
		var Cur = CurNode;
		var i = 0;
		IScanNodeExtraInfo CurTraverseInfo = null!;

		void CompleteDir(){
			Cur.Data.HasBytesSize = true;
			_OnScanCompletedNode(Cur, CurTraverseInfo);
			UpdParentSize();
			Cur = Cur.Parent as Node;
		}

		void UpdParentSize(){
			if(Cur.Data.HasBytesSize){//當前ʹ節ʹ大小ˋ已算好則增ᵣ父點ʹ大小
				if(Cur.Parent != null){
					Cur.Parent.Data.BytesSize += Cur.Data.BytesSize;
				}
			}
		}

		Node? MoveToNextUnvisitedOrCompleteDir(){
			var UnvisitedChild = GetFirstUnvisitedChild(Cur);
			CurTraverseInfo.FirstUnvisitedChildIdx++;
			if(UnvisitedChild == null){//即該文件夾ʹ䀬ʹ孩ˋ皆已遍歷過
				CompleteDir();
				return null;
			}else{
				Cur = UnvisitedChild;
				return UnvisitedChild;
			}
		}



		for(;;i++){
			try{
				if(Cur == null){
					break;
				}
				if(TestIgnore(Cur.Data)){
					continue;
				}
				//System.Console.WriteLine(Cur.Data.AbsPosixPath);//t
				CurTraverseInfo = GetTraverseInfo(Cur);

				//設 當前ʹ節ʹ深度潙父節ʹ深度 +1
				if(Cur.Parent != null){
					var ParentTraverseInfo = GetTraverseInfo(Cur.Parent);
					CurTraverseInfo.Depth = ParentTraverseInfo.Depth + 1;
				}

				_OnScanCurrentNode(Cur, CurTraverseInfo);

				if(Cur.Data.Type == EPathType.File){//當前ʹ節潙葉節(文件)
					UpdParentSize();
					_OnScanCompletedNode(Cur, CurTraverseInfo);
					Cur = Cur.Parent as Node;
					continue;
				}

				//當前節點潙文件夾且未初始化ᵣ諸孩
				if(Cur.Data.Type == EPathType.Dir && Cur.Children.Count == 0){
					var Children = ToolPath.LsFullPath(Cur.Data.AbsPosixPath);
					u64 fileIdx = 0;
					//CurTraverseInfo.FirstUnvisitedChildIdx = 0;//
					foreach(var file in Children){
						var pathInfo = await PathInfo.MkAsy(file, Ct);
						var Node = MkNode(pathInfo);
						Cur.AddChild(Node);
						fileIdx++;
					}
					if(fileIdx == 0){//空文件夾
						CompleteDir();
						continue;
					}else{//非空文件夾 初次訪問
						var NextChildToVisit = GetFirstUnvisitedChild(Cur);
						CurTraverseInfo.FirstUnvisitedChildIdx++;
						if(NextChildToVisit == null){ //不應入斯支
							throw new Exception("不應入斯支");
						}else{
							Cur = NextChildToVisit;
							continue;
						}
					}
				}else{//當前節點潙已遍歷過之文件夾、即回溯ʃ至
					//則取下個孩芝未遍歷者
					MoveToNextUnvisitedOrCompleteDir();
					continue;
				}
			}catch (System.Exception e){
				_OnException(e);
				if(Cur == null){break;}
				if(Cur.Data.Type == EPathType.File){
					Cur = Cur.Parent as Node; continue;
				}
				MoveToNextUnvisitedOrCompleteDir();
				continue;
			}
		}//~for(;;)
		return NIL;
	}

}
