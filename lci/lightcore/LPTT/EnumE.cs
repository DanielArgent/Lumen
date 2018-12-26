﻿using System;
using System.Collections.Generic;
using StandartLibrary;
using StandartLibrary.Expressions;

namespace Stereotype {
	internal class EnumE : Expression {
		private System.String name;
		private List<System.String> constants;

		public EnumE(System.String name, List<System.String> constants) {
			this.name = name;
			this.constants = constants;
		}
		public Expression Optimize(Scope scope) {
			return this;
		}
		public Expression Closure(List<System.String> visible, Scope scope) {
			return this;
		}

		public Value Eval(Scope e) {
			KType result = new KType {
				meta = new TypeMetadata {
					Fields = constants.ToArray(),
					Name = name
				}
			};

			result.SetAttribute("==", new LambdaFun((scope, args) => {
				EnumTypeInstance eti = scope["this"] as EnumTypeInstance;
				if (!(args[0] is EnumTypeInstance)) {
					return (Bool)false;
				}

				EnumTypeInstance other = args[0] as EnumTypeInstance;

				if(eti.Type == other.Type && eti.name == other.name) {
					return (Bool)true;
				}
				return (Bool)false;
			}));

			result.SetAttribute("!=", new LambdaFun((scope, args) => {
				EnumTypeInstance eti = scope["this"] as EnumTypeInstance;
				if (!(args[0] is EnumTypeInstance)) {
					return (Bool)true;
				}

				EnumTypeInstance other = args[0] as EnumTypeInstance;

				if (eti.Type == other.Type && eti.name == other.name) {
					return (Bool)false;
				}
				return (Bool)true;
			}));

			for (Int32 i = 0; i < constants.Count; i++) {
				result.Set(constants[i], new EnumTypeInstance(constants[i], (Num)i, result));
			}

			e.Set(name, result);
			return Const.NULL;
		}

		private class EnumTypeInstance : Value {
			internal String name;
			internal Value value;
			internal KType type;

			public EnumTypeInstance(String name, Value value, KType type) {
				this.name = name;
				this.value = value;
				this.type = type;
			}

			public KType Type => this.type;

			public Value Clone() {
				return this;
			}

			public Int32 CompareTo(Object obj) {
				throw new NotImplementedException();
			}

			public Boolean ToBool(Scope e) {
				return true;
			}

			public Double ToDouble(Scope e) {
				throw new NotImplementedException();
			}

			public String ToString(Scope e) {
				return $"{this.Type}.{this.name}";
			}
		}
	}
}