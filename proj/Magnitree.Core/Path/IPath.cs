namespace Magnitree.Core.Path;
public interface IPath{
	/// <summary>
	/// 以正斜槓分隔
	/// 若潙目錄、則末尾多一個斜槓
	/// </summary>
	public str AbsPosixPath{get;}
	public EPathType Type{get;}

}

public enum EPathType{
	File,
	Dir,
	FileSymlink,     // 文件符号链接
	DirSymlink,// 目录符号链接
	DirJunction,// Windows 目录联接
	HardLinkFile,     // 文件硬链接（Linux/macOS 下无法直接区分）
	ShortcutLnk,      // Windows .lnk 快捷方式
}
