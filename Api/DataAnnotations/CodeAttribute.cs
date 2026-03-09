namespace EstaparParkingChallenge.Api.DataAnnotations {

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class CodeAttribute : Attribute {
		public CodeAttribute(string code) {
			Code = code;
		}

		public string Code { get; }
	}
}
