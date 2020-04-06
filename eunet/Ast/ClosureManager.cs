﻿using System;
using System.Collections.Generic;
using Argent.Xenon.Runtime;

namespace Argent.Xenon.Ast {
	public class ClosureManager {
		private readonly List<String> declared;
		public Scope Scope { get; private set; }
		public Boolean HasYield { get; set; }

		public IEnumerable<String> Declarations {
			get => this.declared;
		}

		public ClosureManager(Scope scope) {
			this.Scope = scope;
			this.declared = new List<String>();
		}

		public void Declare(String name) {
			if(!this.IsDeclared(name)) {
				this.declared.Add(name);
			}
		}

		public void Declare(IEnumerable<String> names) {
			foreach (String name in names) {
				this.Declare(name);
			}
		}

		public Boolean IsDeclared(String name) {
			return this.declared.Contains(name);
		}

		public ClosureManager Clone() {
			ClosureManager manager = new ClosureManager(this.Scope);
			manager.Declare(this.declared);
			manager.HasYield = this.HasYield;
			return manager;
		}
	}
}
