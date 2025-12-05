using System.ComponentModel.DataAnnotations;
using GBazaar.Models.Enums;

namespace GBazaar.ViewModels.Buyer
{
    public class BuyerProfileViewModel
    {
        public CompanySnapshotViewModel CompanySnapshot { get; init; } = new();
        public IReadOnlyList<PendingApprovalViewModel> PendingApprovals { get; init; }
            = Array.Empty<PendingApprovalViewModel>();
        public IReadOnlyList<ApprovalLadderStepViewModel> ApprovalLadder { get; init; }
            = Array.Empty<ApprovalLadderStepViewModel>();
        public IReadOnlyList<RecentlyApprovedViewModel> RecentlyApproved { get; init; }
            = Array.Empty<RecentlyApprovedViewModel>();
        // ❌ ClosedOrders property'sini çıkar - burası Buyer için, Closed Orders Supplier'da olacak
        public IReadOnlyList<InvoiceSummaryViewModel> Invoices { get; init; }
            = Array.Empty<InvoiceSummaryViewModel>();
    }

    public class CompanySnapshotViewModel
    {
        public decimal TotalBudget { get; init; }
        public decimal CommittedSpend { get; init; }
        public decimal Remaining => Math.Max(0, TotalBudget - CommittedSpend);
    }

    public class PendingApprovalViewModel
    {
        public int RequestId { get; init; }
        public string Reference { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Stage { get; init; } = string.Empty;
    }

    public class ApprovalLadderStepViewModel
    {
        public string Role { get; init; } = string.Empty;
        public decimal ThresholdPercent { get; init; }
        public string Description { get; init; } = string.Empty;
    }

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
    public class InvoiceSummaryViewModel
    {
        public int InvoiceId { get; init; }
        public int PurchaseOrderId { get; init; }
        public string Reference { get; init; } = string.Empty;
        public string InvoiceNumber { get; init; } = string.Empty;
        public string SupplierName { get; init; } = string.Empty;
        public decimal? AmountDue { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal OutstandingAmount { get; init; }
        public DateOnly? InvoiceDate { get; init; }
        public DateOnly? DueDate { get; init; }
        public DateOnly? ExpectedDelivery { get; init; }
        public PaymentStatusType? PaymentStatus { get; init; }
        public POStatusType FulfillmentStatus { get; init; }
        public int? ExistingRating { get; init; }
        public IReadOnlyList<InvoiceLineSummaryViewModel> Items { get; init; }
            = Array.Empty<InvoiceLineSummaryViewModel>();

        public string PaymentStatusLabel => PaymentStatus?.ToString() ?? PaymentStatusType.NotPaid.ToString();
        public string FulfillmentStatusLabel => FulfillmentStatus.ToString();
    }

    public class InvoiceLineSummaryViewModel
    {
        public string ItemName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal => Quantity * UnitPrice;
    }

    public class BuyerRateOrderInput
    {
        [Required]
        public int PurchaseOrderId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Range(1, 5)]
        public int RatingScore { get; set; }
    }
}