using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace DehnadWcfService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IBimeIranService
    {
        [OperationContract]
        List<UsersInfo> GetData();

        [OperationContract]
        ResultStatus ValidateDataDelivery(UsersInfo userInfo, ResultStatus deliveryStatus);
        [OperationContract]
        ResultStatus ChangeZipCode(UsersInfo userInfo);
        [OperationContract]
        FullUsersInfo GetUserInfo(UsersInfo userInfo);
    }


    // Use a data contract as illustrated in the sample below to add composite types to service operations.

    [DataContract]
    public class ResultStatus
    {
        [DataMember]
        public Enums.Status Status { get; set; }

        [DataMember]
        public string Description { get; set; }
    }

    public class UsersInfo
    {
        public DateTime DateRequested { get; set; }
        public string MobileNumber { get; set; }
        public string SocialNumber { get; set; }
        public Enums.Request Request { get; set; }
        public string ZipCode { get; set; }
        public string InsuranceCode { get; set; }
        public string Description { get; set; }
    }

    public class FullUsersInfo
    {
        public string MobileNumber { get; set; }
        public string SocialNumber { get; set; }
        public string InsuranceType { get; set; }
        public string ZipCode { get; set; }
        public string InsuranceCode { get; set; }
        public string DateInsuranceRegistered { get; set; }
        public string Description { get; set; }
    }
}
