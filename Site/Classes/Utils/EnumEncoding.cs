using System.Reflection;

using EstaparParkingChallenge.Api.DataAnnotations;

namespace EstaparParkingChallenge.Site.Classes.Utils;

public class DuplicatedCodeException(string duplicatedCode) : Exception($"Duplicated code {duplicatedCode}") { }

public static class EnumEncoding {

	private static List<CodeAttribute> getCodeAttributes(MemberInfo memberInfo)
		=> [.. memberInfo.GetCustomAttributes(typeof(CodeAttribute), false).Cast<CodeAttribute>()];

	public static string GetCode<T>(T value) {
		if (value is null) {
			throw new ArgumentNullException(nameof(value));
		}

		var type = value.GetType();
		var memberName = value.ToString();
		if (string.IsNullOrWhiteSpace(memberName)) {
			throw new NotSupportedException($"Invalid enum value for type {type.Name}");
		}

		var members = type.GetMember(memberName);
		if (members.Length == 0) {
			throw new NotSupportedException($"Value '{memberName}' was not found in type {type.Name}");
		}

		var memberInfo = members[0];
		var codeAttributes = getCodeAttributes(memberInfo);
		if (codeAttributes.Count > 0) {
			return codeAttributes[0].Code;
		}

		throw new NotSupportedException($"{value} doesn't have a {nameof(CodeAttribute)}");
	}

	public static bool TryGetValue<T>(string code, out T result) {
		var type = typeof(T);

		if (!type.IsEnum) {
			throw new NotSupportedException($"Type {type.Name} must be an enum");
		}

		var fields = type.GetFields();
		var codeFieldDict = new Dictionary<string, FieldInfo>();

		foreach (var field in fields) {
			var codeAttributes = getCodeAttributes(field);
			if (codeAttributes.Count > 0) {
				var fieldCode = codeAttributes[0].Code;
				if (codeFieldDict.ContainsKey(fieldCode)) {
					throw new DuplicatedCodeException(fieldCode);
				}

				codeFieldDict[fieldCode] = field;
			}
		}

		if (codeFieldDict.TryGetValue(code, out var fieldInfo)) {
			var fieldName = fieldInfo.Name;
			result = (T)Enum.Parse(type, fieldName);
			return true;
		}

		result = default!;
		return false;
	}

	public static T GetValue<T>(string code) {
		if (!TryGetValue<T>(code, out var result)) {
			throw new KeyNotFoundException($"Code '{code}' not found for type {typeof(T).Name}");
		}

		return result;
	}

	public static T GetValueOrDefault<T>(string code, T defaultValue) {
		if (!TryGetValue<T>(code, out var result)) {
			return defaultValue;
		}

		return result;
	}
}
