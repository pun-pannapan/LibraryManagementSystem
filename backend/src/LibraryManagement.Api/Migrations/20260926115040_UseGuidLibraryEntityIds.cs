using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagement.Api.Migrations;

public partial class UseGuidLibraryEntityIds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE [BorrowTransactions] DROP CONSTRAINT [FK_BorrowTransactions_AspNetUsers_UserId];
            ALTER TABLE [BorrowTransactions] DROP CONSTRAINT [FK_BorrowTransactions_Books_BookId];
            ALTER TABLE [Books] DROP CONSTRAINT [FK_Books_Categories_CategoryId];

            DROP INDEX [IX_BorrowTransactions_BookId_Status] ON [BorrowTransactions];
            DROP INDEX [IX_BorrowTransactions_BorrowedAtUtc] ON [BorrowTransactions];
            DROP INDEX [IX_BorrowTransactions_UserId] ON [BorrowTransactions];
            DROP INDEX [IX_Books_CategoryId] ON [Books];
            DROP INDEX [IX_Books_Author] ON [Books];
            DROP INDEX [IX_Books_AvailabilityStatus] ON [Books];
            DROP INDEX [IX_Books_Isbn] ON [Books];
            DROP INDEX [IX_Books_Title] ON [Books];
            """);

        migrationBuilder.Sql("""
            ALTER TABLE [Categories] ADD [MigrationId] uniqueidentifier NULL;
            ALTER TABLE [Books] ADD [MigrationId] uniqueidentifier NULL, [MigrationCategoryId] uniqueidentifier NULL;
            ALTER TABLE [BorrowTransactions] ADD [MigrationId] uniqueidentifier NULL, [MigrationBookId] uniqueidentifier NULL;
            """);

        migrationBuilder.Sql("UPDATE [Categories] SET [MigrationId] = NEWID();");

        migrationBuilder.Sql("""
            UPDATE [Books]
            SET [MigrationId] = NEWID(),
                [MigrationCategoryId] = [Categories].[MigrationId]
            FROM [Books]
            INNER JOIN [Categories] ON [Categories].[Id] = [Books].[CategoryId];
            """);

        migrationBuilder.Sql("""
            UPDATE [BorrowTransactions]
            SET [MigrationId] = NEWID(),
                [MigrationBookId] = [Books].[MigrationId]
            FROM [BorrowTransactions]
            INNER JOIN [Books] ON [Books].[Id] = [BorrowTransactions].[BookId];
            """);

        migrationBuilder.Sql("""
            ALTER TABLE [BorrowTransactions] DROP CONSTRAINT [PK_BorrowTransactions];
            ALTER TABLE [Books] DROP CONSTRAINT [PK_Books];
            ALTER TABLE [Categories] DROP CONSTRAINT [PK_Categories];
            """);

        migrationBuilder.Sql("""
            ALTER TABLE [BorrowTransactions] DROP COLUMN [Id], [BookId];
            ALTER TABLE [Books] DROP COLUMN [Id], [CategoryId];
            ALTER TABLE [Categories] DROP COLUMN [Id];
            """);

        migrationBuilder.Sql("""
            EXEC sp_rename N'[BorrowTransactions].[MigrationId]', N'Id', N'COLUMN';
            EXEC sp_rename N'[BorrowTransactions].[MigrationBookId]', N'BookId', N'COLUMN';
            EXEC sp_rename N'[Books].[MigrationId]', N'Id', N'COLUMN';
            EXEC sp_rename N'[Books].[MigrationCategoryId]', N'CategoryId', N'COLUMN';
            EXEC sp_rename N'[Categories].[MigrationId]', N'Id', N'COLUMN';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE [BorrowTransactions] ALTER COLUMN [Id] uniqueidentifier NOT NULL;
            ALTER TABLE [BorrowTransactions] ALTER COLUMN [BookId] uniqueidentifier NOT NULL;
            ALTER TABLE [Books] ALTER COLUMN [Id] uniqueidentifier NOT NULL;
            ALTER TABLE [Books] ALTER COLUMN [CategoryId] uniqueidentifier NOT NULL;
            ALTER TABLE [Categories] ALTER COLUMN [Id] uniqueidentifier NOT NULL;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE [Categories] ADD CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]);
            ALTER TABLE [Books] ADD CONSTRAINT [PK_Books] PRIMARY KEY ([Id]);
            ALTER TABLE [BorrowTransactions] ADD CONSTRAINT [PK_BorrowTransactions] PRIMARY KEY ([Id]);
            """);

        migrationBuilder.Sql("""
            ALTER TABLE [Books] ADD CONSTRAINT [FK_Books_Categories_CategoryId]
                FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION;
            ALTER TABLE [BorrowTransactions] ADD CONSTRAINT [FK_BorrowTransactions_Books_BookId]
                FOREIGN KEY ([BookId]) REFERENCES [Books] ([Id]) ON DELETE NO ACTION;
            ALTER TABLE [BorrowTransactions] ADD CONSTRAINT [FK_BorrowTransactions_AspNetUsers_UserId]
                FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
            """);

        migrationBuilder.Sql("""
            CREATE INDEX [IX_Books_CategoryId] ON [Books] ([CategoryId]);
            CREATE INDEX [IX_Books_Author] ON [Books] ([Author]);
            CREATE INDEX [IX_Books_AvailabilityStatus] ON [Books] ([AvailabilityStatus]);
            CREATE UNIQUE INDEX [IX_Books_Isbn] ON [Books] ([Isbn]);
            CREATE INDEX [IX_Books_Title] ON [Books] ([Title]);
            CREATE INDEX [IX_BorrowTransactions_BookId_Status] ON [BorrowTransactions] ([BookId], [Status]);
            CREATE INDEX [IX_BorrowTransactions_BorrowedAtUtc] ON [BorrowTransactions] ([BorrowedAtUtc]);
            CREATE INDEX [IX_BorrowTransactions_UserId] ON [BorrowTransactions] ([UserId]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Converting generated GUID identifiers back to identity integers is not supported.");
    }
}
