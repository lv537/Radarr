using System.Collections.Generic;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Movies.Credits
{
    public class Credit : ModelBase
    {
        public Credit()
        {
            Images = new List<MediaCover.MediaCover>();
        }

        public string Name { get; set; }
        public string CreditTmdbId { get; set; }
        public int PersonTmdbId { get; set; }
        public int MovieId { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public string CrewDepartment { get; set; }
        public string CrewJob { get; set; }
        public string CastCharacter { get; set; }
        public int Order { get; set; }
        public CreditType Type { get; set; }
    }

    public enum CreditType
    {
        Cast,
        Crew
    }
}
