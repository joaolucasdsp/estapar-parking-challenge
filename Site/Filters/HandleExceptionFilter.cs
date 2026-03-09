using System.Net;
using System.Security.Cryptography;

using EstaparParkingChallenge.Site.Classes;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EstaparParkingChallenge.Site.Filters;

public class HandleExceptionFilter(
	ILogger<HandleExceptionFilter> logger
) : IExceptionFilter {

	private static readonly Action<ILogger, Exception?> logApiExceptionCaught =
		LoggerMessage.Define(LogLevel.Warning, new EventId(3000, nameof(logApiExceptionCaught)), "An API exception was caught");
	private static readonly Action<ILogger, string, Exception?> logUnhandledException =
		LoggerMessage.Define<string>(LogLevel.Error, new EventId(3001, nameof(logUnhandledException)), "Unhandled exception -- code: {ExceptionCode}");

	public void OnException(ExceptionContext context) {
		if (context.Exception is ApiException apiException) {
			logApiExceptionCaught(logger, apiException);
			context.Result = new JsonResult(apiException.Error) {
				StatusCode = (int)HttpStatusCode.UnprocessableEntity
			};
			context.ExceptionHandled = true;
		} else {
			var rawCode = new byte[3];
			using (var rng = RandomNumberGenerator.Create()) {
				rng.GetBytes(rawCode);
			}
			var code = Convert.ToHexString(rawCode);
			logUnhandledException(logger, code, context.Exception);
			var error = new {
				Message = $"Critical error. Please contact the support and provide the code: {code}",
				ExceptionCode = code,
			};
			context.Result = new JsonResult(error) {
				StatusCode = (int)HttpStatusCode.InternalServerError
			};
			context.ExceptionHandled = true;
		}
	}
}
