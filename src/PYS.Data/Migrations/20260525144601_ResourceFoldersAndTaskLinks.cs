using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PYS.Data.Migrations
{
    /// <inheritdoc />
    public partial class ResourceFoldersAndTaskLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentFolderId",
                table: "ProjectResources",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskResources_ProjectResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "ProjectResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskResources_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectResources_ParentFolderId",
                table: "ProjectResources",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskResources_ResourceId",
                table: "TaskResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskResources_TaskId_ResourceId",
                table: "TaskResources",
                columns: new[] { "TaskId", "ResourceId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectResources_ProjectResources_ParentFolderId",
                table: "ProjectResources",
                column: "ParentFolderId",
                principalTable: "ProjectResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectResources_ProjectResources_ParentFolderId",
                table: "ProjectResources");

            migrationBuilder.DropTable(
                name: "TaskResources");

            migrationBuilder.DropIndex(
                name: "IX_ProjectResources_ParentFolderId",
                table: "ProjectResources");

            migrationBuilder.DropColumn(
                name: "ParentFolderId",
                table: "ProjectResources");
        }
    }
}
