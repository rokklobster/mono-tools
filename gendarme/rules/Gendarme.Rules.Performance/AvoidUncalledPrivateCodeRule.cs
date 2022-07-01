//
// Gendarme.Rules.Performance.AvoidUncalledPrivateCodeRule
//
// Authors:
//	Nidhi Rawal <sonu2404@gmail.com>
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (c) <2007> Nidhi Rawal
// Copyright (C) 2007-2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using Gendarme.Framework;
using Gendarme.Framework.Helpers;
using Gendarme.Framework.Rocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Gendarme.Rules.Performance {

	/// <summary>
	/// This rule will check for internally visible methods which are never called. 
	/// The rule will warn you if a private method isn't called in its declaring type or 
	/// if an internal method doesn't have any callers in the assembly or isn't invoked by
	/// the runtime or a delegate.
	/// </summary>
	/// <example>
	/// Bad example:
	/// <code>
	/// public class MyClass {
	///	private void MakeSuff ()
	///	{
	///		// ...
	///	}
	///	
	///	public void Method ()
	///	{
	///		Console.WriteLine ("Foo");
	///	}
	/// }
	/// </code>
	/// </example>
	/// <example>
	/// Good example (removing unused code):
	/// <code>
	/// public class MyClass {
	///	public void Method ()
	///	{
	///		Console.WriteLine ("Foo");
	///	}
	/// }
	/// </code>
	/// </example>
	/// <example>
	/// Good example (use the code):
	/// <code>
	/// public class MyClass {
	///	private void MakeSuff ()
	///	{
	///		// ...
	///	}
	///	
	///	public void Method ()
	///	{
	///		Console.WriteLine ("Foo");
	///		MakeSuff ();
	///	}
	/// }
	/// </code>
	/// </example>

	[Problem ("This private or internal (assembly-level) member does not have callers in the assembly, is not invoked by the common language runtime, and is not invoked by a delegate.")]
	[Solution ("Remove the unused code or add code to call it.")]
	[FxCopCompatibility ("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
	public class AvoidUncalledPrivateCodeRule : Rule, IMethodRule {

		static bool HasAnySpecialAttribute (ICustomAttributeProvider method)
		{
			if (!method.HasCustomAttributes)
				return false;

			foreach (CustomAttribute ca in method.CustomAttributes) {
				TypeReference cat = ca.AttributeType;
				string name = cat.Name;
				if ((cat.IsNamed ("System.Diagnostics", name)) || (cat.Namespace == "System.Runtime.InteropServices" && 
					(name == "ComRegisterFunctionAttribute" || name == "ComUnregisterFunctionAttribute"))) {
					return true;
				}
			}
			return false;
		}

		static private bool Applicable (MethodDefinition method)
		{
			// rule doesn't apply to static ctor (called by the runtime)
			if (method.IsStatic && method.IsConstructor)
				return false;

			// don't consider the compiler generated add / remove on events
			if (((method.IsAddOn || method.IsRemoveOn) && method.IsSynchronized))
				return false;

			// rule doesn't apply if the method is the assembly entry point or Main
			if (method.IsEntryPoint () || method.IsMain ())
				return false;

			// rule doesn't apply if the method is generated by the compiler or by a tool
			if (method.IsGeneratedCode ())
				return false;

			// does not apply if the method is used to register/unregister COM objects
			// or it is decorated with a [Conditional("x")] attribute
			if (HasAnySpecialAttribute (method))
				return false;

			return true;
		}

		public RuleResult CheckMethod (MethodDefinition method)
		{
			// check if the the rule applies to this method
			if (!Applicable (method))
				return RuleResult.DoesNotApply;

			// we can't be sure if this code won't be reached indirectly
			if (method.IsVirtual && !method.IsFinal)
				return RuleResult.Success;

			// if the method is visible outside the assembly
			if (method.IsVisible ())
				return RuleResult.Success;

			// check if the method is private 
			if (method.IsPrivate) {
				if (!CheckPrivateMethod (method)) {
					Runner.Report (method, Severity.High, Confidence.Normal, "The private method code is not used in its declaring type.");
					return RuleResult.Failure;
				}
				return RuleResult.Success;
			}

			if (method.IsPublic && CheckPublicMethod (method))
				return RuleResult.Success;

			if (method.IsAssembly && CheckInternalMethod (method))
				return RuleResult.Success;

			// internal methods and visible methods (public or protected) inside a non-visible type
			// needs to be checked if something in the assembly is using this method
			bool need_to_check_assembly = (method.IsAssembly || 
				((method.IsPublic || method.IsFamily) && !method.DeclaringType.IsVisible ()));

			if (!need_to_check_assembly || CheckAssemblyForMethodUsage (method))
				return RuleResult.Success;

			// method is unused and unneeded
			Runner.Report (method, Severity.High, Confidence.Normal, "The method is not visible outside its declaring assembly, nor used within.");
			return RuleResult.Failure;
		}

		public override void TearDown ()
		{
			// reusing the cache (e.g. the wizard) is not a good thing if an exception
			// occured while building it (future analysis results would be bad)
			cache.Clear ();
			base.TearDown ();
		}

		private static bool CheckPrivateMethod (MethodDefinition method)
		{
			// it's ok for have unused private ctor (and common before static class were introduced in 2.0)
			// this also covers private serialization constructors
			if (method.IsConstructor)
				return true;

			// it's ok (used or not) if it's required to implement explicitely an interface
			if (method.HasOverrides)
				return true;

			TypeDefinition type = (method.DeclaringType as TypeDefinition);

			// then we must check if this type use the private method
			if (CheckTypeForMethodUsage (type, method))
				return true;

			// then we must check if this type's nested types (if any) use the private method
			if (!type.HasNestedTypes)
				return false;

			foreach (TypeDefinition nested in type.NestedTypes) {
				if (CheckTypeForMethodUsage (nested, method))
					return true;
			}

			// report if the private method is uncalled
			return false;
		}

		// note: we need to be consistant with some stuff we propose in other rules
		private static bool CheckPublicMethod (MethodDefinition method)
		{
			// handle things like operators - but not properties
			if (method.IsSpecialName && !method.IsProperty ())
				return true;
			
			// handle non-virtual Equals, e.g. Equals(type)
			string name = method.Name;
			if (method.HasParameters && (name == "Equals")) {
				IList<ParameterDefinition> pdc = method.Parameters;
				if ((pdc.Count == 1) && (pdc [0].ParameterType == method.DeclaringType))
					return true;
			}

			// check if this method is needed to satisfy an interface
			TypeDefinition type = (method.DeclaringType as TypeDefinition);
			if (type.HasInterfaces) {
				foreach (InterfaceImplementation tr in type.Interfaces) {
					TypeDefinition intf = tr.InterfaceType.Resolve ();
					if (intf != null) {
						foreach (MethodReference member in intf.Methods) {
							if (name == member.Name)
								return true;
						}
					}
				}
			}
			return false;
		}

		private static bool CheckInternalMethod (MethodReference method)
		{
			// internal ctor for serialization are ok
			return MethodSignatures.SerializationConstructor.Matches (method);
		}

		private static bool CheckAssemblyForMethodUsage (MethodReference method)
		{
			// scan each module in the assembly that defines the method
			AssemblyDefinition assembly = method.DeclaringType.Module.Assembly;
			foreach (ModuleDefinition module in assembly.Modules) {
				// scan each type
				foreach (TypeDefinition type in module.GetAllTypes ()) {
					if (CheckTypeForMethodUsage (type, method))
						return true;
				}
			}
			return false;
		}

		static Dictionary<TypeDefinition, HashSet<ulong>> cache = new Dictionary<TypeDefinition, HashSet<ulong>> ();

		private static ulong GetToken (MethodReference method)
		{
			return (ulong) method.DeclaringType.Module.Assembly.GetHashCode () << 32 | method.GetElementMethod ().MetadataToken.ToUInt32 ();
		}

		private static bool CheckTypeForMethodUsage (TypeDefinition type, MethodReference method)
		{
			if (type.HasGenericParameters)
				type = type.GetElementType ().Resolve ();

			HashSet<ulong> methods = GetCache (type);
			if (methods.Contains (GetToken (method)))
				return true;

			MethodDefinition md = method.Resolve ();
			if ((md != null) && md.HasOverrides) {
				foreach (MethodReference mr in md.Overrides) {
					if (methods.Contains (GetToken (mr)))
						return true;
				}
			}
			return false;
		}

		private static HashSet<ulong> GetCache (TypeDefinition type)
		{
			HashSet<ulong> methods;
			if (!cache.TryGetValue (type, out methods)) {
				methods = new HashSet<ulong> ();
				cache.Add (type, methods);
				if (type.HasMethods) {
					foreach (MethodDefinition md in type.Methods) {
						if (!md.HasBody)
							continue;
						BuildMethodUsage (methods, md);
					}
				}
			}
			return methods;
		}

		private static void BuildMethodUsage (ISet<ulong> methods, MethodDefinition method)
		{
			foreach (Instruction ins in method.Body.Instructions) {
				MethodReference mr = (ins.Operand as MethodReference);
				// avoid CallSite - but do not limit ourselves to Call[virt]+Newobj (e.g. ldftn)
				if ((mr == null) || (ins.OpCode.Code == Code.Calli))
					continue;

				TypeReference type = mr.DeclaringType;
				if (!type.IsArray) {
					// if (type.GetElementType ().HasGenericParameters)
					// the simpler ^^^ does not work under Mono but works on MS
					type = type.Resolve ();
					if (type != null && type.HasGenericParameters) {
						methods.Add (GetToken (type.GetMethod (mr.Name)));
					}
				}
				methods.Add (GetToken (mr));
			}
		}
	}
}

