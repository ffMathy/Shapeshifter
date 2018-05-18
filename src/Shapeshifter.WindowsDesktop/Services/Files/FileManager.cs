namespace Shapeshifter.WindowsDesktop.Services.Files
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;

	using Infrastructure.Threading;
	using Infrastructure.Threading.Interfaces;

	using Interfaces;
	using Serilog;

	public class FileManager
		: IFileManager
	{
		readonly IRetryingThreadLoop retryingThreadLoop;
		readonly IThreadDelay threadDelay;
		readonly ILogger logger;

		readonly ICollection<string> temporaryPaths;

		public FileManager(
			IRetryingThreadLoop retryingThreadLoop,
			IThreadDelay threadDelay,
			ILogger logger)
		{
			this.retryingThreadLoop = retryingThreadLoop;
			this.threadDelay = threadDelay;
			this.logger = logger;
			temporaryPaths = new HashSet<string>();

			var directory = PrepareTemporaryFolder();
			PurgeDirectory(directory);
		}

		static void WrapGracefully(Action action)
		{
			try
			{
				action();
			}
			catch
			{
				// ignored
			}
		}

		void PurgeDirectory(string directory)
		{
			foreach (var file in Directory.GetFiles(directory))
			{
				WrapGracefully(() => DeleteFileIfExists(file));
			}

			foreach (var folder in Directory.GetDirectories(directory))
			{
				WrapGracefully(() => PurgeDirectory(folder));
			}
		}

		public void Dispose()
		{
			foreach (var temporaryPath in temporaryPaths)
			{
				PurgePath(temporaryPath);
			}
		}

		void PurgePath(string temporaryPath)
		{
			DeleteFileIfExists(temporaryPath);
			DeleteDirectoryIfExists(temporaryPath);
		}

		static RetryingThreadLoopJob CreateRetryingFileJob(
			Func<Task> task)
		{
			return new RetryingThreadLoopJob {
				Action = task,
				AttemptsBeforeFailing = 5,
				IntervalInMilliseconds = 1000,
				IsExceptionIgnored = IsExceptionIgnored
			};
		}

		static bool IsExceptionIgnored(Exception ex)
		{
			return ex is IOException || ex is UnauthorizedAccessException;
		}

		public Task DeleteFileIfExistsAsync(string path)
		{
			return retryingThreadLoop.StartAsync(
				CreateRetryingFileJob(
					async () => DeleteFileIfExists(path)));
		}

		void DeleteFileIfExists(string path)
		{
			if (!File.Exists(path))
				return;

			logger.Verbose("Deleting file {file}.", path);
			File.Delete(path);
		}

		public Task DeleteIsolatedFileIfExistsAsync(string path)
		{
			return DeleteFileIfExistsAsync(
				GetFullPathFromIsolatedPath(path));
		}

		public Task DeleteIsolatedDirectoryIfExistsAsync(string path)
		{
			return DeleteDirectoryIfExistsAsync(
				GetFullPathFromIsolatedPath(path));
		}

		public async Task CopyFileAsync(string origin, string destination)
		{
			await retryingThreadLoop.StartAsync(
				CreateRetryingFileJob(
					async () => File.Copy(origin, destination, true)));
			while (!File.Exists(destination))
				await threadDelay.ExecuteAsync(100);
		}

		public Task DeleteDirectoryIfExistsAsync(string path)
		{
			return retryingThreadLoop.StartAsync(
				CreateRetryingFileJob(
					async () => DeleteDirectoryIfExists(path)));
		}

		void DeleteDirectoryIfExists(string path)
		{
			if (!Directory.Exists(path))
				return;

			logger.Verbose("Deleting directory {directory}.", path);
			Directory.Delete(path, true);
		}

		static string GetIsolatedPathRoot()
		{
			return PrepareShapeshifterFolder(
				Environment.GetFolderPath(
					Environment.SpecialFolder.LocalApplicationData));
		}

		static string PrepareShapeshifterFolder(string basePath)
		{
			const string folderName = nameof(Shapeshifter);

			var path = Path.Combine(basePath, folderName);
			CreateDirectoryIfNotExists(path);

			return path;
		}

		static string PrepareTemporaryFolder()
		{
			return PrepareShapeshifterFolder(
				Path.GetTempPath());
		}

		public string FindCommonFolderFromPaths(IReadOnlyCollection<string> paths)
		{
			var pathSimilarityIndex = GetPathSegmentsInCommonCount(paths);

			var firstPath = paths.First();
			var segments = GetPathSegments(firstPath);

			var commonPath = Path.Combine(
				segments
					.Take(pathSimilarityIndex)
					.ToArray());

			return commonPath;
		}

		static int GetPathSegmentsInCommonCount(IReadOnlyCollection<string> paths)
		{
			var splitPaths = paths
				.Select(p => p.Split('\\', '/'))
				.ToList();

			var shortestPath = splitPaths
				.OrderBy(p => p.Count())
				.First();

			var commonCount = shortestPath
				.TakeWhile((s, i) =>
					splitPaths.All(sp => sp[i] == s))
				.Count();

			return commonCount;
		}

		static string[] GetPathSegments(string originPath)
		{
			return originPath.Split('\\', '/');
		}

		public string PrepareFolder(string path)
		{
			CreateDirectoryIfNotExists(path);
			return path;
		}

		public string PrepareIsolatedFolder(string relativePath = null)
		{
			var finalPath = GetFullPathFromIsolatedPath(relativePath);
			return PrepareFolder(finalPath);
		}

		public async Task AppendLineToFileAsync(string path, string line)
		{
			using (var writer = new StreamWriter(File.Open(path, FileMode.Append)))
			{
				await writer.WriteLineAsync(line);
			}
		}

		public async Task<string> WriteBytesToTemporaryFileAsync(string relativePath, byte[] bytes)
		{
			var finalPath = GetFullPathFromTemporaryPath(relativePath);
			await WriteBytesToFileAsync(finalPath, bytes);

			temporaryPaths.Add(finalPath);

			return finalPath;
		}

		public async Task<string> AppendLineToTemporaryFileAsync(string relativePath, string line)
		{
			var finalPath = GetFullPathFromTemporaryPath(relativePath);
			await AppendLineToFileAsync(finalPath, line);

			temporaryPaths.Add(finalPath);

			return finalPath;
		}

		public async Task<string> PrepareTemporaryFolderAsync(string relativePath)
		{
			var finalPath = GetFullPathFromTemporaryPath(relativePath);
			WatchDirectory(finalPath);

			await threadDelay.ExecuteAsync(1000);

			return finalPath;
		}

		void WatchDirectory(string finalPath)
		{
			temporaryPaths.Add(finalPath);
			CreateDirectoryIfNotExists(finalPath);
		}

		static void CreateDirectoryIfNotExists(string relativePath)
		{
			if (!Directory.Exists(relativePath))
			{
				Directory.CreateDirectory(relativePath);
			}
		}

		public static string GetFullPathFromIsolatedPath(string path = null)
		{
			var isolatedFolderPath = GetIsolatedPathRoot();

			var finalPath = path == null
				? isolatedFolderPath
				: Path.Combine(isolatedFolderPath, path);
			return finalPath;
		}

		public static string GetFullPathFromTemporaryPath(string path = null)
		{
			var isolatedFolderPath = PrepareTemporaryFolder();

			var finalPath = path == null
				? isolatedFolderPath
				: Path.Combine(isolatedFolderPath, path);
			return finalPath;
		}

		public async Task WriteBytesToFileAsync(string relativePath, byte[] bytes)
		{
			using (var file = File.Open(relativePath, FileMode.Create))
			{
				await file.WriteAsync(bytes, 0, bytes.Length);
			}
		}
	}
}