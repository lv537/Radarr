using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(166)]
    public class remove_movie_pathstate : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("PathState").FromTable("Movies");

            Execute.Sql("DELETE FROM Config WHERE [KEY] IN ('pathsdefaultstatic')");
        }
    }
}
