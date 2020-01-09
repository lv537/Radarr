using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Blacklisting;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.CustomFormats
{
    public interface ICustomFormatCalculationService
    {
        List<FormatTagMatchResult> MatchFormatTags(ParsedMovieInfo movieInfo);
        List<CustomFormat> ParseCustomFormat(ParsedMovieInfo movieInfo);
        List<CustomFormat> ParseCustomFormat(MovieFile movieFile);
        List<CustomFormat> ParseCustomFormat(Blacklist blacklist);
        List<CustomFormat> ParseCustomFormat(History.History history);
    }

    public class CustomFormatCalculationService : ICustomFormatCalculationService
    {
        private readonly ICustomFormatService _formatService;
        private readonly IParsingService _parsingService;
        private readonly Logger _logger;

        public CustomFormatCalculationService(ICustomFormatService formatService,
                                              IParsingService parsingService,
                                              Logger logger)
        {
            _formatService = formatService;
            _parsingService = parsingService;
            _logger = logger;
        }

        public List<FormatTagMatchResult> MatchFormatTags(ParsedMovieInfo movieInfo)
        {
            var formats = _formatService.All();

            var matches = new List<FormatTagMatchResult>();

            foreach (var customFormat in formats)
            {
                var formatMatches = customFormat.FormatTags
                    .GroupBy(t => t.TagType)
                    .Select(g => new FormatTagMatchesGroup(g.Key,
                                                           g.ToList().ToDictionary(t => t, t => t.DoesItMatch(movieInfo))))
                    .ToList();

                matches.Add(new FormatTagMatchResult
                {
                    CustomFormat = customFormat,
                    GroupMatches = formatMatches,
                    GoodMatch = formatMatches.All(g => g.DidMatch)
                });
            }

            return matches;
        }

        private List<FormatTagMatchResult> MatchFormatTags(MovieFile file)
        {
            var info = new ParsedMovieInfo
            {
                MovieTitle = file.Movie.Title,
                SimpleReleaseTitle = file.GetSceneOrFileName().SimplifyReleaseTitle(),
                Quality = file.Quality,
                Languages = file.Languages,
                ReleaseGroup = file.ReleaseGroup,
                Edition = file.Edition,
                Year = file.Movie.Year,
                ImdbId = file.Movie.ImdbId,
                ExtraInfo = new Dictionary<string, object>
                {
                    { "IndexerFlags", file.IndexerFlags },
                    { "Size", file.Size }
                }
            };

            return MatchFormatTags(info);
        }

        private List<FormatTagMatchResult> MatchFormatTags(Blacklist blacklist)
        {
            var parsed = _parsingService.ParseMovieInfo(blacklist.SourceTitle, null);

            var info = new ParsedMovieInfo
            {
                MovieTitle = blacklist.Movie.Title,
                SimpleReleaseTitle = parsed?.SimpleReleaseTitle ?? blacklist.SourceTitle.SimplifyReleaseTitle(),
                Quality = blacklist.Quality,
                Languages = blacklist.Languages,
                ReleaseGroup = parsed?.ReleaseGroup,
                Edition = parsed?.Edition,
                Year = blacklist.Movie.Year,
                ImdbId = blacklist.Movie.ImdbId,
                ExtraInfo = new Dictionary<string, object>
                {
                    { "IndexerFlags", blacklist.IndexerFlags },
                    { "Size", blacklist.Size }
                }
            };

            return MatchFormatTags(info);
        }

        private List<FormatTagMatchResult> MatchFormatTags(History.History history)
        {
            var parsed = _parsingService.ParseMovieInfo(history.SourceTitle, null);

            Enum.TryParse(history.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags);
            int.TryParse(history.Data.GetValueOrDefault("size"), out var size);

            var info = new ParsedMovieInfo
            {
                MovieTitle = history.Movie.Title,
                SimpleReleaseTitle = parsed?.SimpleReleaseTitle ?? history.SourceTitle.SimplifyReleaseTitle(),
                Quality = history.Quality,
                Languages = history.Languages,
                ReleaseGroup = parsed?.ReleaseGroup,
                Edition = parsed?.Edition,
                Year = history.Movie.Year,
                ImdbId = history.Movie.ImdbId,
                ExtraInfo = new Dictionary<string, object>
                {
                    { "IndexerFlags", flags },
                    { "Size", size }
                }
            };

            return MatchFormatTags(info);
        }

        public List<CustomFormat> ParseCustomFormat(ParsedMovieInfo movieInfo)
        {
            return MatchFormatTags(movieInfo)
                .Where(m => m.GoodMatch)
                .Select(r => r.CustomFormat)
                .ToList();
        }

        public List<CustomFormat> ParseCustomFormat(MovieFile movieFile)
        {
            return MatchFormatTags(movieFile)
                .Where(m => m.GoodMatch)
                .Select(r => r.CustomFormat)
                .ToList();
        }

        public List<CustomFormat> ParseCustomFormat(Blacklist blacklist)
        {
            return MatchFormatTags(blacklist)
                .Where(m => m.GoodMatch)
                .Select(r => r.CustomFormat)
                .ToList();
        }

        public List<CustomFormat> ParseCustomFormat(History.History history)
        {
            return MatchFormatTags(history)
                .Where(m => m.GoodMatch)
                .Select(r => r.CustomFormat)
                .ToList();
        }
    }
}
