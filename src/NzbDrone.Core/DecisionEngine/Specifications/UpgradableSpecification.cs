using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public interface IUpgradableSpecification
    {
        bool IsUpgradable(Profile profile, QualityModel currentQuality, List<CustomFormat> currentCustomFormats, QualityModel newQuality, List<CustomFormat> newCustomFormats);
        bool CutoffNotMet(Profile profile, QualityModel currentQuality, QualityModel newQuality = null);
        bool IsRevisionUpgrade(QualityModel currentQuality, QualityModel newQuality);
        bool IsUpgradeAllowed(Profile qualityProfile, QualityModel currentQuality, QualityModel newQuality);
    }

    public class UpgradableSpecification : IUpgradableSpecification
    {
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public UpgradableSpecification(IConfigService configService, Logger logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public bool IsUpgradable(Profile profile, QualityModel currentQuality, List<CustomFormat> currentCustomFormats, QualityModel newQuality, List<CustomFormat> newCustomFormats)
        {
            var qualityComparer = new QualityModelComparer(profile);
            var qualityCompare = qualityComparer.Compare(newQuality?.Quality, currentQuality.Quality);

            if (qualityCompare > 0)
            {
                _logger.Debug("New item has a better quality");
                return true;
            }

            if (qualityCompare < 0)
            {
                _logger.Debug("Existing item has better quality, skipping");
                return false;
            }

            // Accept unless the user doesn't want to prefer propers, optionally they can
            // use preferred words to prefer propers/repacks over non-propers/repacks.
            if (_configService.AutoDownloadPropers &&
                newQuality?.Revision.CompareTo(currentQuality.Revision) > 0)
            {
                _logger.Debug("New item has a better quality revision");
                return true;
            }

            var customFormatCompare = qualityComparer.Compare(currentCustomFormats, newCustomFormats);
            if (customFormatCompare <= 0)
            {
                _logger.Debug("Existing item has a better custom format score, skipping");
                return false;
            }

            _logger.Debug("New item has a better custom format score");
            return true;
        }

        public bool CutoffNotMet(Profile profile, QualityModel currentQuality, QualityModel newQuality = null)
        {
            var cutoffCompare = new QualityModelComparer(profile).Compare(currentQuality.Quality.Id, profile.Cutoff);

            if (cutoffCompare < 0)
            {
                return true;
            }

            if (newQuality != null && IsRevisionUpgrade(currentQuality, newQuality))
            {
                return true;
            }

            return false;
        }

        public bool IsRevisionUpgrade(QualityModel currentQuality, QualityModel newQuality)
        {
            var compare = newQuality.Revision.CompareTo(currentQuality.Revision);

            if (currentQuality.Quality == newQuality.Quality && compare > 0)
            {
                _logger.Debug("New quality is a better revision for existing quality");
                return true;
            }

            return false;
        }

        public bool IsUpgradeAllowed(Profile qualityProfile, QualityModel currentQuality, QualityModel newQuality)
        {
            var isQualityUpgrade = new QualityModelComparer(qualityProfile).Compare(newQuality, currentQuality) > 0;

            if (isQualityUpgrade && qualityProfile.UpgradeAllowed)
            {
                _logger.Debug("Quality profile allows upgrading");
                return true;
            }

            return false;
        }
    }
}
