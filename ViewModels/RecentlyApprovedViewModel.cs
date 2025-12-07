using GBazaar.Models.Enums;

namespace GBazaar.ViewModels.Approval
{
    public class RecentlyApprovedViewModel
    {
        public string Reference { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string FinalApprovalRole { get; init; } = string.Empty;
        public int PurchaseOrderId { get; init; }
        public POStatusType POStatus { get; init; }
        public bool HasInvoice { get; init; }
        public bool CanMakePayment { get; init; }
        public PaymentStatusType? PaymentStatus { get; init; }
    }
}