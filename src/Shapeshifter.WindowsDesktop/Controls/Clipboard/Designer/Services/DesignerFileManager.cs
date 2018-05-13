namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using WindowsDesktop.Services.Files.Interfaces;

    using Controls.Designer.Services;

	class DesignerFileManager
		: IFileManager,
		  IDesignerService
	{
		public Task AppendLineToFileAsync(string path, string line)
		{
			return Task.CompletedTask;
		}

		public Task<string> AppendLineToTemporaryFileAsync(string relativePath, string line)
		{
			return Task.FromResult<string>(null);
		}

		public Task CopyFileAsync(string origin, string destination)
		{
			throw new System.NotImplementedException();
		}

		public Task DeleteDirectoryIfExistsAsync(string path)
		{
			return Task.CompletedTask;
		}

		public Task DeleteFileIfExistsAsync(string path)
		{
			return Task.CompletedTask;
		}

		public Task DeleteIsolatedDirectoryIfExistsAsync(string relativePath)
		{
			return Task.CompletedTask;
		}

		public Task DeleteIsolatedFileIfExistsAsync(string relativePath)
		{
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			
		}

		public string FindCommonFolderFromPaths(IReadOnlyCollection<string> paths)
		{
			return null;
		}

		public string PrepareFolder(string path)
		{
			return null;
		}

		public string PrepareIsolatedFolder(string relativePath = null)
		{
			return null;
		}

		public string PrepareNewIsolatedFolder(string relativePath)
		{
			return null;
		}

		public string PrepareTemporaryFolderAsync(string relativePath)
		{
			return null;
		}

		public Task WriteBytesToFileAsync(string path, byte[] bytes)
		{
			return Task.CompletedTask;
		}

		public Task<string> WriteBytesToTemporaryFileAsync(string relativePath, byte[] bytes)
		{
			return Task.FromResult<string>(null);
		}

		Task<string> IFileManager.PrepareTemporaryFolderAsync(string relativePath)
		{
			throw new System.NotImplementedException();
		}
	}
}