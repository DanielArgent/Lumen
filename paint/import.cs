﻿using System;

using Lumen.Lang;

public static class Main {
	public static void Import(Scope scope, String s) {
		scope.Bind("Paint", new Lumen.Lang.Libraries.Paint.PaintModule());
	}
}
