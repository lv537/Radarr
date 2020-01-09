using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Profiles;

namespace NzbDrone.Core.Qualities
{
    public class QualityModelComparer : IComparer<Quality>, IComparer<QualityModel>, IComparer<List<CustomFormat>>
    {
        private readonly Profile _profile;

        public QualityModelComparer(Profile profile)
        {
            Ensure.That(profile, () => profile).IsNotNull();
            Ensure.That(profile.Items, () => profile.Items).HasItems();

            _profile = profile;
        }

        public int Compare(int left, int right, bool respectGroupOrder = false)
        {
            var leftIndex = _profile.GetIndex(left);
            var rightIndex = _profile.GetIndex(right);

            return leftIndex.CompareTo(rightIndex, respectGroupOrder);
        }

        public int Compare(Quality left, Quality right)
        {
            return Compare(left, right, false);
        }

        public int Compare(Quality left, Quality right, bool respectGroupOrder)
        {
            var leftIndex = _profile.GetIndex(left);
            var rightIndex = _profile.GetIndex(right);

            return leftIndex.CompareTo(rightIndex, respectGroupOrder);
        }

        public int Compare(QualityModel left, QualityModel right)
        {
            return Compare(left, right, false);
        }

        public int Compare(QualityModel left, QualityModel right, bool respectGroupOrder)
        {
            int result = Compare(left.Quality, right.Quality, respectGroupOrder);

            if (result == 0)
            {
                result = left.Revision.CompareTo(right.Revision);
            }

            return result;
        }

        public int Compare(List<CustomFormat> left, List<CustomFormat> right)
        {
            var leftIndicies = GetIndicies(left, _profile);
            var rightIndicies = GetIndicies(right, _profile);

            // Summing powers of two ensures last format always trumps, but we order correctly if we
            // have extra formats lower down the list
            var leftTotal = leftIndicies.Select(x => Math.Pow(2, x)).Sum();
            var rightTotal = rightIndicies.Select(x => Math.Pow(2, x)).Sum();

            return leftTotal.CompareTo(rightTotal);
        }

        public static List<int> GetIndicies(List<CustomFormat> formats, Profile profile)
        {
            return formats.WithNone().Select(f => profile.FormatItems.FindIndex(v => Equals(v.Format, f))).ToList();
        }
    }
}
