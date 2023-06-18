namespace GFSProduct.Controllers
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
    public class FetchGFSProductlistController : ControllerBase
    {

        private readonly IFetchGFSProductExecution _fetchGFSProductExecution;
        private Stopwatch watch;

        public FetchGFSProductlistController(IFetchGFSProductExecution fetchGFSProductExecution)
        {
            watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._fetchGFSProductExecution = fetchGFSProductExecution;
        }



        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                
                _fetchGFSProductExecution.API_Name = "FetchGFSProductlist";
                _fetchGFSProductExecution.Input_payload = request.ToString();
                GFSProducrListReturn Casetatus = await _fetchGFSProductExecution.ValidateProductInput(request);

                watch.Stop();
                Casetatus.TransactionID = this._fetchGFSProductExecution.Transaction_ID;
                Casetatus.ExecutionTime = watch.ElapsedMilliseconds.ToString() + " ms";

                string response = await _fetchGFSProductExecution.EncriptRespons(JsonConvert.SerializeObject(Casetatus));
                this._fetchGFSProductExecution.CRMLog(JsonConvert.SerializeObject(request), response, Casetatus.ReturnCode);

                return Ok(response);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
