using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class CutoffSpecification : IDecisionEngineSpecification
    {
        private readonly UpgradableSpecification _qualityUpgradableSpecification;
        private readonly ICustomFormatCalculationService _formatService;
        private readonly Logger _logger;

        public CutoffSpecification(UpgradableSpecification qualityUpgradableSpecification,
                                   ICustomFormatCalculationService formatService,
                                   Logger logger)
        {
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _formatService = formatService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteMovie subject, SearchCriteriaBase searchCriteria)
        {
            var profile = subject.Movie.Profile;
            var file = subject.Movie.MovieFile;

            if (file != null)
            {
                if (!_qualityUpgradableSpecification.CutoffNotMet(profile,
                                                                  file.Quality,
                                                                  subject.ParsedMovieInfo.Quality))
                {
                    file.Movie = subject.Movie;
                    var customFormats = _formatService.ParseCustomFormat(file);
                    var cutoff = new List<CustomFormat> { profile.FormatItems.Single(x => x.Format.Id == profile.FormatCutoff).Format };
                    var result = new QualityModelComparer(profile).Compare(cutoff, customFormats);
                    if (result >= 0)
                    {
                        _logger.Debug("Existing custom formats {0} meet cutoff: {1}",
                                      customFormats.ConcatToString(),
                                      cutoff.ConcatToString());

                        var qualityCutoffIndex = profile.GetIndex(profile.Cutoff);
                        var qualityCutoff = profile.Items[qualityCutoffIndex.Index];

                        return Decision.Reject("Existing file meets cutoff: {0} - {1}", qualityCutoff, cutoff.ConcatToString());
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
