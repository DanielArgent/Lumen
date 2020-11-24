﻿using System;
using System.Linq;
using System.Collections.Generic;

using Lumen.Lang.Expressions;
using Lumen.Lang;

namespace Lumen.Lmi {
	public class BlockE : Expression {
		public List<Expression> expressions;

		public BlockE() {
			this.expressions = new List<Expression>();
		}

		public BlockE(List<Expression> Expressions) {
			this.expressions = Expressions;
		}

		public void Add(Expression Expression) {
			this.expressions.Add(Expression);
		}

		public Value Eval(Scope e) {
			Dictionary<String, Value> savedBindings = new Dictionary<String, Value>();

			for (Int32 i = 0; i < this.expressions.Count - 1; i++) {
				if (this.expressions[i] is BindingDeclaration binding) {
					List<String> declared = binding.pattern.GetDeclaredVariables();
					foreach (var x in declared) {
						if (e.ExistsInThisScope(x)) {
							savedBindings[x] = e[x];
							e.Remove(x);
						}
					}
				}

				this.expressions[i].Eval(e);
			}

			if (this.expressions.Count > 0) {
				return this.expressions[this.expressions.Count - 1].Eval(e);
			}

			foreach (var item in savedBindings) {
				e[item.Key] = item.Value;
			}

			return Const.UNIT;
		}

		public override String ToString() {
			return String.Join(Environment.NewLine, this.expressions);

		}

		public Expression Closure(ClosureManager manager) {
			return new BlockE(this.expressions.Select(expression => expression.Closure(manager)).ToList());
		}

		public IEnumerable<Value> EvalWithYield(Scope scope) {
			for (Int32 i = 0; i < this.expressions.Count - 1; i++) {
				IEnumerable<Value> x = this.expressions[i].EvalWithYield(scope);
				foreach (Value it in x) {
					if (it is GeneratorExpressionTerminalResult) {
						continue;
					}
					yield return it;
				}
			}

			IEnumerable<Value> z = this.expressions[this.expressions.Count - 1].EvalWithYield(scope);
			foreach (Value it in z) {
				yield return it;
			}
		}
	}
}
