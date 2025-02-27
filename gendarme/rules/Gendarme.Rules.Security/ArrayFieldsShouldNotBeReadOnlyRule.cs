//
// Gendarme.Rules.Security.ArrayFieldsShouldNotBeReadOnlyRule
//
// Authors:
//	Andreas Noever <andreas.noever@gmail.com>
//
//  (C) 2008 Andreas Noever
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using Gendarme.Framework;
using Gendarme.Framework.Rocks;
using Mono.Cecil;

namespace Gendarme.Rules.Security {

	/// <summary>
	/// This rule warns if a type declares a public <c>readonly</c> array field. 
	/// Marking a field <c>readonly</c> only prevents the field from being assigned
	/// a different value, the object itself can still be changed. This means, that
	/// the elements inside the array can still be changed.
	/// </summary>
	/// <example>
	/// Bad example:
	/// <code>
	/// class Bad {
	///	public readonly string[] Array = new string[] { "A", "B" };
	/// }
	/// 
	/// HasPublicReadonlyArray obj = HasPublicReadonlyArray ();
	/// obj.Array[0] = "B"; // valid 
	/// </code>
	/// </example>
	/// <example>
	/// Good example:
	/// <code>
	/// class Good {
	///	private readonly string[] array = new string[] { "A", "B" };
	///	public string[] GetArray ()
	///	{
	///		return (string []) array.Clone();
	///	}
	/// }
	/// 
	/// string[] obj = new HasPublicReadonlyArray ().GetArray ();
	/// obj [0] = "B"; // valid, but has no effect on other users 
	/// </code>
	/// </example>

	[Problem ("This type contains read-only array(s), however elements inside the array(s) are not read-only.")]
	[Solution ("Replace the array with a method returning a clone of the array or a read-only collection.")]
	[FxCopCompatibility ("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
	public class ArrayFieldsShouldNotBeReadOnlyRule : Rule, ITypeRule {

		public RuleResult CheckType (TypeDefinition type)
		{
			// rule does not apply to interface, enumerations and delegates or to types without fields
			if (type.IsInterface || type.IsEnum || !type.HasFields || type.IsDelegate ())
				return RuleResult.DoesNotApply;

			foreach (FieldDefinition field in type.Fields) {
				//IsInitOnly == readonly
				if (field.IsInitOnly && field.IsVisible () && field.FieldType.IsArray) {
					// Medium = this will work as long as no code starts "playing" with the array values
					Runner.Report (field, Severity.Medium, Confidence.Total);
				}
			}

			return Runner.CurrentRuleResult;
		}
	}
}
