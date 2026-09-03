using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edvantix.Migrations.PostgreSQL.Payments
{
    /// <inheritdoc />
    public partial class PaymentsClientAssignedKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No schema change. Tariff/StudentInvoice/InvoiceLine/PaymentConfirmation keys switched
            // from store-generated to client-assigned (ValueGeneratedNever) so EF classifies a child
            // added through an already-tracked invoice aggregate as Added, not Modified (EDX-020).
            // The Guid PK columns already carried no database default — this only updates the model
            // snapshot so the next migration diffs cleanly.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to revert — see Up.
        }
    }
}
