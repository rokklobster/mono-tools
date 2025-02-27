// 
// Unit tests for MarkAssemblyWithCLSCompliantRule
//
// Authors:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.Runtime.InteropServices;

using Mono.Cecil;
using Gendarme.Rules.Design;

using NUnit.Framework;
using Test.Rules.Fixtures;
using Test.Rules.Helpers;

namespace Test.Rules.Design {

	[TestFixture]
	public class MarkAssemblyWithCLSCompliantTest : AssemblyRuleTestFixture<MarkAssemblyWithCLSCompliantRule> {

		private AssemblyDefinition assembly;
		private CustomAttribute comvisible;
		private CustomAttribute clscompliant;

		[SetUp]
		public void FixtureSetUp ()
		{
			assembly = AssemblyDefinition.CreateAssembly (new AssemblyNameDefinition ("CLSCompliant", new Version ()), "CLSCompliant", ModuleKind.Dll);
			comvisible = new CustomAttribute (DefinitionLoader.GetMethodDefinition<ComVisibleAttribute> (".ctor"));
			clscompliant = new CustomAttribute (DefinitionLoader.GetMethodDefinition<CLSCompliantAttribute> (".ctor"));
			Runner.Engines.Subscribe ("Gendarme.Framework.Engines.SuppressMessageEngine");
		}

		[Test]
		public void DoesNotApply ()
		{
			// no attribute
			assembly.CustomAttributes.Clear ();
			AssertRuleDoesNotApply (assembly);
		}
		
		[Test]
		public void Good ()
		{
			assembly.CustomAttributes.Clear ();
			assembly.CustomAttributes.Add (clscompliant);
			AssertRuleSuccess (assembly);
		}

		[Test]
		public void Bad ()
		{
			assembly.CustomAttributes.Clear ();
			assembly.CustomAttributes.Add (comvisible);
			AssertRuleFailure (assembly, 1);
		}

		[Test]
		public void FxCop_GloballySuppressed ()
		{
			AssemblyDefinition assembly = DefinitionLoader.GetAssemblyDefinition (this.GetType ());
			// see GlobalSuppressions.cs
			AssertRuleDoesNotApply (assembly);
		}
	}
}
