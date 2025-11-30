using GBazaar.Models.Enums;
using System.Collections.ObjectModel;

namespace GBazaar.ViewModels.Supplier
{
    public class SupplierDashboardViewModel
    {
        public IReadOnlyList<IncomingRequestGroupViewModel> IncomingRequests { get; init; }
            = Array.Empty<IncomingRequestGroupViewModel>();

        public IReadOnlyList<ActiveOrderViewModel> ActiveOrders { get; init; }
            = Array.Empty<ActiveOrderViewModel>();

        public IReadOnlyList<AcceptedHistoryItemViewModel> AcceptedHistory { get; init; }
            = Array.Empty<AcceptedHistoryItemViewModel>();

        public SupplierPerformanceViewModel Performance { get; init; } = SupplierPerformanceViewModel.Placeholder();

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

    public class AcceptedHistoryItemViewModel
    {
        public string Reference { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateOnly? AcceptedOn { get; init; }
        public string BuyerName { get; init; } = string.Empty;
        public PaymentStatusType? PaymentStatus { get; init; }
        public POStatusType FulfillmentStatus { get; init; }
        public DateOnly? DeliveryDate { get; init; }
        public DateOnly? InvoiceDate { get; init; }
        public DateOnly? PaymentDueDate { get; init; }
        public DateOnly? PaymentDate { get; init; }
        public string InvoiceNumber { get; init; } = string.Empty;
        public decimal? InvoiceAmount { get; init; }

        public string PaymentStatusLabel => PaymentStatus?.ToString() ?? PaymentStatusType.NotPaid.ToString();
        public string FulfillmentStatusLabel => FulfillmentStatus.ToString();
    }

    public class SupplierPerformanceViewModel
    {
        public int OnTimeDeliveryPercentage { get; init; }
        public int AverageResponseHours { get; init; }
        public decimal AverageRating { get; init; }

        public static SupplierPerformanceViewModel Placeholder() => new()
        {
            OnTimeDeliveryPercentage = 92,
            AverageResponseHours = 48,
            AverageRating = 4.8m
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
