namespace Shapeshifter.WindowsDesktop.Services.Web
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Files;
	using Files.Interfaces;

	using Infrastructure.Handles.Factories.Interfaces;
	using Infrastructure.Threading.Interfaces;

	using Interfaces;

	class LinkParser : ILinkParser
	{
		readonly IAsyncFilter asyncFilter;
		readonly IDomainNameResolver domainNameResolver;
		readonly IFileTypeInterpreter fileTypeInterpreter;
		readonly IPerformanceHandleFactory performanceHandleFactory;

		public LinkParser(
			IFileTypeInterpreter fileTypeInterpreter,
			IDomainNameResolver domainNameResolver,
			IAsyncFilter asyncFilter,
			IPerformanceHandleFactory performanceHandleFactory)
		{
			this.fileTypeInterpreter = fileTypeInterpreter;
			this.domainNameResolver = domainNameResolver;
			this.asyncFilter = asyncFilter;
			this.performanceHandleFactory = performanceHandleFactory;
		}

		public async Task<IReadOnlyCollection<string>> ExtractLinksFromTextAsync(string text)
		{
			return await Task.Run(
				async () =>
				{
					var words = GetWords(text);
					return await asyncFilter.FilterAsync(words, IsValidLinkAsync);
				});
		}

		static string[] GetWords(string text)
		{
			return text.Split(new[] { '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		}

		public LinkType GetLinkType(string link)
		{
			var linkType = default(LinkType);
			if (fileTypeInterpreter.GetFileTypeFromFileName(link) == FileType.Image)
			{
				linkType |= LinkType.ImageFile;
			}

			if (link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				linkType |= LinkType.Https;
			}
			else if (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
			{
				linkType |= LinkType.Http;
			}

			return linkType;
		}

		public bool IsLinkOfType(string link, LinkType type)
		{
			var linkType = GetLinkType(link);
			return (linkType == type) || linkType.HasFlag(type);
		}

		public async Task<bool> HasLinkAsync(string text)
		{
			return await HasLinkWithConditionsAsync(text, IsValidLinkAsync);
		}

		public async Task<bool> HasLinkOfTypeAsync(string text, LinkType linkType)
		{
			async Task<bool> ValidationFunction(string word) =>
				IsLinkOfType(word, linkType) &&
				await IsValidLinkAsync(word);

			return await HasLinkWithConditionsAsync(text, ValidationFunction);
		}

		async Task<bool> HasLinkWithConditionsAsync(string text, Func<string, Task<bool>> validationFunction)
		{
			using (performanceHandleFactory.StartMeasuringPerformance())
			{
				var words = GetWords(text);
				return await asyncFilter.HasMatchAsync(
					words,
					validationFunction);
			}
		}

		public async Task<bool> IsValidLinkAsync(string link)
		{
			if (link.Length > 1024)
				return false;

			if (link.StartsWith("."))
				return false;

			if (link.Count(x => x == '.') == 0)
				return false;

			if (link.Contains(".."))
				return false;

			var domain = link;

			if (domain.StartsWith("http://"))
				domain = domain.Substring("http://".Length);

			if (domain.StartsWith("https://"))
				domain = domain.Substring("https://".Length);

			var pathOrQueryIndex = domain.IndexOfAny(new[] { '?', '&', '/' });
			if (pathOrQueryIndex != -1)
				domain = domain.Substring(0, pathOrQueryIndex);

			return await domainNameResolver.IsValidDomainAsync(domain);

		}
	}
}