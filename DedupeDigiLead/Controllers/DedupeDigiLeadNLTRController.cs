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

namespace DedupeDigiLead.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DedupeDigiLeadNLTRController : ControllerBase
    {

        private readonly IDedupDgLdNLExecution _dedupDgLdNLExecution;

        public DedupeDigiLeadNLTRController(IDedupDgLdNLExecution dedupDgLdNLExecution)
        {
            this._dedupDgLdNLExecution = dedupDgLdNLExecution;
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
                _dedupDgLdNLExecution.API_Name = "DedupeDigiLeadNLTR";
                _dedupDgLdNLExecution.Input_payload = request.ToString();
                DedupDgLdNLTRReturn Casetatus = await _dedupDgLdNLExecution.ValidateDedupDgLdNL(request, Header_Value,"NLTR");

                return Ok(Casetatus);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
