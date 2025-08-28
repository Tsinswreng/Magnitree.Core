namespace Magnitree.Core.Path;
using System.IO;

public static class ToolPath{
	public static str CombinePath(str Path1, str Path2){
		if(Path1.EndsWith('/')){
			return Path1+Path2;
		}
		return Path1+"/"+Path2;
	}

	public static str ToUnixPath(str Path, bool IsDir=false){
		var R = Path.Replace('\\', '/');
		if(IsDir){
			if(R.EndsWith('/')){
				
			}else{
				R = R+"/";
			}
		}
		return R;
	}

	public static IEnumerable<str?> Ls(string path){
		return Directory.EnumerateFileSystemEntries(path)
				.Select(Path.GetFileName);
	}

	public static IEnumerable<str?> LsAsy(string path){
		return Directory.EnumerateFileSystemEntries(path)
				.Select(Path.GetFileName);
	}


	public static IEnumerable<str> LsFullPath(str Path){
		var AbsUnixRoot = ToUnixPath(Path, true);
		var ls = Ls(AbsUnixRoot);
		foreach(var f in ls){
			if(f == null){
				continue;
			}
			yield return CombinePath(AbsUnixRoot, f);
		}
	}
}