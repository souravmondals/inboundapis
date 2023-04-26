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
            this.commonFunc = new CommonFunction(queryParser, logger);
           
           

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
            JArray NLTR_data;
            try
            {
                var Lead_data = await this.commonFunc.getLeadData(RequestData.LeadID.ToString());
                if (Lead_data.Count > 0)
                {
                    dynamic LeadData = Lead_data[0];

                    if (type == "NLTR")
                    {
                        NLTR_data = await this.commonFunc.getNLTRData(LeadData.fullname.ToString());
                    }
                    else
                    {
                        NLTR_data = await this.commonFunc.getNLData(LeadData.fullname.ToString());
                    }

                    if (NLTR_data.Count > 0)
                    {
                        int dedup = 0;
                        foreach (var nld in NLTR_data)
                        {                            
                            if (type == "NLTR")
                            {
                                if (LeadData.eqs_passportnumber.ToString() == nld["eqs_passports"].ToString())
                                {
                                    dedup = 1;
                                }
                                if (LeadData.eqs_internalpan.ToString() == nld["eqs_pan"].ToString())
                                {
                                    dedup = 1;
                                }
                                if (LeadData.eqs_aadhar.ToString() == nld["eqs_aadhaar"].ToString())
                                {
                                    dedup = 1;
                                }
                                if (LeadData.eqs_cinnumber.ToString() == nld["eqs_cin"].ToString())
                                {
                                    dedup = 1;
                                }
                                if (LeadData.eqs_dob.ToString() == nld["eqs_dob"].ToString())
                                {
                                    dedup = 1;
                                }                                
                            }
                            else if (type == "NL")
                            {
                                if (LeadData.eqs_passportnumber.ToString() == nld["eqs_passport"].ToString())
                                {
                                    dedup = 1;
                                }
                                if (LeadData.eqs_internalpan.ToString() == nld["eqs_pan"].ToString())
                                {
                                    dedup = 1;
                                }
                                if (LeadData.eqs_aadhar.ToString() == nld["eqs_aadhaar"].ToString())
                                {
                                    dedup = 1;
                                }
                                if (LeadData.eqs_cinnumber.ToString() == nld["eqs_cin"].ToString())
                                {
                                    dedup = 1;
                                }
                                if (LeadData.eqs_dob.ToString() == nld["eqs_doiordob"].ToString())
                                {
                                    dedup = 1;
                                }
                            }

                            if (dedup==1)
                            {
                                break;
                            }
                        }

                        if (dedup == 1)
                        {
                            if (type == "NLTR")
                            {
                                ldRtPrm.decideNLTR = true;
                            }
                            else if (type == "NL")
                            {
                                ldRtPrm.decideNL = true;
                            }
                        }                                               
                        else
                        {
                            ldRtPrm.decideNLTR = false;
                        }
                        ldRtPrm.ReturnCode = "CRM - SUCCESS";
                        ldRtPrm.Message = "";
                    }
                    else
                    {
                        
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Resource_n_Found;
                    }

                    
                }
            }
            catch (Exception ex)
            {
                
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = OutputMSG.Resource_n_Found;
            }
            
                return ldRtPrm;
        }


    }
}
