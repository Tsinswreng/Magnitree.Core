namespace Magnitree.Core.Path;

using Tsinswreng.Tempus;

public interface IPathInfo:IPath{
	public bool HasSizeBytes{get;set;}
	public u64 SizeBytes{get;set;}
	public Tempus CreatedAt{get;}
	public Tempus ModifiedAt{get;}

}
