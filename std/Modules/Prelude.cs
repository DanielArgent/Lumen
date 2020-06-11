﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Lumen.Lang.Expressions;

namespace Lumen.Lang {
	public sealed class Prelude : Module {
		#region Fields
		public static Module Exception { get; } = new ExceptionClass();
		public static Module Functor { get; } = new Functor();
		public static Module Collection { get; } = new Collection();
		public static Module Ord { get; } = new OrdModule();
		public static Module Format { get; } = new Format();
		public static Module Context { get; } = new Context();

		public static Module Any { get; } = new AnyModule();

		public static Module Unit { get; } = new UnitModule();

		public static Module Iterator { get; } = new Iterator();
		public static Module Ref { get; } = new StateModule();
		public static Module Stream { get; } = new StreamModule();
		public static Module Array { get; } = new ArrayModule();
		public static Module Function { get; } = new Function();
		public static Module Number { get; } = new NumberModule();
		public static Module Map { get; } = new MapModule();
		public static Module Pair { get; } = new MapModule.PairModule();
		public static Module Text { get; } = new TextModule();
		public static Module List { get; } = new ListModule();

		public static Module Boolean { get; } = new BooleanModule();

		public static IType Fail { get; private set; }
		public static Module Option { get; } = new Option();
		public static IType None { get; } = (Option as Option).None;
		public static Constructor Some { get; } = (Option as Option).Some;

		public static Module Regex { get; } = new RegexModule();

		public static Prelude Instance { get; } = new Prelude();

		#endregion
		private static readonly Random mainRandomObject = new Random();
		public static Dictionary<String, Module> GlobalImportCache { get; } = new Dictionary<String, Module>();

		private Prelude() {
			ConstructFail();

			this.SetMember("Prelude", this);

			this.SetMember("Ord", Ord);
			this.SetMember("Format", Format);
			this.SetMember("Functor", Functor);
			this.SetMember("Context", Context);

			this.SetMember("Fail", Fail);

			this.SetMember("Pair", MapModule.PairModule.ctor);

			this.SetMember("Unit", Unit);

			this.SetMember("Option", Option);
			this.SetMember("Some", (Option as Option).Some);
			this.SetMember("None", (Option as Option).None);

			this.SetMember("Iterator", Iterator);
			this.SetMember("Ref", Ref);
			this.SetMember("ref", Ref);
			this.SetMember("List", List);
			this.SetMember("Stream", Stream);
			this.SetMember("Array", Array);
			this.SetMember("Number", Number);
			this.SetMember("Boolean", Boolean);
			this.SetMember("Text", Text);
			this.SetMember("Function", Function);
			this.SetMember("Collection", Collection);

			this.SetMember("Map", Map);

			this.SetMember("true", Const.TRUE);
			this.SetMember("false", Const.FALSE);

			this.SetMember("inf", new Number(Double.PositiveInfinity));
			this.SetMember("nan", new Number(Double.NaN));

			this.SetMember("PI", (Number)Math.PI);
			this.SetMember("E", (Number)Math.E);

			this.SetMember("writeFile", new LambdaFun((scope, args) => {
				String fileName = scope["fileName"].ToString();
				String text = scope["text"].ToString();

				try {
					File.WriteAllText(fileName, text);
					return Helper.CreateSome(new Text(fileName));
				}
				catch {
					return Prelude.None;
				}
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("text"),
					new NamePattern("fileName")
				}
			});

			this.SetMember("readFile", new LambdaFun((scope, args) => {
				String fileName = scope["fileName"].ToString();

				if (File.Exists(fileName)) {
					try {
						return Helper.CreateSome(new Text(File.ReadAllText(fileName)));
					}
					catch {
						return Prelude.None;
					}
				}

				return Prelude.None;
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("fileName")
				}
			});

			this.SetMember("readLines", new LambdaFun((scope, args) => {
				String fileName = scope["fileName"].ToString();

				if (File.Exists(fileName)) {
					try {
						return new List(LinkedList.Create(File.ReadAllLines(fileName).Select(i => new Text(i))));
					}
					catch {
						return new List(LinkedList.Empty);
					}
				}

				return new List(LinkedList.Empty);
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("fileName")
				}
			});

			this.SetMember("readArray", new LambdaFun((scope, args) => {
				String fileName = scope["fileName"].ToString();

				if (File.Exists(fileName)) {
					try {
						return new Array(File.ReadAllLines(fileName).Select(i => new Text(i) as Value).ToList());
					}
					catch {
						return new Array();
					}
				}

				return new Array();
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("fileName")
				}
			});

			this.SetMember("createFile", new LambdaFun((scope, args) => {
				String fileName = scope["fileName"].ToString();
				try {
					File.Create(fileName).Close();
					return Helper.CreateSome(new Text(fileName));
				}
				catch {
					return Prelude.None;
				}
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("fileName")
				}
			});

			// 23/04
			this.SetMember("println", new LambdaFun((scope, args) => {
				Value x = scope["x"];

				Console.WriteLine(x.ToString());

				return Const.UNIT;
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("x")
				}
			});

			this.SetMember("print", new LambdaFun((scope, args) => {
				Value x = scope["x"];

				Console.Write(x.ToString());

				return Const.UNIT;
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("x")
				}
			});

			this.SetMember("read", new LambdaFun((scope, args) => {
				return new Text(Console.ReadLine());
			}));

			this.SetMember("input", new LambdaFun((scope, args) => {
				Value prompt = scope["prompt"];

				Console.Write(prompt.ToString());

				return new Text(Console.ReadLine());
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("prompt")
				}
			});

			this.SetMember("require", new LambdaFun((scope, args) => {
				String path = scope["name"].ToString() + ".dll";
				if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + path)) {
					Assembly ass = Assembly.LoadFile(AppDomain.CurrentDomain.BaseDirectory + path);
					ass.GetType("Main").GetMethod("Import").Invoke(null, new Object[] { scope.parent, "" });
					return Const.UNIT;
				}
				Console.WriteLine("FAILED");
				return Const.UNIT;
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("name")
				}
			});

			this.SetMember("sleep", new LambdaFun((scope, args) => {
				System.Threading.Thread.Sleep(scope["x"].ToInt(scope));
				return Const.UNIT;
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("x")
				}
			});

			this.SetMember("assert", new LambdaFun((scope, args) => {
				Assert(scope["condition"].ToBoolean());

				return Const.UNIT;
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("condition")
				}
			});

			this.SetMember("functionIsNotImplementedForType", new LambdaFun((scope, args) => {
				FunctionIsNotImplementedForType(scope["fName"].ToString(), scope["t"].ToString());
				return Const.UNIT;
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("fName"),
					new NamePattern("t")
				}
			});

			this.SetMember("parsen", new LambdaFun((scope, args) => {
				String str = scope["inputStr"].ToString();

				if (Double.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out var result)) {
					return Helper.CreateSome(new Number(result));
				}

				return Prelude.None;
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("inputStr")
				}
			});
		}

		private static void ConstructFail() {
			Module FailModule = new Module("_") {
			};

			FailModule.SetMember("String", new LambdaFun((scope, args) => {
				IType obj = scope["this"] as IType;
				if (obj.TryGetMember("message", out Value result)) {
					return new Text($"Failed with message '{result.ToString()}'");
				}

				throw new LumenException("failed in fail.tos");
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("this")
				}
			}, null);

			Fail = Helper.CreateConstructor("prelude.Fail", FailModule, new List<string> { "message" });
		}

		public static Value DeconstructSome(Value some, Scope scope) {
			if (Some.IsParentOf(some)) {
				Instance someInstance = some as Instance;
				return someInstance.GetField("x", scope);
			}

			return some;
		}

		public static void Assert(Boolean condition, String message = null) {
			if (!condition) {
				throw new LumenException(Exceptions.ASSERT_IS_BROKEN + (message == null ? "" : ": " + message));
			}
		}

		public static void FunctionIsNotImplementedForType(String functionName, String typeName) {
			throw new LumenException(Exceptions.FUNCTION_IS_NOT_IMPLEMENTED_FOR_TYPE.F(functionName, typeName));
		}
	}

	public class LumenIterator : BaseValueImpl, IEnumerator<Value> {
		public IEnumerator<Value> InnerValue { get; }
		public Scope Scope { get; set; }

		public override IType Type => Prelude.Iterator;

		public Value Current => this.InnerValue.Current;

		Object IEnumerator.Current => this.InnerValue.Current;

		public LumenIterator(IEnumerator<Value> innerValue) {
			this.InnerValue = innerValue;
		}

		public Value Send(Value value) {
			if (this.Scope != null) {
				this.Scope["<curr-gen-val>"] = value;
			}

			this.MoveNext();
			return this.Current;
		}

		public Value Next() {
			this.MoveNext();
			return this.Current;
		}

		public void Dispose() {
			InnerValue.Dispose();
		}

		public Boolean MoveNext() {
			return InnerValue.MoveNext();
		}

		public void Reset() {
			InnerValue.Reset();
		}
	}

	public class LumenGenerator : IEnumerable<Value> {
		public Expression generatorBody;
		public IEnumerable<Value> Value { get; set; }

		public Scope AssociatedScope { get; set; }

		public IEnumerator<Value> GetEnumerator() {
			var s = new Scope(AssociatedScope);
			return new LumenIterator(f(s).GetEnumerator()) { Scope = s };
		}

		IEnumerable<Value> f(Scope s) {
			foreach (Value i in generatorBody.EvalWithYield(s)) {
				if (i is StopIteration sit) {
					yield break;
				}
				yield return i;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			Scope s = new Scope(this.AssociatedScope);
			return new LumenIterator(this.f(s).GetEnumerator()) { Scope = s };
		}
	}
}
