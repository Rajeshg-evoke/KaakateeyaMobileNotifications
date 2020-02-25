using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using Kaakateeya.Customer.NotificationService.Model;
using Dapper;
using System.Data.SqlClient;
using System.Data;
using System.Linq;

namespace Kaakateeya.Customer.NotificationService
{
    public class DataAccess
    {
        private static string ConnectionString = ConfigurationManager.ConnectionStrings["KakDbConnection"].ConnectionString;

        public static async Task<List<DeviceDetailsDTO>> GetDeviceTokenDetailsForEmptyEndpoints()
        {
            try
            {
                var param = new DynamicParameters();

                using (IDbConnection conn = new SqlConnection(ConnectionString))
                {
                    var emptyEndpoints = await conn.QueryAsync<DeviceDetailsDTO>(SpNames.GetDeviceTokenDetailsForEmptyEndpoints, param: param, commandType: CommandType.StoredProcedure);
                    return emptyEndpoints?.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        public static async Task<int> UpdateDeviceTokenEndpoint(long CustID, string DeviceToken, string EndpointARN, int PlatformTypeID, string Comment)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@CustID", CustID);
                param.Add("@DeviceToken", DeviceToken);
                param.Add("@PlatformTypeID", PlatformTypeID);
                param.Add("@EndpointARN", EndpointARN);
                param.Add("@Comment", Comment);
                param.Add("@Status", dbType: DbType.Int32, direction: ParameterDirection.Output);
                using (IDbConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.QueryAsync(SpNames.UpdateDeviceTokenEndpoint, param: param, commandType: CommandType.StoredProcedure);
                    return param.Get<int>("@Status");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<DeviceDetailsDTO>> GetDeviceTokenDetailsEndpoints()
        {
            try
            {
                var param = new DynamicParameters();

                using (IDbConnection conn = new SqlConnection(ConnectionString))
                {
                    var emptyEndpoints = await conn.QueryAsync<DeviceDetailsDTO>(SpNames.GetDeviceTokenDetailsEndpoints, param: param, commandType: CommandType.StoredProcedure);
                    return emptyEndpoints?.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<MobileNotificationMessageDTO>> GetMobileNotificationMessage()
        {
            try
            {
                var param = new DynamicParameters();

                using (IDbConnection conn = new SqlConnection(ConnectionString))
                {
                    var mobileNotificationMessage = await conn.QueryAsync<MobileNotificationMessageDTO>(SpNames.GetMobileNotificationMessage, param: param, commandType: CommandType.StoredProcedure);
                    return mobileNotificationMessage?.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<int> UpdateMobileNotificationMessagePublishStatus(int ID, long? ToCustID, string Comment)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@ToCustID", ToCustID);
                param.Add("@ID", ID);
                param.Add("@Comment", Comment);
                param.Add("@Status", dbType: DbType.Int32, direction: ParameterDirection.Output);
                using (IDbConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.QueryAsync(SpNames.UpdateMobileNotificationMessagePublishStatus, param: param, commandType: CommandType.StoredProcedure);
                    return param.Get<int>("@Status");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task DeleteDeviceEndPoints(DeleteDeviceEndPointsDTO deleteDeviceEndPointsDTO)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@tbl_Endpoints", Common.DataTableAdd(deleteDeviceEndPointsDTO.EndPoint, deleteDeviceEndPointsDTO.DTEndPoint, "EndPoint", "EndPoints"), dbType: DbType.Object);
                using (IDbConnection conn = new SqlConnection(ConnectionString))
                {
                    await conn.QueryAsync(SpNames.DeleteDeviceEndPoints, param: param, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
