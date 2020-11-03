﻿using System;
using System.Collections.Generic;
using Lumen.Lang.Expressions;

namespace Lumen.Lang {
	internal sealed class FunctionModule : Module {
		internal FunctionModule() {
			this.Name = "Function";

			this.AppendImplementation(Prelude.Functor);
			this.AppendImplementation(Prelude.Applicative);

			LambdaFun RCombine = new LambdaFun((e, args) => {
				Fun m = Converter.ToFunction(e["m"], e);
				Fun f = Converter.ToFunction(e["f"], e);

				return new LambdaFun((scope, arguments) => m.Run(new Scope(scope), f.Run(new Scope(scope), arguments))) {
					Arguments = f.Arguments
				};
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("m"),
					new NamePattern("f"),
				}
			};

			LambdaFun LCombine = new LambdaFun((e, args) => {
				Fun m = Converter.ToFunction(e["fc"], e);
				Fun f = Converter.ToFunction(e["fn"], e);

				return new LambdaFun((scope, arguments) =>
				f.Run(new Scope(scope), m.Run(new Scope(scope), arguments))) {
					Arguments = m.Arguments
				};
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("fn"),
					new NamePattern("fc"),
				}
			};

			this.SetMember(Constants.PLUS, new LambdaFun((e, args) => {
				Fun m = Converter.ToFunction(e["fc"], e);
				Fun f = Converter.ToFunction(e["fn"], e);

				return new LambdaFun((scope, arguments) => {
					m.Run(new Scope(scope), arguments);
					return f.Run(new Scope(scope), arguments);
				}) {
					Arguments = m.Arguments
				};
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("fn"),
					new NamePattern("fc"),
				}
			});

			this.SetMember(Constants.STAR, new LambdaFun((e, args) => {
				Fun m = Converter.ToFunction(e["fn"], e);
				Int32 f = e["n"].ToInt(e);

				return new LambdaFun((scope, arguments) => {
					Value result = m.Run(scope, arguments);

					for (Int32 i = 1; i < f; i++) {
						result = m.Run(scope, result);
					}

					return result;
				}) {
					Arguments = m.Arguments
				};
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("fn"),
					new NamePattern("n"),
				}
			});

			// let fmap f m = fmap (m >> f)
			this.SetMember("fmap", LCombine);

			// Applicative
			this.SetMember("liftA", new LambdaFun((scope, args) => {
				Fun obj = scope["f"].ToFunction(scope);
				Fun obj2 = scope["m"].ToFunction(scope);

				return new LambdaFun((e, a) => {
					Value al = e["x'"];

					Fun z = obj2.Run(new Scope(), al).ToFunction(e);

					return z.Run(new Scope(), obj.Run(new Scope(), al));
				}) {
					Arguments = new List<IPattern> {
					new NamePattern("x'")
				}
				};
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("f"),
					new NamePattern("m"),
				}
			});

			// add >>
			// add <<
			// add +=
			// add -=
			// add +
			// add -
			// add .curry
			// add .combine
		}
	}
}
