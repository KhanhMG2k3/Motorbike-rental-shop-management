using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoRent.BusinessObjects.Constants
{
    public static class PaymentType
    {
        public const string Deposit = "Deposit";
        public const string RentalFee = "RentalFee";
        public const string Penalty = "Penalty";
        public const string Refund = "Refund";
    }
    public static class PaymentMethod
    {
        public const string Cash = "Cash";
        public const string BankTransfer = "BankTransfer";
        public const string Momo = "Momo";
        public const string VNPay = "VNPay";
    }
    public static class PaymentStatus
    {
        public const string Pending = "Pending";
        public const string Paid = "Paid";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
    }
}
