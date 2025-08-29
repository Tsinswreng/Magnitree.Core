namespace Magnitree.Core.Scan;
using Magnitree.Core.Path;
using System.Collections;
using System.IO;

using Node = ITreeNode<Magnitree.Core.Path.IPathInfo>;

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

	public Node Tree{get;set;} = new TreeNode<IPathInfo>();

	public IPath StartPath{get;set;}
	#pragma warning disable CS8618
	protected Scanner(){}

	public static async Task<Scanner> MkAsy(CfgScanner Cfg, CT Ct){
		var R = new Scanner();
		R.Cfg = Cfg;
		R.StartPath = await PathInfo.MkAsy(Cfg.RootDir, Ct);
		if(R.StartPath.Type != EPathType.Dir){
			throw new ArgumentException($"DirPath:{Cfg.RootDir} is not a directory."
			+"Only Normal Directory is supported, links are not supported."
			);
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

	Dictionary<IPathInfo, TraverseInfo> Node_TraverseInfo = new Dictionary<IPathInfo, TraverseInfo>();

	//Stack<ITreeNode<IPathInfo>> Stack{get;set;} = new Stack<ITreeNode<IPathInfo>>();

	Node MkNode(IPathInfo PathInfo){
		var R = new TreeNode<IPathInfo>(PathInfo);
		//Node_TraverseInfo[R] = new TraverseInfo();
		Node_TraverseInfo[PathInfo] = new TraverseInfo();
		return R;
	}

	TraverseInfo GetTraverseInfo(Node Node){
		return Node_TraverseInfo[Node.Data];
	}

	public Node? GetFirstUnvisitedChild(Node Node){
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
	void _OnException(Exception Ex){
		OnException?.Invoke(this, new EvtArgException{Exception = Ex});
	}

	//TODO 多綫程並行
	public async Task<nil> Scan(CT Ct){
		var CurPathInfo = await PathInfo.MkAsy(StartPath.AbsPosixPath, Ct);
		var CurNode = MkNode(CurPathInfo);
		var Cur = CurNode;
		var i = 0;
		TraverseInfo CurTraverseInfo;

		void CompleteDir(){
			Cur.Data.HasBytesSize = true;
			_OnScanCompletedNode(Cur, CurTraverseInfo);
			UpdParentSize();
			Cur = Cur.Parent;
		}

		void UpdParentSize(){
			if(Cur.Data.HasBytesSize){//當前ʹ節ʹ大小ˋ已算好則增ᵣ父點ʹ大小
				if(Cur.Parent != null){
					Cur.Parent.Data.BytesSize += Cur.Data.BytesSize;
				}
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
					Cur = Cur.Parent;
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
					var UnvisitedChild = GetFirstUnvisitedChild(Cur);
					CurTraverseInfo.FirstUnvisitedChildIdx++;
					if(UnvisitedChild == null){//即該文件夾ʹ䀬ʹ孩ˋ皆已遍歷過
						CompleteDir();
						continue;
					}else{
						Cur = UnvisitedChild;
						continue;
					}
				}
			}catch (System.Exception e){
				_OnException(e);
			}
		}//~for(;;)
		return NIL;
	}

}
