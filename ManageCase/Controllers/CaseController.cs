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

namespace ManageCase.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CaseController : ControllerBase
    {

        private readonly ILogger<CaseController> _log;
        private readonly IQueryParser _queryp;
        private readonly IKeyVaultService _keyVaultService;


        public CaseController(ILogger<CaseController> log, IQueryParser queryParser, IKeyVaultService keyVaultService) 
        {
            this._log = log;
            this._queryp = queryParser;
            this._keyVaultService = keyVaultService;
        }

        [HttpPost("CreateCase")]        
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());
                CreateCaseExecution createcaseEx = new CreateCaseExecution(this._log, this._queryp, this._keyVaultService);

                string Header_Value = string.Empty;
                if (Request.Headers.TryGetValue("appkey", out var headerValues))
                {
                    Header_Value = headerValues;
                }


                CaseReturnParam Casetatus = await createcaseEx.ValidateCreateCase(request, Header_Value);

                
                return Ok(Casetatus);
                
                    
            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }

        [HttpPost("getCaseStatus")]
        public async Task<IActionResult> getCaseStatus()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());
                CreateCaseExecution createleadEx = new CreateCaseExecution(this._log, this._queryp, this._keyVaultService);

                string Header_Value = string.Empty;
                if (Request.Headers.TryGetValue("appkey", out var headerValues))
                {
                    Header_Value = headerValues;
                }

                CaseStatusRtParam Casetatus = await createleadEx.ValidategetCaseStatus(request, Header_Value);
                return Ok(Casetatus);
            }
            catch (Exception ex)
            {

                return BadRequest();

            }

        }

        [HttpPost("getCaseList")]
        public async Task<IActionResult> getCaseList()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());
                CreateCaseExecution createleadEx = new CreateCaseExecution(this._log, this._queryp, this._keyVaultService);
                
                string Header_Value = string.Empty;
                if (Request.Headers.TryGetValue("appkey", out var headerValues))
                {
                    Header_Value = headerValues;
                }

                CaseListParam CaseList = await createleadEx.getCaseList(request, Header_Value);
                return Ok(CaseList);
            }
            catch (Exception ex)
            {

                return BadRequest();

            }

        }


    }
}
