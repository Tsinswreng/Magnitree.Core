namespace Magnitree.Core.Path;

using Tsinswreng.Tempus;
using System.IO;

public class PathInfo: IPathInfo{
	public bool HasSizeBytes{get;set;}
	public u64 SizeBytes{get;set;}
	public Tempus CreatedAt{get;}
	public Tempus ModifiedAt{get;}
	public str AbsPosixPath{get;}
	public EPathType Type{get;}

	public PathInfo(string path){
		FileSystemInfo info;

		if(File.Exists(path)){
			info = new FileInfo(path);
			AbsPosixPath = ToolPath.ToUnixPath(path, false);
		}else if(Directory.Exists(path)){
			info = new DirectoryInfo(path);
			AbsPosixPath = ToolPath.ToUnixPath(path, true);
		}else{
			throw new FileNotFoundException($"Path not found: {path}");
		}

		info.Refresh();
		
		TryGetPathType(path, out var t);
		Type = t;

		if (info is FileInfo fi){
			HasSizeBytes = true;
			SizeBytes = (u64)fi.Length;
			CreatedAt = Tempus.FromDateTime(fi.CreationTimeUtc);
			ModifiedAt = Tempus.FromDateTime(fi.LastWriteTimeUtc);
		}
		else if (info is DirectoryInfo di){
			HasSizeBytes = false;
			SizeBytes = 0; // 目录大小可自定义统计
			CreatedAt = Tempus.FromDateTime(di.CreationTimeUtc);
			ModifiedAt = Tempus.FromDateTime(di.LastWriteTimeUtc);
		}
		else{
			throw new NotSupportedException($"Unsupported path type: {path}");
			// HasSizeBytes = false;
			// SizeBytes = 0;
			// CreatedAt = new Tempus();
			// ModifiedAt = new Tempus();
		}
	}


	


	public static bool TryGetPathType(string path, out EPathType R){
		R = default;
		if (!File.Exists(path) && !Directory.Exists(path)){
			return false;
		}
		// 统一用 FileInfo，因为它既能代表文件也能代表目录
		FileSystemInfo info = new FileInfo(path);
		info.Refresh();

		var attr = info.Attributes;

		// 1. 快捷方式(.lnk)——仅 Windows
		if (OperatingSystem.IsWindows() && path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)){
			R = EPathType.ShortcutLnk;
			return true;
		}
		// 2. 符号链接 / 目录联接
		if (attr.HasFlag(FileAttributes.ReparsePoint)){
			// “目录” 位 + 符号链接位 → 目录符号链接
			if (attr.HasFlag(FileAttributes.Directory)){
				R = EPathType.DirSymlink;
				return true;
			}else{
				R = EPathType.FileSymlink;
				return true;
			}
		}

		// 3. 目录联接（Windows 特有，也是 ReparsePoint 但类型不同）
		//    通过 FileSystemInfo.ResolveLinkTarget 可以进一步判断，但
		//    对应用层来说与 DirectorySymlink 行为相同，可合并或细分
		//    这里示例：若需要区分，可调用 Win32 API GetReparsePointTag

		// 4. 普通目录
		if (attr.HasFlag(FileAttributes.Directory)){
			R =  EPathType.Dir;
			return true;
		}

		// 5. 普通文件（可能是硬链接，但 .NET 无法直接判断硬链接个数）
		R = EPathType.File;
		return true;
	}

	/// <summary>
	/// 返回指定目录下的直接子级：文件和目录的名字（不含全路径）。
	/// </summary>
	// public static (IReadOnlyList<string> Files, IReadOnlyList<string> Directories) GetImmediateChildren(string path){
	// 	// 文件
	// 	var files = Directory.EnumerateFiles(path)
	// 						.Select(Path.GetFileName)
	// 						.ToArray();

	// 	// 目录
	// 	var dirs = Directory.EnumerateDirectories(path)
	// 						.Select(Path.GetFileName)
	// 						.ToArray();

	// 	return (files, dirs);
	// }


}


public static class ExtnPath{
	public static bool IsDir(this IPathInfo z){
		return z.Type == EPathType.Dir 
		|| z.Type == EPathType.DirSymlink 
		|| z.Type == EPathType.DirJunction;
	}
}