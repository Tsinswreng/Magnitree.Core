namespace Magnitree.Core.Scan;

public interface IScanNodeExtraInfo{
	public u64 Depth{get;set;}
	public u64 FirstUnvisitedChildIdx{get;set;}
}
