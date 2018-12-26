﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace StandartLibrary {
	public class Range : IEnumerator<Value> {
		Value begin;
		Value end;
		Value step;
		Value current;
		Boolean flag;

		public Range(Value begin, Value end, Value step) {
			this.begin = begin;
			this.end = end;
			this.step = step;
			this.current = begin;
		}

		public Value Current {
			get => current;
		}

		Object IEnumerator.Current {
			get => current;
		}

		public void Dispose() {

		}

		public bool MoveNext() {
			Scope s = new Scope(null);
			s.This = this.current;

			if (!this.flag) {
				this.flag = true;
				return Converter.ToBoolean(((Fun)this.current.Type.GetAttribute("!=", null)).Run(s, this.end));
			}

			if (this.step == null) {
				this.current = ((Fun)this.current.Type.GetAttribute("++", null)).Run(s);
			}
			else {
				this.current = ((Fun)this.current.Type.GetAttribute("+", null)).Run(s, this.step);
			}

			return Converter.ToBoolean(((Fun)this.current.Type.GetAttribute("!=", null)).Run(s, this.end));
		}

		public void Reset() {
			current = begin;
		}
	}
}
