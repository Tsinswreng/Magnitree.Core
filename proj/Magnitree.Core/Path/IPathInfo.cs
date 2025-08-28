namespace Magnitree.Core.Path;

using Tsinswreng.Tempus;

public interface IPathInfo:IPath{
	public bool HasBytesSize{get;set;}
	public u64 BytesSize{get;set;}
	public bool HasBytesSizeOnDisk{get;set;}
	public u64 BytesSizeOnDisk{get;set;}
	public Tempus CreatedAt{get;}
	public Tempus ModifiedAt{get;}
}
