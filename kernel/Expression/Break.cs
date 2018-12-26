﻿using System;
using System.Collections.Generic;

using Lumen.Lang.Std;

namespace Lumen.Lang.Expressions {
	public class Break : System.Exception, Expression {
		private Int32 label;

		public Break(Int32 label) : base("unexpected break with label") {
			this.label = label;
		}

		public Int32 UseLabel() {
			this.label--;
			return this.label;
		}

		public Value Eval(Scope e) {
			throw this;
		}

		public Expression Closure(List<String> visible, Scope thread) {
			return this;
		}

		public Expression Optimize(Scope scope) {
			return this;
		}
	}
}