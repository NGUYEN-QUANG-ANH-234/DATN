using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "allowance_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TypeName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsTaxable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsInsuranceBase = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_allowance_types", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "configurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ParamKey = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParamValue = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configurations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "leave_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TypeName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPaid = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_types", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payroll_formulas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FormulaName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Expression = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeadlineAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_formulas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PermissionCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JobLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ShiftName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    LateThresholdMins = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_shifts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OAuthId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RefreshToken = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsMfaEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MfaSecretKey = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_accounts_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    ActionType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TableName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_logs_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "attendance_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    ShiftId = table.Column<int>(type: "int", nullable: true),
                    CheckIn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CheckOut = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GpsLat = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    GpsLong = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendance_logs_work_shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "work_shifts",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "candidates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RecruitmentRequestId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CvFilePath = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AppliedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    ContractNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContractType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BasicSalary = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    SalaryPercentage = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contracts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "department_budgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DeptId = table.Column<int>(type: "int", nullable: true),
                    MonthYear = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TotalBudget = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    UsedBudget = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeadlineAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department_budgets", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DeptCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeptName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParentDeptId = table.Column<int>(type: "int", nullable: true),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_departments_departments_ParentDeptId",
                        column: x => x.ParentDeptId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    CandidateId = table.Column<int>(type: "int", nullable: true),
                    EmployeeCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IdentityNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TaxCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SocialInsCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BankAccount = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BankName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeptId = table.Column<int>(type: "int", nullable: true),
                    PositionId = table.Column<int>(type: "int", nullable: true),
                    IsIntern = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    JoinedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_employees_candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_employees_departments_DeptId",
                        column: x => x.DeptId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "recruitment_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DeptId = table.Column<int>(type: "int", nullable: true),
                    PositionId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deadline = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recruitment_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recruitment_requests_departments_DeptId",
                        column: x => x.DeptId,
                        principalTable: "departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_recruitment_requests_positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "positions",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "dependents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Relationship = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BirthDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dependents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dependents_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "employee_allowances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    AllowanceTypeId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_allowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_allowances_allowance_types_AllowanceTypeId",
                        column: x => x.AllowanceTypeId,
                        principalTable: "allowance_types",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_employee_allowances_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "employment_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangeDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employment_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employment_history_employees_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_employment_history_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "leave_balances",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    TotalDays = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    UsedDays = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_balances", x => new { x.EmployeeId, x.LeaveTypeId, x.Year });
                    table.ForeignKey(
                        name: "FK_leave_balances_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_leave_balances_leave_types_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "leave_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Reason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeadlineAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leave_requests_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_leave_requests_leave_types_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "leave_types",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "payrolls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    Month = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    Year = table.Column<short>(type: "smallint", nullable: true),
                    GrossSalary = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    TotalAllowance = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    TotalBonus = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    InsuranceDeduction = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    TaxDeductionFamily = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    TaxableIncome = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    PitAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    NetSalary = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payrolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payrolls_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    RequestType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetDeptId = table.Column<int>(type: "int", nullable: true),
                    TargetPositionId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentApproverId = table.Column<int>(type: "int", nullable: true),
                    DeadlineAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_requests_departments_TargetDeptId",
                        column: x => x.TargetDeptId,
                        principalTable: "departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_requests_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_requests_positions_TargetPositionId",
                        column: x => x.TargetPositionId,
                        principalTable: "positions",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TaskType = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedTo = table.Column<int>(type: "int", nullable: true),
                    DeptBudgetId = table.Column<int>(type: "int", nullable: true),
                    BonusAmount = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    ActualBonus = table.Column<decimal>(type: "DECIMAL(15,2)", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EvidencePath = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deadline = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tasks_department_budgets_DeptBudgetId",
                        column: x => x.DeptBudgetId,
                        principalTable: "department_budgets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tasks_employees_AssignedTo",
                        column: x => x.AssignedTo,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "trainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    CourseName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deadline = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trainings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trainings_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "handover_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    SenderId = table.Column<int>(type: "int", nullable: true),
                    ReceiverId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "VARCHAR(50)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeadlineAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handover_requests_employees_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_handover_requests_employees_SenderId",
                        column: x => x.SenderId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_handover_requests_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "requests",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "task_feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TaskId = table.Column<int>(type: "int", nullable: true),
                    ReviewerId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_feedbacks_employees_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_task_feedbacks_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "task_progresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TaskId = table.Column<int>(type: "int", nullable: true),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_progresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_progresses_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "handover_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    HandoverRequestId = table.Column<int>(type: "int", nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: true),
                    ItemName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_handover_items_handover_requests_HandoverRequestId",
                        column: x => x.HandoverRequestId,
                        principalTable: "handover_requests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_handover_items_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "tasks",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_Email",
                table: "accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_RoleId",
                table: "accounts",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_logs_EmployeeId",
                table: "attendance_logs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_logs_ShiftId",
                table: "attendance_logs",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_AccountId",
                table: "audit_logs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_candidates_RecruitmentRequestId",
                table: "candidates",
                column: "RecruitmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_ContractNumber",
                table: "contracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contracts_EmployeeId",
                table: "contracts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_department_budgets_DeptId",
                table: "department_budgets",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_departments_DeptCode",
                table: "departments",
                column: "DeptCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_ManagerId",
                table: "departments",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_departments_ParentDeptId",
                table: "departments",
                column: "ParentDeptId");

            migrationBuilder.CreateIndex(
                name: "IX_dependents_EmployeeId",
                table: "dependents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_allowances_AllowanceTypeId",
                table: "employee_allowances",
                column: "AllowanceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_allowances_EmployeeId",
                table: "employee_allowances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_AccountId",
                table: "employees",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_CandidateId",
                table: "employees",
                column: "CandidateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_DeptId",
                table: "employees",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_EmployeeCode",
                table: "employees",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_IdentityNumber",
                table: "employees",
                column: "IdentityNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_PositionId",
                table: "employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_SocialInsCode",
                table: "employees",
                column: "SocialInsCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_TaxCode",
                table: "employees",
                column: "TaxCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employment_history_ApprovedBy",
                table: "employment_history",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_employment_history_EmployeeId",
                table: "employment_history",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_items_HandoverRequestId",
                table: "handover_items",
                column: "HandoverRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_items_TaskId",
                table: "handover_items",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_requests_ReceiverId",
                table: "handover_requests",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_requests_RequestId",
                table: "handover_requests",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_handover_requests_SenderId",
                table: "handover_requests",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_leave_balances_LeaveTypeId",
                table: "leave_balances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_EmployeeId",
                table: "leave_requests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_LeaveTypeId",
                table: "leave_requests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_payrolls_EmployeeId",
                table: "payrolls",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_PermissionCode",
                table: "permissions",
                column: "PermissionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_positions_Title",
                table: "positions",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recruitment_requests_DeptId",
                table: "recruitment_requests",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_recruitment_requests_PositionId",
                table: "recruitment_requests",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_requests_EmployeeId",
                table: "requests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_requests_TargetDeptId",
                table: "requests",
                column: "TargetDeptId");

            migrationBuilder.CreateIndex(
                name: "IX_requests_TargetPositionId",
                table: "requests",
                column: "TargetPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_PermissionId",
                table: "role_permissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_roles_RoleName",
                table: "roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_feedbacks_ReviewerId",
                table: "task_feedbacks",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_task_feedbacks_TaskId",
                table: "task_feedbacks",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_task_progresses_TaskId",
                table: "task_progresses",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_AssignedTo",
                table: "tasks",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_DeptBudgetId",
                table: "tasks",
                column: "DeptBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_trainings_EmployeeId",
                table: "trainings",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_logs_employees_EmployeeId",
                table: "attendance_logs",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_candidates_recruitment_requests_RecruitmentRequestId",
                table: "candidates",
                column: "RecruitmentRequestId",
                principalTable: "recruitment_requests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_contracts_employees_EmployeeId",
                table: "contracts",
                column: "EmployeeId",
                principalTable: "employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_department_budgets_departments_DeptId",
                table: "department_budgets",
                column: "DeptId",
                principalTable: "departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_departments_employees_ManagerId",
                table: "departments",
                column: "ManagerId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_accounts_roles_RoleId",
                table: "accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_departments_employees_ManagerId",
                table: "departments");

            migrationBuilder.DropTable(
                name: "attendance_logs");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "configurations");

            migrationBuilder.DropTable(
                name: "contracts");

            migrationBuilder.DropTable(
                name: "dependents");

            migrationBuilder.DropTable(
                name: "employee_allowances");

            migrationBuilder.DropTable(
                name: "employment_history");

            migrationBuilder.DropTable(
                name: "handover_items");

            migrationBuilder.DropTable(
                name: "leave_balances");

            migrationBuilder.DropTable(
                name: "leave_requests");

            migrationBuilder.DropTable(
                name: "payroll_formulas");

            migrationBuilder.DropTable(
                name: "payrolls");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "task_feedbacks");

            migrationBuilder.DropTable(
                name: "task_progresses");

            migrationBuilder.DropTable(
                name: "trainings");

            migrationBuilder.DropTable(
                name: "work_shifts");

            migrationBuilder.DropTable(
                name: "allowance_types");

            migrationBuilder.DropTable(
                name: "handover_requests");

            migrationBuilder.DropTable(
                name: "leave_types");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "requests");

            migrationBuilder.DropTable(
                name: "department_budgets");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "candidates");

            migrationBuilder.DropTable(
                name: "recruitment_requests");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "positions");
        }
    }
}
