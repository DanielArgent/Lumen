﻿using System.Collections.Generic;

using Lumen.Lang.Expressions;

namespace Lumen.Lang {
	internal class Applicative : SystemClass {
		internal Applicative() {
			this.Name = "Applicative";

			this.AppendImplementation(Prelude.Functor);

			this.SetMember("-<<", new LambdaFun((scope, args) => {
				Value appl = scope["appl"];
				return appl.Type.GetMember("lift", scope).ToFunction(scope)
					.Call(scope, scope["func"], appl);
			}) {
				Name = "-<<",
				Parameters = new List<IPattern> {
					new NamePattern("appl"),
					new NamePattern("func")
				}
			});

			this.SetMember(">>-", new LambdaFun((scope, args) => {
				Value appl = scope["appl"];
				return appl.Type.GetMember("lift", scope).ToFunction(scope)
					.Call(scope, scope["func"], appl);
			}) {
				Name = ">>-",
				Parameters = new List<IPattern> {
					new NamePattern("func"),
					new NamePattern("appl")
				}
			});

			this.SetMember("map", new LambdaFun((scope, args) => {
				Value appl = scope["appl"];
				Value function = appl.Type.GetMember("pure", scope).ToFunction(scope)
					.Call(new Scope(), scope["func"]);

				return appl.Type.GetMember("lift", scope).ToFunction(scope)
					.Call(new Scope(), appl, function);
			}) {
				Name = "lift",
				Parameters = new List<IPattern> {
					new NamePattern("func"),
					new NamePattern("appl")
				}
			});
		}

		public override void OnImplement(Module target) {
			target.SetMemberIfAbsent("pure", new LambdaFun((scope, args) => {
				Prelude.FunctionIsNotImplementedForType("Applicative.pure", target);
				return Const.UNIT;
			}) {
				Parameters = new List<IPattern> {
					new NamePattern("init"),
				}
			});

			target.SetMemberIfAbsent("lift", new LambdaFun((scope, args) => {
				Prelude.FunctionIsNotImplementedForType("Applicative.lift", target);
				return Const.UNIT;
			}) {
				Parameters = new List<IPattern> {
					new NamePattern("func"),
					new NamePattern("appl")
				}
			});
		}
	}
}
