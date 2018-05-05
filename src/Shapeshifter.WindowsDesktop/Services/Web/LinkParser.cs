namespace Shapeshifter.WindowsDesktop.Services.Web
{
	using System;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;

	using Files;
	using Files.Interfaces;

	using Infrastructure.Handles.Factories.Interfaces;
	using Infrastructure.Threading.Interfaces;

	using Interfaces;

	class LinkParser : ILinkParser
	{
		static readonly Regex linkValidationExpression;

		static readonly Regex whitespaceExpression;

		readonly IAsyncFilter asyncFilter;

		readonly IDomainNameResolver domainNameResolver;

		readonly IFileTypeInterpreter fileTypeInterpreter;

		readonly IPerformanceHandleFactory performanceHandleFactory;

		static LinkParser()
		{
			linkValidationExpression =
				new Regex(
					@"^(?:(?:https?|ftp):\/\/)?((?:\S+(?::\S*)?@)?(?:(?!10(?:\.\d{1,3}){3})(?!127(?:\.\d{1,3}){3})(?!169\.254(?:\.\d{1,3}){2})(?!192\.168(?:\.\d{1,3}){2})(?!172\.(?:1[6-9]|2\d|3[0-1])(?:\.\d{1,3}){2})(?:[1-9]\d?|1\d\d|2[01]\d|22[0-3])(?:\.(?:1?\d{1,2}|2[0-4]\d|25[0-5])){2}(?:\.(?:[1-9]\d?|1\d\d|2[0-4]\d|25[0-4]))|(?:(?:[a-z\u00A1-\uFFFF0-9]+-?)*[a-z\u00A1-\uFFFF0-9]+)(?:\.(?:[a-z\u00A1-\uFFFF0-9]+-?)*[a-z\u00A1-\uFFFF0-9]+)*(?:\.(?:[a-z\u00A1-\uFFFF]{2,})))(?::\d{2,5})?)(?:\/?[^\s]*)?$",
					RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline |
					RegexOptions.CultureInvariant,
					TimeSpan.FromMilliseconds(25));
			whitespaceExpression = new Regex(@"\s", RegexOptions.Compiled);
		}

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
				async () => {
					var words = GetWords(text);
					return await asyncFilter.FilterAsync(words, IsValidLinkAsync);
				});
		}

		static string[] GetWords(string text)
		{
			return whitespaceExpression.Split(text);
		}

		static IEnumerable<string> ExtractSuspiciousWords(string[] words)
		{
			foreach (var word in words)
			{
				if (IsSuspiciousWord(word))
				{
					yield return word;
				}
			}
		}

		static IEnumerable<string> ExtractNonSuspiciousWords(string[] words)
		{
			foreach (var word in words)
			{
				if (!IsSuspiciousWord(word))
				{
					yield return word;
				}
			}
		}

		static bool IsSuspiciousWord(string word)
		{
			return
				word.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ||
				word.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
				word.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
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
			using (performanceHandleFactory.StartMeasuringPerformance())
				return await Task.Run(
					async () => {
						var words = GetWords(text);

						var suspiciousWords = ExtractSuspiciousWords(words);
						if (
							await asyncFilter.HasMatchAsync(
								suspiciousWords,
								IsValidLinkAsync))
						{
							return true;
						}

						var nonSuspiciousWords = ExtractNonSuspiciousWords(words);
						return await asyncFilter.HasMatchAsync(
							nonSuspiciousWords,
							IsValidLinkAsync);
					});
		}

		public async Task<bool> HasLinkOfTypeAsync(string text, LinkType linkType)
		{
			using (performanceHandleFactory.StartMeasuringPerformance())
			{
				Func<string, Task<bool>> validationFunction =
					async word => IsLinkOfType(word, linkType) && await IsValidLinkAsync(word);
				return await Task.Run(
					async () => {
						var words = GetWords(text);

						var suspiciousWords = ExtractSuspiciousWords(words);
						if (
							await
							asyncFilter.HasMatchAsync(
								suspiciousWords,
								validationFunction))
						{
							return true;
						}

						var nonSuspiciousWords = ExtractNonSuspiciousWords(words);
						return
							await
							asyncFilter.HasMatchAsync(
								nonSuspiciousWords,
								validationFunction);
					});
			}
		}

		public async Task<bool> IsValidLinkAsync(string link)
		{
			try
			{
				var match = linkValidationExpression.Match(link);
				if (!match.Success)
				{
					return false;
				}

				var domain = match.Groups[1].Value;
				return linkValidationExpression.IsMatch(link) &&
					   await domainNameResolver.IsValidDomainAsync(domain);
			}
			catch (RegexMatchTimeoutException)
			{
				return false;
			}
		}
	}
}