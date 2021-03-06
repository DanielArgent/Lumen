﻿using System;
using System.Collections.Generic;

using Lumen.Lang.Expressions;
using Lumen.Lang.Patterns;

namespace Lumen.Lang {
	public sealed class LambdaFun : Fun {
		public LumenFunc _lambda;

		public String Name { get; set; }

		public List<IPattern> Parameters { get; set; } = new List<IPattern>();

		public IType Type { get => Prelude.Function; }

		public LambdaFun(LumenFunc lambda) {
			this._lambda = lambda;
		}

		public LambdaFun(LumenFunc lambda, List<IPattern> parameters) {
			this._lambda = lambda;
			this.Parameters = parameters;
		}

		public Value Call(Scope e, params Value[] arguments) {
			// Too less given args - partial application
			if (this.Parameters?.Count > arguments.Length) {
				return Helper.MakePartial(this, arguments);
			}

			Int32 counter = 0;

			// Check function guards
			while (counter < this.Parameters.Count) {
				MatchResult match = this.Parameters[counter].Match(arguments[counter], e);
				if (match.Kind != MatchResultKind.Success) {
					LumenException exception = Helper.InvalidArgument(
							this.Parameters[counter].ToString(),
							Exceptions.FUNCTION_CAN_NOT_BE_APPLIED.F(String.Join(" ", this.Parameters)));
					exception.Note = $"Details: {match.Note}";
					throw exception;
				}
				counter++;
			}

			Value result;
			try {
				result = this._lambda(e, arguments);
			}
			catch (Return rt) {
				result = rt.Result;
			}

			Int32 delta = arguments.Length - this.Parameters.Count;
			// If given too many arguments - try to apply to result
			if (delta > 0) {
				if (this.Parameters.Count == 0 && arguments[0] == Const.UNIT) {
					return result;
				}

				Int32 position = this.Parameters.Count;
				while (position < arguments.Length) {
					Fun func = result.ToFunction(e);
					result = func.Call(e, arguments[position]);
					position++;
				}
			}

			return result;
		}

		public override String ToString() {
			return this.Name ?? $"[Function {this.GetHashCode()}]";
		}

		public String ToString(String format, IFormatProvider formatProvider) {
			return this.ToString();
		}

		public Value Clone() {
			return new LambdaFun(this._lambda) {
				Parameters = this.Parameters,
				Name = this.Name
			};
		}

		public Int32 CompareTo(Object obj) {
			throw new NotImplementedException();
		}
	}

	public class PartialFun : Value, Fun {
		public Fun InnerFunction { get; set; }
		public Value[] Args { get; set; }
		public Int32 restArgs;

		public String Name { get; set; }
		public List<IPattern> Parameters { get; set; }
		public IType Parent { get; set; }
		public IType Type => Prelude.Function;

		public Value GetField(String name, Scope scope) {
			throw new NotImplementedException();
		}

		public Boolean IsParentOf(Value value) {
			throw new NotImplementedException();
		}

		public Value Call(Scope e, params Value[] args) {
			if (this.restArgs > args.Length) {
				return new PartialFun {
					InnerFunction = this,
					Args = args,
					restArgs = this.restArgs - args.Length
				};
			}

			List<Value> vals = new List<Value>();
			vals.AddRange(args);

			for (Int32 i = 0; i < this.Args.Length; i++) {
				vals.Insert(i, this.Args[i]);
			}

			return this.InnerFunction.Call(e, vals.ToArray());
		}

		public void SetField(String name, Value value, Scope scope) {
			throw new NotImplementedException();
		}

		public override String ToString() {
			return "partial";
		}

		public Boolean TryGetField(String name, out Value result) {
			throw new NotImplementedException();
		}

		public String ToString(String format, IFormatProvider formatProvider) {
			return this.ToString();
		}

		/*public Value Clone() {
			return new PartialFun {
				Args = this.Args,
				Arguments = this.Arguments,
				InnerFunction = this.InnerFunction.Clone() as Fun,
				Name = this.Name,
				Parent = this.Parent,
				restArgs = this.restArgs
			};
		}
		*/
		public Int32 CompareTo(Object obj) {
			throw new NotImplementedException();
		}
	}
}
