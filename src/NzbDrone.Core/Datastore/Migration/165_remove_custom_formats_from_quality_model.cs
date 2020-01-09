using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(165)]
    public class remove_custom_formats_from_quality_model : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Blacklist").AddColumn("IndexerFlags").AsInt32().WithDefaultValue(0);
            Alter.Table("MovieFiles").AddColumn("IndexerFlags").AsInt32().WithDefaultValue(0);

            // Remove Custom Formats from QualityModel
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<QualityModel164>());
            Execute.WithConnection((conn, tran) => RemoveCustomFormatFromQuality(conn, tran, "Blacklist"));
            Execute.WithConnection((conn, tran) => RemoveCustomFormatFromQuality(conn, tran, "History"));
            Execute.WithConnection((conn, tran) => RemoveCustomFormatFromQuality(conn, tran, "MovieFiles"));

            // Fish out indexer flags from history
            Execute.WithConnection(AddIndexerFlagsToBlacklist);
            Execute.WithConnection(AddIndexerFlagsToMovieFiles);
        }

        private void RemoveCustomFormatFromQuality(IDbConnection conn, IDbTransaction tran, string table)
        {
            var rows = conn.Query<QualityRow>($"SELECT Id, Quality from {table}");

            var sql = $"UPDATE {table} SET Quality = @Quality WHERE Id = @Id";

            conn.Execute(sql, rows, transaction: tran);
        }

        private void AddIndexerFlagsToBlacklist(IDbConnection conn, IDbTransaction tran)
        {
            var blacklists = conn.Query<BlacklistData>("SELECT Blacklist.Id, Blacklist.TorrentInfoHash, History.Data " +
                                                       "FROM Blacklist " +
                                                       "JOIN History ON Blacklist.MovieId = History.MovieId " +
                                                       "WHERE History.EventType = 1");

            var toUpdate = new List<IndexerFlagsItem>();

            foreach (var item in blacklists)
            {
                var dict = Json.Deserialize<Dictionary<string, string>>(item.Data);

                if (dict.GetValueOrDefault("torrentInfoHash") == item.TorrentInfoHash &&
                    Enum.TryParse(dict.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                {
                    if (flags != 0)
                    {
                        toUpdate.Add(new IndexerFlagsItem
                        {
                            Id = item.Id,
                            IndexerFlags = (int)flags
                        });
                    }
                }
            }

            var updateSql = "UPDATE Blacklist SET IndexerFlags = @IndexerFlags WHERE Id = @Id";
            conn.Execute(updateSql, toUpdate, transaction: tran);
        }

        private void AddIndexerFlagsToMovieFiles(IDbConnection conn, IDbTransaction tran)
        {
            var movieFiles = conn.Query<MovieFileData>("SELECT MovieFiles.Id, MovieFiles.SceneName, History.SourceTitle, History.Data " +
                                                       "FROM MovieFiles " +
                                                       "JOIN History ON MovieFiles.MovieId = History.MovieId " +
                                                       "WHERE History.EventType = 1");

            var toUpdate = new List<IndexerFlagsItem>();

            foreach (var item in movieFiles)
            {
                var dict = Json.Deserialize<Dictionary<string, string>>(item.Data);

                if (item.SourceTitle == item.SceneName &&
                    Enum.TryParse(dict.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                {
                    if (flags != 0)
                    {
                        toUpdate.Add(new IndexerFlagsItem
                        {
                            Id = item.Id,
                            IndexerFlags = (int)flags
                        });
                    }
                }
            }

            var updateSql = "UPDATE MovieFiles SET IndexerFlags = @IndexerFlags WHERE Id = @Id";
            conn.Execute(updateSql, toUpdate, transaction: tran);
        }

        private class BlacklistData : ModelBase
        {
            public string TorrentInfoHash { get; set; }
            public string Data { get; set; }
        }

        private class MovieFileData : ModelBase
        {
            public string SceneName { get; set; }
            public string SourceTitle { get; set; }
            public string Data { get; set; }
        }

        private class IndexerFlagsItem : ModelBase
        {
            public int IndexerFlags { get; set; }
        }

        private class QualityRow : ModelBase
        {
            public QualityModel164 Quality { get; set; }
        }

        private class QualityModel164
        {
            public int Quality { get; set; }
            public Revision164 Revision { get; set; }
            public string HardcodedSubs { get; set; }
        }

        private class Revision164
        {
            public int Version { get; set; }
            public int Real { get; set; }
            public bool IsRepack { get; set; }
        }
    }
}
