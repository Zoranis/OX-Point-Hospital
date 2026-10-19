using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OxBox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Boxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Site = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BuHost = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    BuPort = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostCentres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCentres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    FullFlag = table.Column<int>(type: "integer", nullable: false),
                    EmptyFlag = table.Column<int>(type: "integer", nullable: false),
                    DisplayTimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LowStockThreshold = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                    table.CheckConstraint("CK_SystemSettings_Singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ServiceBarcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BoxId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Board = table.Column<int>(type: "integer", nullable: false),
                    LockNumber = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Boxes_BoxId",
                        column: x => x.BoxId,
                        principalTable: "Boxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlertRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CostCentreId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertRecipients_CostCentres_CostCentreId",
                        column: x => x.CostCentreId,
                        principalTable: "CostCentres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BoxCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CostCentreId = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RaisedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClearedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_CostCentres_CostCentreId",
                        column: x => x.CostCentreId,
                        principalTable: "CostCentres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CylinderTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BoxCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PositionNumber = table.Column<int>(type: "integer", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CostCentreCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FlagAtTime = table.Column<int>(type: "integer", nullable: false),
                    TechnicianId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CylinderTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CylinderTransactions_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cylinders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CostCentreId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PositionId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cylinders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cylinders_CostCentres_CostCentreId",
                        column: x => x.CostCentreId,
                        principalTable: "CostCentres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cylinders_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AlertId = table.Column<long>(type: "bigint", nullable: false),
                    Channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboxMessages_Alerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "Alerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CylinderStatusChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CylinderId = table.Column<int>(type: "integer", nullable: false),
                    AtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FromLocation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToLocation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CylinderStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CylinderStatusChanges_Cylinders_CylinderId",
                        column: x => x.CylinderId,
                        principalTable: "Cylinders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRecipients_CostCentreId",
                table: "AlertRecipients",
                column: "CostCentreId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_CostCentreId",
                table: "Alerts",
                column: "CostCentreId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_OneActivePerKey",
                table: "Alerts",
                column: "Key",
                unique: true,
                filter: "\"ClearedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_RaisedAtUtc",
                table: "Alerts",
                column: "RaisedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_Code",
                table: "Boxes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostCentres_Code",
                table: "CostCentres",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cylinders_Barcode",
                table: "Cylinders",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cylinders_CostCentreId_Status_Location",
                table: "Cylinders",
                columns: new[] { "CostCentreId", "Status", "Location" });

            migrationBuilder.CreateIndex(
                name: "IX_Cylinders_PositionId",
                table: "Cylinders",
                column: "PositionId",
                unique: true,
                filter: "\"PositionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CylinderStatusChanges_CylinderId_AtUtc",
                table: "CylinderStatusChanges",
                columns: new[] { "CylinderId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CylinderTransactions_AtUtc_Action",
                table: "CylinderTransactions",
                columns: new[] { "AtUtc", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_CylinderTransactions_Barcode",
                table: "CylinderTransactions",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_CylinderTransactions_CostCentreCode_AtUtc",
                table: "CylinderTransactions",
                columns: new[] { "CostCentreCode", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CylinderTransactions_TechnicianId",
                table: "CylinderTransactions",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_AlertId",
                table: "OutboxMessages",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_NextAttemptAtUtc",
                table: "OutboxMessages",
                column: "NextAttemptAtUtc",
                filter: "\"SentAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_BoxId_Board_LockNumber",
                table: "Positions",
                columns: new[] { "BoxId", "Board", "LockNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_BoxId_Number",
                table: "Positions",
                columns: new[] { "BoxId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_ServiceBarcode",
                table: "Technicians",
                column: "ServiceBarcode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertRecipients");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CylinderStatusChanges");

            migrationBuilder.DropTable(
                name: "CylinderTransactions");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Cylinders");

            migrationBuilder.DropTable(
                name: "Technicians");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "CostCentres");

            migrationBuilder.DropTable(
                name: "Boxes");
        }
    }
}
