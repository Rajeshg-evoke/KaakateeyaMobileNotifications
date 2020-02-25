using System;
using System.Collections.Generic;
using System.Text;

namespace Kaakateeya.Customer.NotificationService.Model
{
    public class MobileNotificationMessageDTO
    {
        public int ID { get; set; }
        public long? FromCustID { get; set; }
        public long? ToCustID { get; set; }
        public string MessageText { get; set; }
        public int? PublishStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string Comments { get; set; }
    }
}
