using System;
using System.Collections.Generic;

namespace ArkhamHorror.Mechanics.Persistence
{
    public interface IMechanicsSnapshotMigrator
    {
        int FromVersion { get; }
        int ToVersion { get; }
        MechanicsSnapshot Migrate(MechanicsSnapshot snapshot);
    }

    public sealed class SnapshotMigrationRegistry
    {
        private readonly Dictionary<int, IMechanicsSnapshotMigrator> migrators =
            new Dictionary<int, IMechanicsSnapshotMigrator>();

        public void Register(IMechanicsSnapshotMigrator migrator)
        {
            if (migrator == null) throw new ArgumentNullException(nameof(migrator));
            if (migrator.ToVersion != migrator.FromVersion + 1)
                throw new ArgumentException("Snapshot migrations must advance exactly one schema version.", nameof(migrator));
            if (migrators.ContainsKey(migrator.FromVersion))
                throw new InvalidOperationException("A migration from that schema version is already registered.");
            migrators.Add(migrator.FromVersion, migrator);
        }

        public MechanicsSnapshot MigrateToCurrent(MechanicsSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Version > MechanicsSnapshot.CurrentVersion)
                throw new InvalidOperationException("The snapshot was created by a newer mechanics schema.");
            MechanicsSnapshot current = snapshot;
            while (current.Version < MechanicsSnapshot.CurrentVersion)
            {
                if (!migrators.TryGetValue(current.Version, out IMechanicsSnapshotMigrator migrator))
                    throw new InvalidOperationException("No migration path exists for this snapshot schema.");
                MechanicsSnapshot migrated = migrator.Migrate(current)
                    ?? throw new InvalidOperationException("A snapshot migrator returned null.");
                if (migrated.Version != migrator.ToVersion)
                    throw new InvalidOperationException("A snapshot migrator returned the wrong schema version.");
                current = migrated;
            }
            return current;
        }
    }
}
