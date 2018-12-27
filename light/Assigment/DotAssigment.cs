﻿using System;
using System.Collections.Generic;

using Lumen.Lang.Expressions;
using Lumen.Lang.Std;

namespace Stereotype {
	internal class DotAssigment : Expression {
		internal Expression rigth;
		internal DotExpression left;
		internal String file;
		internal Int32 line;

		internal DotAssigment(DotExpression left, Expression rigth, String file, Int32 line) {
			this.left = left;
			this.rigth = rigth;
			this.file = file;
			this.line = line;
		}

		public Value Eval(Scope e) {
			Value value = this.rigth.Eval(e);
			String name = this.left.nameVariable;
			Value obj = this.left.expression.Eval(e);
			if (obj is IObject iobj) {
				AccessModifiers mode = AccessModifiers.PUBLIC;
			/*	if ((this.left.expression is IdExpression id && id.id == "this") || (e.IsExsists("this") && e.Get("this").Type.Match(iobj))) {
					mode = AccessModifiers.PRIVATE;
				}*/

				iobj.Set(name, value, mode, e);
			}
			else if (obj is Module module) {
				module.Set(name, value);
			}
			else if (obj is Record type) {
				type.Set(name, value, e);
			}
			else if (obj is Expando hobj) {
				hobj.Set(name, value, AccessModifiers.PUBLIC, e);
			}
			else {
				type = obj.Type;
				if (type.AttributeExists("set_" + name) && type.GetAttribute("set_" + name, e) is Fun property) {
					property.Run(new Scope(e) { ["this"] = obj }, value);
				}
				else {
					throw new Lumen.Lang.Std.Exception($"object of type '{type}' does not have a field/property '{name}'", stack: e) {
						file = this.file,
						line = this.line
					};
				}
			}

			return value;
		}

		public Expression Closure(List<String> visible, Scope thread) {
			return new DotAssigment(this.left.Closure(visible, thread) as DotExpression, this.rigth.Closure(visible, thread), this.file, this.line);
		}

		public Expression Optimize(Scope scope) {
			return this;
		}

		public override String ToString() {
			return $"{this.left} = {this.rigth}";
		}
	}
}