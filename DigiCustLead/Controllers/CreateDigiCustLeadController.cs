﻿namespace DigiCustLead.Controllers
{

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.Net.Http;
    using Newtonsoft.Json;
    using System.Text;
    using System.Net.Http.Headers;
    using Newtonsoft.Json.Linq;
    using System.Reflection.PortableExecutable;
    using System.Linq;
    using Microsoft.VisualBasic;
    using Microsoft.Extensions.Caching.Memory;
    using CRMConnect;
    using System.Diagnostics;



    [Route("[controller]")]
    [ApiController]
    public class CreateDigiCustLeadController : ControllerBase
    {

        private readonly ICrDgCustLeadExecution _crDgCustLeadExecution;
        private Stopwatch watch;

        public CreateDigiCustLeadController(ICrDgCustLeadExecution crDgCustLeadExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._crDgCustLeadExecution = crDgCustLeadExecution;
        }



        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                string Header_Value = string.Empty;
                if (Request.Headers.TryGetValue("appkey", out var headerValues))
                {
                    Header_Value = headerValues;
                }

                if (Request.Headers.TryGetValue("ChannelID", out var ChannelID))
                {
                    _crDgCustLeadExecution.Channel_ID = ChannelID;
                }

                if (Request.Headers.TryGetValue("communicationID", out var communicationID))
                {
                    _crDgCustLeadExecution.Transaction_ID = communicationID;
                }

                _crDgCustLeadExecution.API_Name = "CreateDigiCustLead";
                _crDgCustLeadExecution.Input_payload = request.ToString();
                CreateCustLeadReturn Casetatus = await _crDgCustLeadExecution.ValidateCustLeadDetls(request, Header_Value);

                watch.Stop();
                Casetatus.TransactionID = this._crDgCustLeadExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _crDgCustLeadExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));
                this._crDgCustLeadExecution.CRMLog(JsonConvert.SerializeObject(request), response, Casetatus.ReturnCode);

                return Ok(response);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
