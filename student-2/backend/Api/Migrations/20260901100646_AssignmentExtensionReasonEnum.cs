using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class UseAssignmentExtensionReasonCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE AssignmentExtensionAutomations
                SET Reason = CASE Reason
                    WHEN 'Extension request reason 1' THEN 'UNW'
                    WHEN 'Extension request reason 2' THEN 'ACL'
                    WHEN 'Extension request reason 3' THEN 'NMT'
                    WHEN 'Extension request reason 4' THEN 'FAM'
                    WHEN 'Extension request reason 5' THEN 'CAR'
                    WHEN 'Extension request reason 6' THEN 'REL'
                    WHEN 'Extension request reason 7' THEN 'WRK'
                    WHEN 'Extension request reason 8' THEN 'TEC'
                    WHEN 'Extension request reason 9' THEN 'BRV'
                    WHEN 'Extension request reason 10' THEN 'OTH'
                    WHEN 'I''m unwell' THEN 'UNW'
                    WHEN 'I’m unwell' THEN 'UNW'
                    WHEN 'I have assignment clashes' THEN 'ACL'
                    WHEN 'I need more time to complete my assignment task' THEN 'NMT'
                    WHEN 'I have family commitments/responsibilities' THEN 'FAM'
                    WHEN 'I have had unexpected carer responsibilities' THEN 'CAR'
                    WHEN 'I have religious commitments' THEN 'REL'
                    WHEN 'I have to prioritise work' THEN 'WRK'
                    WHEN 'I have encountered a technical problem trying to submit my assignment' THEN 'TEC'
                    WHEN 'I have suffered a loss or bereavement' THEN 'BRV'
                    WHEN 'Other/Prefer not to say' THEN 'OTH'
                    WHEN 'UNW' THEN 'UNW'
                    WHEN 'ACL' THEN 'ACL'
                    WHEN 'NMT' THEN 'NMT'
                    WHEN 'FAM' THEN 'FAM'
                    WHEN 'CAR' THEN 'CAR'
                    WHEN 'REL' THEN 'REL'
                    WHEN 'WRK' THEN 'WRK'
                    WHEN 'TEC' THEN 'TEC'
                    WHEN 'BRV' THEN 'BRV'
                    WHEN 'OTH' THEN 'OTH'
                    ELSE 'OTH'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE AssignmentExtensionAutomations
                SET Reason = CASE Reason
                    WHEN 'UNW' THEN 'I''m unwell'
                    WHEN 'ACL' THEN 'I have assignment clashes'
                    WHEN 'NMT' THEN 'I need more time to complete my assignment task'
                    WHEN 'FAM' THEN 'I have family commitments/responsibilities'
                    WHEN 'CAR' THEN 'I have had unexpected carer responsibilities'
                    WHEN 'REL' THEN 'I have religious commitments'
                    WHEN 'WRK' THEN 'I have to prioritise work'
                    WHEN 'TEC' THEN 'I have encountered a technical problem trying to submit my assignment'
                    WHEN 'BRV' THEN 'I have suffered a loss or bereavement'
                    WHEN 'OTH' THEN 'Other/Prefer not to say'
                    ELSE Reason
                END;
                """);
        }
    }
}
