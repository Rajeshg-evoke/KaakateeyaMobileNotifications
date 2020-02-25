using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Kaakateeya.Customer.NotificationService.Model
{
    public class DeviceDetailsDTO
    {
        public long Cust_ID { get; set; }
        public int PlatformTypeID { get; set; }
        public string DeviceToken { get; set; }

        public string EndpointARN { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }

        public string Comments { get; set; }

    }

    public class DeleteDeviceEndPointsDTO
    {
        public string EndPoint { get; set; }
        public DataTable DTEndPoint { get; set; }
    }
}
