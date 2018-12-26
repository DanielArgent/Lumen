﻿using System;
using System.Collections.Generic;

using StandartLibrary;
using StandartLibrary.Expressions;

namespace Stereotype {
	internal class RaiseE : Expression {
		private Expression expression;
		private String file;
		private Int32 line;

		public Expression Optimize(Scope scope) {
			return this;
		}
		public RaiseE(Expression mess, String fileName, Int32 line) {
			this.expression = mess;
			this.file = fileName;
			this.line = line;
		}

		public Value Eval(Scope e) {
			Value v = this.expression.Eval(e);

			if (v is StandartLibrary.Exception hex) {
				hex.line = this.line;
				hex.file = this.file;
				if (hex.stack == null)
					hex.stack = e;
				e.Set("$!", hex); // to global
				throw v as StandartLibrary.Exception;
			}

			StandartLibrary.Exception exception = new StandartLibrary.Exception(this.expression.Eval(e).ToString(), stack: e) {
				line = this.line,
				file = this.file
			};

			e.Set("$!", exception);
			throw exception;
		}

		public Expression Closure(List<String> visible, Scope thread) {
			return new RaiseE(this.expression?.Closure(visible, thread), this.file, this.line);
		}
	}
}