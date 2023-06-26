namespace DMSCallBack.Controllers
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
    public class DMSCallBackController : ControllerBase
    {

        private readonly IDCMCallBackExecution _dmscallBackExecution;
        private Stopwatch watch;

        public DMSCallBackController(IDCMCallBackExecution dmscallBackExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._dmscallBackExecution = dmscallBackExecution;
        }



        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _dmscallBackExecution.API_Name = "DMSCallBack";
                _dmscallBackExecution.Input_payload = request.ToString();
                DMSCallBackReturn dmsreturn = await _dmscallBackExecution.ValidateDMSInput(request);

                watch.Stop();
                dmsreturn.TransactionID = this._dmscallBackExecution.Transaction_ID;
                dmsreturn.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _dmscallBackExecution.EncriptRespons(JsonConvert.SerializeObject(dmsreturn));
                this._dmscallBackExecution.CRMLog(JsonConvert.SerializeObject(request), response, dmsreturn.ReturnCode);

                return Ok(response);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
