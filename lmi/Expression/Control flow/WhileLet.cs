﻿using System.Collections.Generic;
using Lumen.Lang;
using Lumen.Lang.Expressions;

namespace Lumen.Lmi {
	internal class WhileLet : Expression {
		private IPattern pattern;
		private Expression assinableExpression;
		private Expression body;

		public WhileLet(IPattern pattern, Expression assinableExpression, Expression body) {
			this.pattern = pattern;
			this.assinableExpression = assinableExpression;
			this.body = body;
		}

		public Value Eval(Scope scope) {
			Value value = this.assinableExpression.Eval(scope);

			while (this.pattern.Match(value, scope).Success) {
				try {
					this.body.Eval(scope);
					value = this.assinableExpression.Eval(scope);
				}
				catch (Break bs) {
					if (bs.UseLabel() > 0) {
						throw bs;
					}
					break;
				}
				catch (Next) {
					continue;
				}
			}

			return Const.UNIT;
		}

		public IEnumerable<Value> EvalWithYield(Scope scope) {
			Value value = this.assinableExpression.Eval(scope);
	
			while (this.pattern.Match(value, scope).Success) {
				IEnumerable<Value> yieldedValues;

				try {
					yieldedValues = this.body.EvalWithYield(scope);
					value = this.assinableExpression.Eval(scope);
				}
				catch (Break bs) {
					if (bs.UseLabel() > 0) {
						throw bs;
					}
					break;
				}
				catch (Next) {
					continue;
				}

				if (yieldedValues != null) {
					foreach (Value yieldedValue in yieldedValues) {
						if (yieldedValue is GeneratorTerminalResult) {
							continue;
						}

						yield return yieldedValue;
					}
				}
			}
		}

		public Expression Closure(ClosureManager manager) {
			return new WhileLet(this.pattern.Closure(manager) as IPattern,
				this.assinableExpression.Closure(manager), this.body.Closure(manager));
		}
	}
}