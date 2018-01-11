using BaseService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using PinPayObjects.Enumerators.Common;
using System.ServiceModel.Channels;
using PinPayObjects.BaseObjects;
using Atlas.Core.Logic;

namespace Atlas.WCF.TicketsService
{    
    public class TicketsManager :MainClass, ITicketsManager
    {
        public TicketsManager()
          : base("ATLAS_SERVICE", typeof(TicketsManager))
        {
            var lVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            ApplyVersion(lVersion);
            if (Global.Settings != null)
            {
                Global._connectionInfo = new DBConnectionInfo()
                {
                    Host = Global.Settings.AppSettingsList.Where(p => p.code.Equals("DB_HOST")).Select(p => p.value).FirstOrDefault(),
                    Catalog = Global.Settings.AppSettingsList.Where(p => p.code.Equals("DB_CATALOG")).Select(p => p.value).FirstOrDefault(),
                    User = Global.Settings.AppSettingsList.Where(p => p.code.Equals("DB_USER")).Select(p => p.value).FirstOrDefault(),
                    Password = Global.Settings.AppSettingsList.Where(p => p.code.Equals("DB_PASSWORD")).Select(p => p.value).FirstOrDefault(),
                    WinAuth = Convert.ToBoolean(Global.Settings.AppSettingsList.Where(p => p.code.Equals("DB_WINAUTH")).Select(p => p.value).FirstOrDefault())
                };
            }
        }
        private Tuple<Binding, EndpointAddress> ResolveEndPoint(string pKey)
        {
            Binding binding = null;

            var lEndPoint = System.Configuration.ConfigurationManager.AppSettings[pKey];

            var isNetTcp = lEndPoint.StartsWith("http") == false;

            if (isNetTcp)
                binding = new NetTcpBinding(SecurityMode.None);
            else
                binding = new BasicHttpBinding();
            return new Tuple<Binding, EndpointAddress>(binding, new EndpointAddress(lEndPoint));
        }

        public override void StopService()
        {
            base.Stop();
        }

        public override void StartService()
        {
            base.Start();
        }
        public override bool ResetServiceCache()
        {
            return base.ResetCache();
        }

        public override bool Ping()
        {

            bool l_result = false;
            try
            {
                var settingsBinding = ResolveEndPoint("SettingsServiceEndPoint");
                BaseService.WCFSettings.SettingsServiceClient client = new BaseService.WCFSettings.SettingsServiceClient(settingsBinding.Item1, settingsBinding.Item2);
                client.PingAsync();

                var statusBinding = ResolveEndPoint("StatusServiceEndPoint");
                BaseService.WCFStatus.StatusServiceClient clt = new BaseService.WCFStatus.StatusServiceClient(statusBinding.Item1, statusBinding.Item2);
                clt.PingAsync();

                var logic = new TicketingLogic(Global._connectionInfo);

                l_result = logic.CheckDb();
                if (!l_result)
                {
                    Dictionary<string, string> l_params = new Dictionary<string, string>();
                    l_params.Add("_connectionInfo", Newtonsoft.Json.JsonConvert.SerializeObject(Global._connectionInfo));
                    UpdateStatus(l_params, StatusLevel.Error, null, "", Convert.ToString((int)ResultCodes.RESULT_EXCEPTION));
                    Global.NLog.LogError("Exception", null);
                }
            }
            catch (Exception ex)
            {
                base.LogException("", "Ping", ex);
            }
            return l_result;
        }

        public override void SetServiceLogLevel(LogLevel p_logLevel)
        {
            base.SetLogLevel(p_logLevel);
        }
        public override string RetrieveVersion()
        {
            return base.GetVersion();
        }

        public override void SetVersion(string p_version)
        {
            base.ApplyVersion(p_version);
        }
    }
}
