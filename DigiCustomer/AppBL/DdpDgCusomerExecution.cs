namespace CustomerLead
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
    using System.Security.Cryptography.Xml;
    using Azure;

    public class DdpDgCusomerExecution : IDdpDgCustomerExecution
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

        private string Leadid, LeadAccountid, DDEId;

        private List<string> applicents = new List<string>();
       
        private ICommonFunction _commonFunc;

        public DdpDgCusomerExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {                    
            this._logger = logger;            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
        }


        public async Task<DedupeDigiCustomerReturn> ValidateInput(dynamic RequestData)
        {
            DedupeDigiCustomerReturn ldRtPrm = new DedupeDigiCustomerReturn();
            RequestData = await this.getRequestData(RequestData, "DedupeDigiCustomer");
            try
            { 

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "DedupeDigiCustomerappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        
                        if (!string.IsNullOrEmpty(RequestData.ApplicantId.ToString()))
                        {
                            ldRtPrm = await this.getCustomerLead(RequestData.ApplicantId.ToString());
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateInput", "Input UCIC are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }
                        
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateInput", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateInput", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateInput", ex.Message);
                throw ex;
            }
            
        }


        private async Task<DedupeDigiCustomerReturn> getCustomerLead(string customerLeadID)
        {
            DedupeDigiCustomerReturn ddupdgCustomerReturn = new DedupeDigiCustomerReturn();
            try
            {
                string Token = await this._queryParser.getAccessToken();
                string RequestTemplate = "{\"searchOrUpdateDigiDedupeCustomerReq\":{\"msgHdr\":{\"channelID\":\"Posidex2023091207502836896407\",\"transactionType\":\"Posidex2023091207502836896407\",\"transactionSubType\":\"Posidex2023091207502836896407\",\"conversationID\":\"Posidex2023091207502836896407\",\"externalReferenceId\":\"Posidex2023091207502836896407\",\"authInfo\":{\"branchID\":\"1001\",\"userID\":\"Posidex\",\"token\":\"Posidex\"},\"isAsync\":false},\"msgBdy\":{\"CUSTOMER_CATEGORY\":\"I\",\"MATCHING_RULE_PROFILE\":1,\"LEAD_ID\":\"\",\"DATA_SOURCE\":\"wizard\",\"ACCOUNT_ID\":\"\",\"REQUEST_TYPE\":\"search\",\"UCIC\":\"\",\"PSX_REQUEST_ID\":\"\",\"DEMOGRAPHIC_INFORMATION\":{\"FIRST_NAME\":\"govcomp\",\"LAST_NAME\":\"\",\"MIDDLE_NAME\":\"\",\"FATHER_NAME\":\"\",\"MOTHER_NAME\":\"\",\"SPOUSE_NAME\":\"\",\"FATHER_SPOUSE_NAME\":\"\",\"DATE_OF_BIRTH\":\"\",\"DATE_OF_INC\":\"\",\"AADHAR_REFERENCE_NO\":\"\",\"PAN\":\"\",\"PASSPORT\":\"\",\"DRIVING_LICENSE\":\"\",\"TIN\":\"\",\"GSTIN\":\"\",\"VOTER_CARD_NO\":\"\",\"RATION_CARD_NO\":\"\"},\"ADDRESS_INFORMATION\":{\"ADDRESS1\":\"\",\"AREA1\":\"\",\"CITY1\":\"\",\"STATE1\":\"\",\"PINCODE1\":\"\",\"ADDRESS_TYPE1\":\"\",\"ADDRESS2\":\"\",\"AREA2\":\"\",\"CITY2\":\"\",\"STATE2\":\"\",\"PINCODE2\":\"\",\"ADDRESS_TYPE2\":\"\"},\"CONTACT_INFORMATION\":{\"CUSTOMER_CONTACT_1\":\"\",\"CUSTOMER_CONTACT_TYPE_1\":\"\",\"CUSTOMER_CONTACT_2\":\"\",\"CUSTOMER_CONTACT_TYPE_2\":\"\"}}}}";
                dynamic Request_Template = JsonConvert.DeserializeObject(RequestTemplate);

                dynamic msgBdy = Request_Template.searchOrUpdateDigiDedupeCustomerReq.msgBdy;

                var applicentDtls = await this._commonFunc.getApplicentDetail(customerLeadID);

                msgBdy.CUSTOMER_CATEGORY = await this._commonFunc.getCustomerType(applicentDtls[0]["_eqs_entitytypeid_value"].ToString());
                if (msgBdy.CUSTOMER_CATEGORY == "I")
                {
                    msgBdy.DEMOGRAPHIC_INFORMATION.FIRST_NAME = applicentDtls[0]["eqs_firstname"].ToString();
                    msgBdy.DEMOGRAPHIC_INFORMATION.LAST_NAME = applicentDtls[0]["eqs_lastname"].ToString();
                    msgBdy.DEMOGRAPHIC_INFORMATION.MIDDLE_NAME = applicentDtls[0]["eqs_middlename"].ToString();
                    msgBdy.DEMOGRAPHIC_INFORMATION.DATE_OF_BIRTH = applicentDtls[0]["eqs_dob"].ToString();
                }
                else
                {
                    msgBdy.DEMOGRAPHIC_INFORMATION.FIRST_NAME = applicentDtls[0]["eqs_companynamepart1"].ToString();
                    msgBdy.DEMOGRAPHIC_INFORMATION.LAST_NAME = applicentDtls[0]["eqs_companynamepart2"].ToString();
                    msgBdy.DEMOGRAPHIC_INFORMATION.MIDDLE_NAME = applicentDtls[0]["eqs_companynamepart3"].ToString();                    
                    msgBdy.DEMOGRAPHIC_INFORMATION.DATE_OF_INC = applicentDtls[0]["eqs_dateofincorporation"].ToString();
                }

                msgBdy.DEMOGRAPHIC_INFORMATION.PAN = applicentDtls[0]["eqs_internalpan"].ToString();
                msgBdy.DEMOGRAPHIC_INFORMATION.AADHAR_REFERENCE_NO = applicentDtls[0]["eqs_aadhaarreference"].ToString();
                msgBdy.DEMOGRAPHIC_INFORMATION.PASSPORT = applicentDtls[0]["eqs_passportnumber"].ToString();
                msgBdy.DEMOGRAPHIC_INFORMATION.VOTER_CARD_NO = applicentDtls[0]["eqs_voterid"].ToString();



                Request_Template.searchOrUpdateDigiDedupeCustomerReq.msgBdy = msgBdy;

                string postDataParametr = await EncriptRespons(JsonConvert.SerializeObject(Request_Template));
                string Lead_details = await this._queryParser.HttpCBSApiCall(Token, HttpMethod.Post, "CBSsearchOrUpdateDedupeCustomer", postDataParametr);
                //string Lead_details = "{\"searchOrUpdateDigiDedupeCustomerRes\":{\"msgHdr\":{\"result\":\"OK\"},\"msgBdy\":{\"REQUEST_ID\":\"1051065\",\"STATUS\":\"C\",\"RESPONSE_CODE\":\"200\",\"DESCRIPTION\":\"RequestProcessedSuccessfully\",\"CUSTOMER_MATCH_COUNT\":\"10\",\"EXACT_MATCH_COUNT\":\"10\",\"PROBABLE_MATCH_COUNT\":\"0\",\"SUCCESS_MESSAGE\":\"Customerinformationsearchedsuccessfully\",\"CUSTOMER_MATCHES\":{\"INDIVIDUAL_MATCHES\":[{\"NAME\":\"RAVANALANKAPRABU\",\"FATHER_NAME\":\"\",\"FATHER_SPOUSE_NAME\":\"\",\"MOTHER_NAME\":\"\",\"SPOUSE_NAME\":\"\",\"DATE_OF_BIRTH\":\"\",\"DATE_OF_INC\":\"\",\"AADHAR_REFERENCE_NO\":\"\",\"PAN\":\"EIXIX6982G\",\"PASSPORT_NO\":\"\",\"DRIVING_LICENSE_NO\":\"\",\"TIN\":\"\",\"TAN\":\"\",\"CIN\":\"\",\"DIN\":\"\",\"NREGA\":\"\",\"CKYC\":\"\",\"VOTER_CARD_NO\":\"\",\"GST_IN\":\"\",\"RATION_CARD\":\"\",\"ADDRESS_TYPE_0\":\"\",\"ADDRESS_0\":\"\",\"AREA_0\":\"\",\"CITY_0\":\"\",\"STATE_0\":\"\",\"PINCODE_0\":\"\",\"ADDRESS_TYPE_1\":\"\",\"ADDRESS_1\":\"\",\"AREA_1\":\"\",\"CITY_1\":\"\",\"STATE_1\":\"\",\"PINCODE_1\":\"\",\"ADDRESS_TYPE_2\":\"\",\"ADDRESS_2\":\"\",\"AREA_2\":\"\",\"CITY_2\":\"\",\"STATE_2\":\"\",\"PINCODE_2\":\"\",\"ADDRESS_TYPE_3\":\"\",\"ADDRESS_3\":\"\",\"AREA_3\":\"\",\"CITY_3\":\"\",\"STATE_3\":\"\",\"PINCODE_3\":\"\",\"ADDRESS_TYPE_4\":\"\",\"ADDRESS_4\":\"\",\"AREA_4\":\"\",\"CITY_4\":\"\",\"STATE_4\":\"\",\"PINCODE_4\":\"\",\"MOBILE_TYPE_0\":\"\",\"MOBILE_0\":\"\",\"MOBILE_TYPE_1\":\"\",\"MOBILE_1\":\"\",\"MOBILE_TYPE_2\":\"\",\"MOBILE_2\":\"\",\"MOBILE_TYPE_3\":\"\",\"MOBILE_3\":\"\",\"MOBILE_TYPE_4\":\"\",\"MOBILE_4\":\"\",\"RECORD_TYPE\":\"ONLINE\",\"MATCH_CRITERIA\":\"PAN\",\"IS_EXACT_MATCH\":\"True\",\"IS_PROBABLE_MATCH\":\"False\",\"MATCHED_ID\":\"12312\",\"UCIC\":\"12312\",\"LEAD_ID\":\"\",\"IND_NON_DETAILS\":\"I\"}],\"ORGANISATION_MATCHES\":[]}}}}";
                dynamic responsD = JsonConvert.DeserializeObject(Lead_details);

                if (Convert.ToInt32(responsD.searchOrUpdateDigiDedupeCustomerRes.msgBdy.EXACT_MATCH_COUNT.ToString()) > 0)
                {
                    Dictionary<string, string> odatab = new Dictionary<string, string>();
                    odatab.Add("eqs_leadstatus", await this._queryParser.getOptionSetTextToValue("lead", "eqs_leadstatus", "Not Onboarded"));
                    odatab.Add("eqs_notonboardedreason", "duplicate");
                    postDataParametr = JsonConvert.SerializeObject(odatab);
                    var LeadAccount_details = await this._queryParser.HttpApiCall($"leads({applicentDtls[0]["_eqs_leadid_value"].ToString()})", HttpMethod.Patch, postDataParametr);
                    ddupdgCustomerReturn.decideNL = true;
                }
                else
                {
                    ddupdgCustomerReturn.decideNL = false;
                }

                ddupdgCustomerReturn.Message = OutputMSG.Case_Success;
                ddupdgCustomerReturn.ReturnCode = "CRM-SUCCESS";

            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateAccountByLead", ex.Message);
                ddupdgCustomerReturn.Message = OutputMSG.Incorrect_Input;
                ddupdgCustomerReturn.ReturnCode = "CRM-ERROR-102";
            }

            return ddupdgCustomerReturn;
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
