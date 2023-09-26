﻿namespace DMSCallBack
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Xml.Linq;
    using CRMConnect;
    using System.Xml;
    using System.Threading.Channels;
    using System.ComponentModel;
    using System;
    using System.Globalization;
    using Azure.Core;

    public class DownloadDigiDocExecution : IDownloadDigiDocExecution
    {

        private ILoggers _logger;
        private IQueryParser _queryParser;

        public string Channel_ID
        {
            set
            {
                _logger.Channel_ID = value;
            }
            get
            {
                return _logger.Channel_ID;
            }
        }
        public string Transaction_ID
        {
            set
            {
                _logger.Transaction_ID = value;
            }
            get
            {
                return _logger.Transaction_ID;
            }
        }

        public string appkey { set; get; }

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

                
        private ICommonFunction _commonFunc;

        public DownloadDigiDocExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
           
        }


        public async Task<DMSCallBackReturn> ValidateDownloadDocInput(dynamic RequestData)
        {
            DMSCallBackReturn ldRtPrm = new DMSCallBackReturn();
            RequestData = await this.getRequestData(RequestData, "DownloadDigiDocument");
           
            try
            {
                var DocumentData = await this._commonFunc.getDocumentData(RequestData.requestId.ToString()); 
                     

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "DownloadDigiDocumentappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if (DocumentData.Count > 0)
                        {
                             
                            ldRtPrm = await this.ExecDownloadDigiDoc(DocumentData, RequestData);
                            
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateDownloadDocInput", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateDownloadDocInput", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateDownloadDocInput", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }

               
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateDMSInput", ex.Message);
                this._logger.LogInformation("ValidateDownloadDocInput", "Input parameters are incorrect");
                ldRtPrm.ReturnCode = "CRM-ERROR-101";
                ldRtPrm.Message = OutputMSG.Incorrect_Input;
            }

            return ldRtPrm;
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

       

        public async Task<DMSCallBackReturn> ExecDownloadDigiDoc(dynamic Documentdetails, dynamic succData)
        {
            DMSCallBackReturn csRtPrm = new DMSCallBackReturn();
            try
            {
                Dictionary<string, string> DataMapping = new Dictionary<string, string>();
                Dictionary<string, JObject> DataMapping1 = new Dictionary<string, JObject>();

                DataMapping.Add("filename", succData.docNm.ToString());
                DataMapping.Add("documentbody", succData.bsPyld.ToString());
                
                DataMapping.Add("objectid_eqs_leaddocument@odata.bind", $"eqs_leaddocuments({Documentdetails[0].eqs_leaddocumentid.ToString()})");
               

                string postDataParametr = JsonConvert.SerializeObject(DataMapping);
                
                var document_details = await this._queryParser.HttpApiCall($"annotations()?$select=annotationid", HttpMethod.Post, postDataParametr);

                if (document_details.Count > 0)
                {
                    dynamic respons_code = document_details[0];
                    if (respons_code.responsecode == 201)
                    {
                        csRtPrm.Documentid = succData.requestId.ToString();
                        csRtPrm.ReturnCode = "CRM - SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }
                    else
                    {
                        this._logger.LogInformation("ExecDownloadDigiDoc", "Input parameters are incorrect");
                        csRtPrm.ReturnCode = "CRM-ERROR-102";
                        csRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ExecDownloadDigiDoc", "Input parameters are incorrect");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                }

            }
            catch (Exception ex)
            {
                this._logger.LogError("ExecDownloadDigiDoc", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }

            return csRtPrm;
        }

       


        public async Task CRMLog(string InputRequest, string OutputRespons, string CallStatus)
        {
            Dictionary<string, string> CRMProp = new Dictionary<string, string>();
            CRMProp.Add("eqs_name", this.Transaction_ID);
            CRMProp.Add("eqs_requestbody", InputRequest);
            CRMProp.Add("eqs_responsebody", OutputRespons);
            CRMProp.Add("eqs_requeststatus", (CallStatus.Contains("ERROR")) ? "615290001" : "615290000");
            string postDataParametr = JsonConvert.SerializeObject(CRMProp);
            await this._queryParser.HttpApiCall("eqs_apilogs", HttpMethod.Post, postDataParametr);
        }

        public async Task<string> EncriptRespons(string ResponsData)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID);
        }

        private async Task<dynamic> getRequestData(dynamic inputData, string APIname)
        {

            dynamic rejusetJson;

            var EncryptedData = inputData.req_root.body.payload;
            string xmlData = await this._queryParser.PayloadDecryption(EncryptedData.ToString());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData);
            string xpath = "PIDBlock/payload";
            var nodes = xmlDoc.SelectSingleNode(xpath);
            foreach (XmlNode childrenNode in nodes)
            {
                JObject rejusetJson1 = (JObject)JsonConvert.DeserializeObject(childrenNode.Value);

                dynamic payload = rejusetJson1[APIname];

                this.appkey = payload.msgHdr.authInfo.token.ToString();
                this.Transaction_ID = payload.msgHdr.conversationID.ToString();
                this.Channel_ID = payload.msgHdr.channelID.ToString();

                rejusetJson = payload.msgBdy;

                return rejusetJson;

            }

            return "";

        }

    }
}
