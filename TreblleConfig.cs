using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.IO;

namespace Treblle
{
    public class TreblleConfig
    {
        String treblleApiKey;
        public String TreblleApiKey
        {
            get { return treblleApiKey; }
            set { treblleApiKey = value; }
        }

        String treblleProjectId;
        public string TreblleProjectId
        {
            get { return treblleProjectId; }
            set { treblleProjectId = value; }
        }

        public TreblleConfig()
        {
            try
            {
                NameValueCollection appSettings = WebConfigurationManager.AppSettings;

                if (appSettings.Count == 0)
                {
                    throw new Exception("Configuration is empty");
                }
                else
                {
                    TreblleApiKey = appSettings.Get("TreblleApiKey");
                    TreblleProjectId = appSettings.Get("TreblleProjectId");

                    if (string.IsNullOrEmpty(TreblleApiKey))
                    {
                        throw new Exception("TreblleApiKey not defined");
                    }

                    if (string.IsNullOrEmpty(TreblleProjectId))
                    {
                        throw new Exception("TreblleProjectId not defined");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
