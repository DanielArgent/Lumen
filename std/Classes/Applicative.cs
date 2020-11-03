﻿using System.Collections.Generic;

using Lumen.Lang.Expressions;

namespace Lumen.Lang {
	internal class Applicative : Module {
		internal Applicative() {
			this.Name = "Applicative";

			this.SetMember("liftA", new LambdaFun((scope, args) => {
				Prelude.FunctionIsNotImplementedForType("Applicative.liftA", scope["x"].Type, scope);
				return Const.UNIT;
			}) {
				Name = "liftA",
				Arguments = new List<IPattern> {
					new NamePattern("fn"),
					new NamePattern("fc")
				}
			});

			this.SetMember("<*>", new LambdaFun((scope, args) => {
				Value fc = scope["fc"];
				return fc.Type.GetMember("liftA", scope).ToFunction(scope).Run(scope, scope["fn"], fc);
			}) {
				Name = "<*>",
				Arguments = new List<IPattern> {
					new NamePattern("fc"),
					new NamePattern("fn")
				}
			});
		}
	}
}
