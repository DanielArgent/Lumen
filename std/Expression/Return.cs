﻿using System;
using System.Collections.Generic;

namespace Lumen.Lang.Expressions {
    public sealed class Return : Exception, Expression {
        private readonly Expression expression;

        public Value Result { get; private set; }

        public Return(Expression expression) {
            this.expression = expression;
        }

        public Return(Value result) {
            this.Result = result;
        }

        public Return(Value result, Expression expression) {
            this.Result = result;
            this.expression = expression;
        }

        public Value Eval(Scope e) {
            this.Result = this.expression.Eval(e);
            throw this;
        }

        public Expression Closure(List<String> visible, Scope thread) {
            return new Return(this.expression.Closure(visible, thread)) {
                Result = this.Result
            };
        }

        public override String ToString() {
            return "return " + (this.expression?.ToString() ?? this.Result?.ToString());
        }
    }
}
