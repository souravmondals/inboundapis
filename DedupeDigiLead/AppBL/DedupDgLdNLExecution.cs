using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Collections.Generic;

namespace DedupeDigiLead
{
    public class DedupDgLdNLExecution : IDedupDgLdNLExecution
    {

        private ILoggers _logger;
        private IQueryParser _queryParser;
        public string API_Name { set
            {
                _logger.API_Name = value;
            }
        }
        public string Input_payload { set {
                _logger.Input_payload = value;
            } 
        }

        private readonly IKeyVaultService _keyVaultService;        
        
        private CommonFunction commonFunc;

        public DedupDgLdNLExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this.commonFunc = new CommonFunction(queryParser);
           
           

        }


        public async Task<dynamic> ValidateDedupDgLdNL(dynamic RequestData, string appkey, string type)
        {

            dynamic ldRtPrm = (type == "NLTR") ? new DedupDgLdNLTRReturn() : new DedupDgLdNLReturn();
            try
            {
                string LeadID = RequestData.LeadID;
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "DedupDgLdNLappkey"))
                {
                    if (!string.IsNullOrEmpty(LeadID) && LeadID != "")
                    {

                        ldRtPrm = await this.getDedupDgLdNLStatus(RequestData, type);

                    }
                    else
                    {
                        this._logger.LogInformation("ValidateFtchDgLdSts", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateFtchDgLdSts", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateFtchDgLdSts", ex.Message);
                throw ex;
            }

        }

        


        public bool checkappkey(string appkey, string APIKey)
        {
            if (this._keyVaultService.ReadSecret(APIKey) == appkey)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public async Task<dynamic> getDedupDgLdNLStatus(dynamic RequestData, string type)
        {
            dynamic ldRtPrm = (type == "NLTR") ? new DedupDgLdNLTRReturn() : new DedupDgLdNLReturn();
            return new DedupDgLdNLReturn();
        }


    }
}
