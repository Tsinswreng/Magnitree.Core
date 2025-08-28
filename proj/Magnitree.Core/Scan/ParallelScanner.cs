#if false
namespace Magnitree.Core.Scan;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Magnitree.Core.Path;
using Node = ITreeNode<Magnitree.Core.Path.IPathInfo>;

public class ParallelScanner {
	public event EventHandler<EvtArgScanProgress>? OnScanCurrentNode;
	public event EventHandler<EvtArgScanProgress>? OnScanCompletedNode;

	public Node Tree { get; set; } = new TreeNode<IPathInfo>();

	public IPath StartPath { get; set; }

	// 用於把事件派回 UI 線程（若需要）
	private readonly SynchronizationContext? _uiCtx = SynchronizationContext.Current;

	// 統計剩餘工作，用來 await 直到掃完
	private readonly CountdownEvent _pending = new(1);

	// 線程安全的佇列
	private readonly ConcurrentQueue<(Node Parent, string FullPath, u64 Depth)> _work =
		new();

	// Node->Children 的鎖
	private readonly object _childrenLock = new();

	public async Task Scan(CT ct) {
		// 建立根節點
		var rootInfo = await PathInfo.MkAsy(StartPath.AbsPosixPath, ct);
		var root = new TreeNode<IPathInfo>(rootInfo);
		Tree = root;
		_work.Enqueue((root, StartPath.AbsPosixPath, 0));

		// 啟動固定數量的 Worker
		var workers = Enumerable.Range(0, Environment.ProcessorCount)
								.Select(_ => Task.Run(() => Worker(ct)))
								.ToArray();

		await Task.WhenAll(workers);
	}

	private async Task Worker(CT ct) {
		while (!ct.IsCancellationRequested && _work.TryDequeue(out var item)) {
			var (parent, path, depth) = item;
			await ProcessDirectoryAsync(parent, path, depth, ct);
			_pending.Signal(); // 本目錄完成
		}
	}

	private async Task ProcessDirectoryAsync(Node parent, string path, u64 depth, CT ct) {
		// 建立子節點
		var childrenPaths = ToolPath.LsFullPath(path);
		var childNodes = new List<Node>();

		foreach (var childPath in childrenPaths) {
			ct.ThrowIfCancellationRequested();
			var info = await PathInfo.MkAsy(childPath, ct);
			var node = new TreeNode<IPathInfo>(info);

			// 觸發「當前節點」事件
			PostEvent(OnScanCurrentNode, node, depth + 1);

			lock (_childrenLock) { parent.AddChild(node); }

			childNodes.Add(node);

			if (info.Type == EPathType.Dir) {
				_pending.AddCount();
				_work.Enqueue((node, childPath, depth + 1));
			} else if (info.Type == EPathType.File) {
				// 檔案直接算完
				PostEvent(OnScanCompletedNode, node, depth + 1);
				Interlocked.Add(ref parent.Data.SizeBytes, info.SizeBytes);
			}
		}

		// 目錄本身算完成
		ulong total = 0;
		foreach (var c in childNodes)
			total += c.Data.SizeBytes;

		parent.Data.SizeBytes = total;
		parent.Data.HasSizeBytes = true;
		PostEvent(OnScanCompletedNode, parent, depth);
	}

	private void PostEvent(EventHandler<EvtArgScanProgress>? handler, Node node, u64 depth) {
		if (handler == null) return;
		var args = new EvtArgScanProgress { TreeNode = node, Depth = depth };
		if (_uiCtx != null)
			_uiCtx.Post(_ => handler(this, args), null);
		else
			handler(this, args);
	}
}



#endif
