//
// Unit tests for DoNotPrefixValuesWithEnumNameRule
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

using System;
using System.Reflection;

using Gendarme.Framework;
using Gendarme.Rules.Naming;
using Gendarme.Framework.Rocks;
using Mono.Cecil;

using NUnit.Framework;
using Test.Rules.Helpers;

namespace Test.Rules.Naming {

	[TestFixture]
	public class DoNotPrefixValuesWithEnumNameTest {

		private DoNotPrefixValuesWithEnumNameRule rule;
		private AssemblyDefinition assembly;
		private TypeDefinition type;
		private TestRunner runner;

		[SetUp]
		public void FixtureSetUp ()
		{
			string unit = Assembly.GetExecutingAssembly ().Location;
			assembly = AssemblyDefinition.ReadAssembly (unit);
			type = assembly.MainModule.GetType ("Test.Rules.Naming.DoNotPrefixValuesWithEnumNameTest");
			rule = new DoNotPrefixValuesWithEnumNameRule ();
			runner = new TestRunner (rule);
		}

		private TypeDefinition GetTest (string name)
		{
			foreach (TypeDefinition nestedType in type.NestedTypes) {
				if (nestedType.Name == name)
					return nestedType;
			}
			return null;
		}


		struct NonEnum {
			int NonEnum1, NonEnum2;
		}

		[Test]
		public void TestNonEnum ()
		{
			TypeDefinition type = GetTest ("NonEnum");
			Assert.AreEqual (RuleResult.DoesNotApply, runner.CheckType (type), "RuleResult");
			Assert.AreEqual (0, runner.Defects.Count, "Count");
		}


		enum FalsePositive {
			A,
			B,
			C
		}

		[Test]
		public void TestFalsePositive ()
		{
			TypeDefinition type = GetTest ("FalsePositive");
			Assert.AreEqual (RuleResult.Success, runner.CheckType (type), "RuleResult");
			Assert.AreEqual (0, runner.Defects.Count, "Count");
		}


		enum SameName {
			A,
			B,
			C,
			SameName
		}

		[Test]
		public void TestSameName ()
		{
			TypeDefinition type = GetTest ("SameName");
			Assert.AreEqual (RuleResult.Failure, runner.CheckType (type), "RuleResult");
			Assert.AreEqual (1, runner.Defects.Count, "Count");
		}


		enum Prefix {
			A,
			PREfiXB,
			C
		}

		[Test]
		public void TestReserved ()
		{
			TypeDefinition type = GetTest ("Prefix");
			Assert.AreEqual (RuleResult.Failure, runner.CheckType (type), "RuleResult");
			Assert.AreEqual (1, runner.Defects.Count, "Count");
		}
	}
}
