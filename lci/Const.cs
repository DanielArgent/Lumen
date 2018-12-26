﻿using System.Collections.Generic;

namespace StandartLibrary {

	/// <summary> Contains more important Kernel constants. </summary>
	public static class Const {
		/// <summary> const null </summary>
		public static Null NULL { get; } = new Null();

		/// <summary> const true </summary>
		public static Bool TRUE { get; } = new Bool(true);

		/// <summary> const false </summary>
		public static Bool FALSE { get; } = new Bool(false);

		internal static List<FunctionArgument> OTHER { get; } = new List<FunctionArgument> { new FunctionArgument("other") };
	}
}
