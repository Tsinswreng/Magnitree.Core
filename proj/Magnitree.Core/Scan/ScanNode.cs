using Magnitree.Core.Path;

namespace Magnitree.Core.Scan;




// public struct ScanPathInfo:IPathInfo{

// }

// public struct ScanNode:ITreeNode{

// }

public class ScanNode<T>
	:TreeNode<T>
	,IScanNodeExtraInfo
{
	public ScanNode(){}
	public ScanNode(T Data):base(Data){}
	public u64 Depth{get;set;}
	public u64 FirstUnvisitedChildIdx{get;set;}
}
