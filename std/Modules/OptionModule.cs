﻿using System;
using System.Collections.Generic;
using Lumen.Lang.Expressions;

namespace Lumen.Lang {
	internal class OptionModule : Module {
		public Constructor Some { get; private set; }
		public IType None { get; private set; }

		public OptionModule() {
			this.Name = "Option";

			this.AppendImplementation(Prelude.Functor);
			this.AppendImplementation(Prelude.Applicative);

			this.Some = Helper.CreateConstructor("Option.Some", this, new List<String> { "x" }) as Constructor;
			this.None = Helper.CreateConstructor("Option.None", this, new List<String>());

			this.SetMember("Some", this.Some);
			this.SetMember("None", this.None);

			LambdaFun fmap = new LambdaFun((scope, args) => {
				Value functor = scope["fc"];

				if (functor == this.None) {
					return this.None;
				}
				else if (this.Some.IsParentOf(functor)) {
					Fun mapper = scope["fn"].ToFunction(scope);
					return Helper.CreateSome(mapper.Run(new Scope(scope), Prelude.DeconstructSome(functor)));
				}

				throw new LumenException(Exceptions.TYPE_ERROR.F(this, functor.Type));
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("fn"),
					new NamePattern("fc"),
				}
			};

			this.SetMember("fmap", fmap);

			// Applicative
			this.SetMember("liftA", new LambdaFun((scope, args) => {
				Value obj = scope["f"];

				if (obj == this.None) {
					return this.None;
				}
				else if (this.Some.IsParentOf(obj)) {
					return scope["m"].CallMethodFlip("fmap", scope, Prelude.DeconstructSome(obj));
				}

				throw new LumenException("liftA option");
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("m"),
					new NamePattern("f"),
				}
			});

			this.SetMember("liftB", new LambdaFun((scope, args) => {
				IType obj = scope["m"] as IType;

				if (obj == this.None) {
					return this.None;
				}
				else if (this.Some.IsParentOf(obj)) {
					Fun f = scope["f"] as Fun;
					return f.Run(new Scope(scope), Prelude.DeconstructSome(obj));
				}

				throw new LumenException("fmap option");
			}) {
				Arguments = new List<IPattern> {
					new NamePattern("m"),
					new NamePattern("f"),
				}
			});

			this.SetMember("String", new LambdaFun((scope, args) => {
				IType obj = scope["this"] as IType;
				if (this.Some.IsParentOf(obj)) {
					return new Text($"Some {obj.GetMember("x", scope)}");
				}
				else {
					return new Text("None");
				}

			}) {
				Arguments = new List<IPattern> {
					new NamePattern("this")
				}
			});
		}
	}
}
