﻿using FluentMigrator;

namespace Structerre.MetaStore.Database.Migrations.Schema
{
    [Migration(8)]
    public class Create_HandlerSequence_Table : Migration
    {
        public override void Up()
        {
            Create.Table("HandlerSequence")
                .WithColumn("Dispatcher")
                .AsString(150)
                .PrimaryKey()
                .NotNullable()
                .WithColumn("Pointer")
                .AsInt64()
                .NotNullable();
        }

        public override void Down()
        {
            Delete.Table("HandlerSequence");
        }
    }
}