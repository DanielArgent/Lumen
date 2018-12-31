﻿using System;
using System.Linq;
using System.Collections.Generic;

/*
 * Сокращаем regex, exception до минимума
 * Убираем Arguments
 * Не слишком ли много Fun?
 * HENUMERABLE
 * Добесколько интерфейсовавить н
 * Основные функции сосредоточить в ядре
 */

namespace Lumen.Lang.Std {
	internal sealed class EnumeratorType : Record {
		internal EnumeratorType() {
			this.meta = new TypeMetadata {
				Name = "Kernel.Enumerator",
			};

			IEnumerable<Value> Step(IEnumerable<Value> val, Int32 by) {
				Int32 indexer = by;
				foreach (Value i in val) {
					if (by == indexer) {
						yield return i;
						indexer = 1;
					}
					else {
						indexer++;
					}
				}
			}

			SetAttribute("step", new LambdaFun((e, args) => {
				IEnumerable<Value> v = Converter.ToIterator(e.Get("this"), e);

				return new Enumerator(Step(v, (int)Converter.ToDouble(args[0], e)));
			}));

			SetAttribute(Op.SLASH, new LambdaFun((scope, args) => {
				IEnumerable<Value> value = scope.This.ToIterator(scope);
				Fun func = scope["func"].ToFunction(scope);
				Value seed = scope["seed"];

				if (seed is Null) {
					return value.Aggregate((x, y) => func.Run(new Scope(scope), x, y));
				}
				else {
					return value.Aggregate(seed, (x, y) => func.Run(new Scope(scope), x, y));
				}
			}) {
				Arguments = new List<FunctionArgument> {
						new FunctionArgument("func"),
						new FunctionArgument("seed", Const.NULL)
					}
			});

			SetAttribute("get_enumerator", new LambdaFun((e, args) => {
				Enumerator v = ((Enumerator)e.Get("this"));

				IEnumerator<Value> enumerator = v.GetEnumerator();

				return new Expando {
					["get_next", e] = new LambdaFun((scope, argsx) => (Bool)enumerator.MoveNext()),
					["get_current", e] = new LambdaFun((scope, argsx) => enumerator.Current)
				};
			}));

			SetAttribute("to_e", new LambdaFun((e, args) => {
				Enumerator v = ((Enumerator)e.Get("this"));

				return v;
			}));

			SetAttribute("vec", new LambdaFun((e, args) => {
				Enumerator v = ((Enumerator)e.Get("this"));

				return new Vec(v.innerValue.ToList());
			}));
		}

		internal IEnumerable<Value> By(IEnumerable<Value> val, Int32 by) {
			Int32 indexer = by;
			foreach (Value i in val) {
				if (by == indexer) {
					yield return i;
					indexer = 1;
				}
				else {
					indexer++;
				}
			}
		}
	}
}