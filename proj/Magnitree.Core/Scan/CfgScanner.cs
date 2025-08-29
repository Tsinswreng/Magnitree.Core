using Magnitree.Core.Path;

namespace Magnitree.Core.Scan;

public class CfgScanner{
	public str RootDir{get;set;} = "";
	public Func<IPathInfo, bool>? IsIgnored = _ => false;
}

