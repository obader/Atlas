using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using PinPayObjects.Enumerators.Common;
namespace Atlas.WCF.TicketsService
{

    [ServiceContract(Namespace = "http://www.pin-pay.com/schemas/Atlas")]
    public interface ITicketsManager
    {
        [BaseService.OperationBehavior]
        [OperationContract]
        [WebGet]
        bool Ping();

        [BaseService.OperationBehavior]
        [OperationContract]
        void StopService();

        [BaseService.OperationBehavior]
        [OperationContract]
        void SetVersion(string p_version);

        [BaseService.OperationBehavior]
        [OperationContract]
        [WebGet]
        string RetrieveVersion();


        [BaseService.OperationBehavior]
        [OperationContract]
        void StartService();

        [BaseService.OperationBehavior]
        [OperationContract]
        void SetServiceLogLevel(LogLevel p_logLevel);

        [BaseService.OperationBehavior]
        [OperationContract]
        [WebGet]
        bool ResetServiceCache();

    }
}
