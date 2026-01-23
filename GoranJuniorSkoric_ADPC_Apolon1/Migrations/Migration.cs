namespace Migrations;


// Base class for database migrations.
// Each migration represents a version of the database schema.
public abstract class Migration
{
    // Gets migration id (typically a timestamp or version number).
    public abstract string Id { get; }

    // Gets migration description.
    public abstract string Description { get; }

    // Applies migration (moves database forward).
    public abstract Task UpAsync(MigrationBuilder builder);

    // Reverts migration (moves database backward).
    public abstract Task DownAsync(MigrationBuilder builder);
}
