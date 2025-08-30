namespace Magnitree.Core.Scan;

public class TraverseInfo:IScanNodeExtraInfo{
	public u64 Depth{get;set;} = 0;
	public u64 FirstUnvisitedChildIdx{get;set;} = 0;
}

