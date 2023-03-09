using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OData.UriParser;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace DVPP.OdataConvert
{
	public static class ODataConvert
	{
		[FunctionName("FilterData")]
		public static async Task<IActionResult> Run(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
			[FromQuery(Name = "$filter")] string filterExpression)
		{
			string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
			var requestData = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestBody);

			// Extract the data array from the request data
			var dataArray = (List<object>)requestData["data"];

			// Parse the OData filter from the URL
			var urlFilter = string.IsNullOrWhiteSpace(filterExpression) ? null : new FilterQueryOption(filterExpression, null, null, new ODataQueryContext(new ODataQueryOptionParser(new ODataQueryOptions(new ODataQueryOptionsParser(), new Microsoft.AspNetCore.Http.DefaultHttpContext())), typeof(Dictionary<string, object>), new ODataPath()));

			// Create an OData query context and apply the filters
			var queryContext = new ODataQueryContext(new ODataQueryOptionParser(new ODataQueryOptions(new ODataQueryOptionsParser(), new Microsoft.AspNetCore.Http.DefaultHttpContext())), typeof(Dictionary<string, object>), new ODataPath());
			var odataFilter = new FilterQueryOption("age ge 30", null, null, queryContext);
			var filteredDataArray = odataFilter.ApplyTo(dataArray.AsQueryable()) as IQueryable<object>;
			if (urlFilter != null)
			{
				filteredDataArray = urlFilter.ApplyTo(filteredDataArray) as IQueryable<object>;
			}

			// Serialize the filtered data back to JSON and return it
			var filteredJsonObject = new Dictionary<string, object>();
			filteredJsonObject["data"] = filteredDataArray.ToList();
			var filteredJson = JsonConvert.SerializeObject(filteredJsonObject);
			return new OkObjectResult(filteredJson);
		}
	}
}
