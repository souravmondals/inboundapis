﻿using Microsoft.AspNetCore.Http;
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


namespace DedupeDigiLead.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DedupeDigiCustomerNLController : ControllerBase
    {

        private readonly IDedupDgLdNLExecution _dedupDgLdNLExecution;
        private Stopwatch watch;

        public DedupeDigiCustomerNLController(IDedupDgLdNLExecution dedupDgLdNLExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._dedupDgLdNLExecution = dedupDgLdNLExecution;
        }



        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                

                _dedupDgLdNLExecution.API_Name = "DedupeDigiLeadNL";
                _dedupDgLdNLExecution.Input_payload = request.ToString();
                DedupDgLdNLReturn Casetatus = await _dedupDgLdNLExecution.ValidateDedupDgLdNL(request,"NL");

                watch.Stop();
                Casetatus.TransactionID = this._dedupDgLdNLExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";
                string response = await _dedupDgLdNLExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));

                return Ok(response);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
