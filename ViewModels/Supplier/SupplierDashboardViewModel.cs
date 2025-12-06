using GBazaar.Models.Enums;

namespace GBazaar.ViewModels.Supplier
{
    public class SupplierDashboardViewModel
    {
        public IReadOnlyList<IncomingRequestGroupViewModel> IncomingRequests { get; init; }
            = Array.Empty<IncomingRequestGroupViewModel>();

        public IReadOnlyList<ActiveOrderViewModel> ActiveOrders { get; init; }
            = Array.Empty<ActiveOrderViewModel>();

        public IReadOnlyList<ClosedOrderViewModel> ClosedOrders { get; init; }
            = Array.Empty<ClosedOrderViewModel>();

        public SupplierPerformanceViewModel Performance { get; init; } = SupplierPerformanceViewModel.Default();

        public IReadOnlyList<RevenueSliceViewModel> RevenueMix { get; init; } = Array.Empty<RevenueSliceViewModel>();
    }

    public class IncomingRequestGroupViewModel
    {
        public int BuyerId { get; init; }
        public string BuyerName { get; init; } = string.Empty;
        public IReadOnlyList<IncomingRequestItemViewModel> Requests { get; init; }
            = Array.Empty<IncomingRequestItemViewModel>();
    }

    public class IncomingRequestItemViewModel
    {
        public int RequestId { get; init; }
        public string Reference { get; init; } = string.Empty;
        public decimal EstimatedTotal { get; init; }
        public DateTime DateSubmitted { get; init; }
        public PRStatusType Status { get; init; }
        public DateOnly? NeededBy { get; init; }

        public string StatusLabel => Status.ToString();
    }

    public class ActiveOrderViewModel
    {
        public int PurchaseOrderId { get; init; }
        public string Reference { get; init; } = string.Empty;
        public DateOnly? EstimatedDeliveryDate { get; init; }
        public PaymentStatusType? PaymentStatus { get; init; }
        public POStatusType FulfillmentStatus { get; init; }
        public decimal TotalAmount { get; init; }
        public bool IsDelivered { get; init; }
        public bool IsPaymentComplete { get; init; }

        public string PaymentStatusLabel => PaymentStatus?.ToString() ?? PaymentStatusType.NotPaid.ToString();
        public string FulfillmentStatusLabel => FulfillmentStatus.ToString();
    }

    public class ClosedOrderViewModel
    {
        public string Reference { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string BuyerName { get; init; } = string.Empty;
        public DateOnly? AcceptedOn { get; init; }
        public DateTime? CompletedDate { get; init; }
        public string InvoiceNumber { get; init; } = string.Empty;
        public DateOnly? InvoiceDate { get; init; }
        public PaymentStatusType? PaymentStatus { get; init; }
        public POStatusType FulfillmentStatus { get; init; }
        public int PurchaseOrderId { get; init; }
        public int? InvoiceId { get; init; }
        public DateOnly? PaymentDate { get; init; }
        public DateOnly? DeliveryDate { get; init; }
        public DateOnly? PaymentDueDate { get; init; }
        public decimal? InvoiceAmount { get; init; }

        public string PaymentStatusLabel => PaymentStatus?.ToString() ?? PaymentStatusType.NotPaid.ToString();
        public string FulfillmentStatusLabel => FulfillmentStatus.ToString();
    }

        public class SupplierPerformanceViewModel
    {
        public decimal AverageRating { get; init; }
        public int TotalRatings { get; init; }

       
        public static SupplierPerformanceViewModel Default() => new()
        {
            AverageRating = 0.0m,
            TotalRatings = 0
        };

       
        public static SupplierPerformanceViewModel Create(decimal averageRating, int totalRatings) => new()
        {
            AverageRating = averageRating,
            TotalRatings = totalRatings
        };
    }

    public class RevenueSliceViewModel
    {
        public string Label { get; init; } = string.Empty;
        public decimal Amount { get; init; }
    }

    public class SupplierBulkDecisionInput
    {
        public string Decision { get; set; } = string.Empty;
        public int[] SelectedRequestIds { get; set; } = Array.Empty<int>();
    }
}