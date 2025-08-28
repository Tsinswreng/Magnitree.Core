namespace Magnitree.Core.Path;

using Tsinswreng.Tempus;
using System.IO;

public class PathInfo: IPathInfo{
	public bool HasBytesSize{get;set;}
	public u64 BytesSize{get;set;}

	public bool HasBytesSizeOnDisk{get;set;}
	public u64 BytesSizeOnDisk{get;set;}
	public Tempus CreatedAt{get;set;}
	public Tempus ModifiedAt{get;set;}
	public str AbsPosixPath{get;set;}
	public EPathType Type{get;set;}

	public override bool Equals(object? obj) {
		if(obj is not IPathInfo other){
			return false;
		}
		return this.AbsPosixPath == other.AbsPosixPath;
	}

	public override int GetHashCode() {
		return this.AbsPosixPath.GetHashCode();
	}

	// public PathInfo(string path){

	// }

	public static async Task<PathInfo> MkAsy(str path, CT Ct){
		var R = new PathInfo();
		FileSystemInfo info;
		if(File.Exists(path)){
			info = new FileInfo(path);
			R.AbsPosixPath = ToolPath.ToUnixPath(path, false);
		}else if(Directory.Exists(path)){
			info = new DirectoryInfo(path);
			R.AbsPosixPath = ToolPath.ToUnixPath(path, true);
		}else{
			throw new FileNotFoundException($"Path not found: {path}");
		}

		info.Refresh();

		TryGetPathType(path, out var t);
		R.Type = t;

		if (info is FileInfo fi){
			R.HasBytesSize = true;
			R.BytesSize = (u64)fi.Length;
			R.CreatedAt = Tempus.FromDateTime(fi.CreationTimeUtc);
			R.ModifiedAt = Tempus.FromDateTime(fi.LastWriteTimeUtc);
		}
		else if (info is DirectoryInfo di){
			R.HasBytesSize = false;
			R.BytesSize = 0; // 目录大小可自定义统计
			R.CreatedAt = Tempus.FromDateTime(di.CreationTimeUtc);
			R.ModifiedAt = Tempus.FromDateTime(di.LastWriteTimeUtc);
		}
		else{
			throw new NotSupportedException($"Unsupported path type: {path}");
			// HasSizeBytes = false;
			// SizeBytes = 0;
			// CreatedAt = new Tempus();
			// ModifiedAt = new Tempus();
		}
		return R;
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


	public static string ToHumanSize(u64 bytes){
		const double KB = 1024;
		const double MB = KB * 1024;
		const double GB = MB * 1024;
		if (bytes >= GB){
			return $"{bytes / GB:F2}G".TrimEnd('0').TrimEnd('.');
		}
		if (bytes >= MB){
			return $"{bytes / MB:F2}M".TrimEnd('0').TrimEnd('.');
		}
		if (bytes >= KB){
			return $"{bytes / KB:F2}K".TrimEnd('0').TrimEnd('.');
		}
		return $"{bytes}B";
	}

}


public static class ExtnPath{
	public static bool IsDir(this IPathInfo z){
		return z.Type == EPathType.Dir
		|| z.Type == EPathType.DirSymlink
		|| z.Type == EPathType.DirJunction;
	}

	public static void AddSizeOf(
		this IPathInfo z
		,IPathInfo Other
	){
		z.BytesSize += Other.BytesSize;
		z.BytesSizeOnDisk += Other.BytesSizeOnDisk;
	}
}
