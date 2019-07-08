using GSD.Common.Database;
using GSD.Common.FileSystem;
using GSD.Common.Tracing;
using GSD.DiskLayoutUpgrades;
using System;
using System.Collections.Generic;
using System.IO;

namespace GSD.Common.DiskLayoutUpgrades
{
    public abstract class DiskLayoutUpgrade_SqlitePlaceholders : DiskLayoutUpgrade.MajorUpgrade
    {
        public override bool TryUpgrade(ITracer tracer, string enlistmentRoot)
        {
            string dotGSDRoot = Path.Combine(enlistmentRoot, GSDPlatform.Instance.Constants.DotGSDRoot);
            try
            {
                PhysicalFileSystem fileSystem = new PhysicalFileSystem();
                string error;
                LegacyPlaceholderListDatabase placeholderList;
                if (!LegacyPlaceholderListDatabase.TryCreate(
                    tracer,
                    Path.Combine(dotGSDRoot, GSDConstants.DotGSD.Databases.PlaceholderList),
                    fileSystem,
                    out placeholderList,
                    out error))
                {
                    tracer.RelatedError("Failed to open placeholder list database: " + error);
                    return false;
                }

                using (placeholderList)
                using (GSDDatabase database = new GSDDatabase(fileSystem, enlistmentRoot, new SqliteDatabase()))
                {
                    PlaceholderTable placeholders = new PlaceholderTable(database);
                    List<IPlaceholderData> oldPlaceholderEntries = placeholderList.GetAllEntries();
                    foreach (IPlaceholderData entry in oldPlaceholderEntries)
                    {
                        placeholders.AddPlaceholderData(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                tracer.RelatedError("Error updating placeholder list database to SQLite: " + ex.ToString());
                return false;
            }

            if (!this.TryIncrementMajorVersion(tracer, enlistmentRoot))
            {
                return false;
            }

            return true;
        }
    }
}
