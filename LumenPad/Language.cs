﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FastColoredTextBoxNS;

namespace LumenPad {
	public class Language {
		public Dictionary<String, Style> Styles = new Dictionary<String, Style>();
		public List<(String, String)> Folding { get; private set; } = new List<(String, String)>();

		internal virtual List<AutocompleteItem> GetAutocompleteItems(TextBoxManager manager) {
			throw new NotImplementedException();
		}

		internal virtual void OnTextChanged(TextBoxManager textBoxManager) {
			throw new NotImplementedException();
		}
	}

	public class KlischeeLanguage : Language {
		public static Language Instance { get; internal set; } = new KlischeeLanguage();

		private KlischeeLanguage() {
			this.Folding.AddRange(new List<(String, String)> {
				("{", "}"),
				("#\\s*<(region)(.*?)>", "#\\s*</region>")
			});

			this.Styles.Add("\".*?[^\\\\]\"", Settings.String);
			this.Styles.Add("#.*", Settings.Comment);
			this.Styles.Add("\\[[a-zA-Z$._]+(\\(.*\\))?\\]", Settings.String);
			this.Styles.Add("\\b(raise|record|next|try|except|finally|global|break|not|self|and|is|let|module|or|xor|where|new|do|end|this|if|else|for|in|while|from|return|true|false|void|(:(\\s+)?(?<range>auto)))\\b", Settings.Keyword);
			this.Styles.Add("\\b(num|exception|seq|expando|file|str|vec|bool|map)\\b", Settings.Type);
		}

		internal override void OnTextChanged(TextBoxManager manager) {
			FastColoredTextBox textBox = manager.TextBox;

			Indent(textBox);

			textBox.Range.ClearFoldingMarkers();

			foreach ((String, String) i in this.Folding) {
				textBox.Range.SetFoldingMarkers(i.Item1, i.Item2);
			}

			textBox.Range.ClearStyle(StyleIndex.ALL);

			foreach (KeyValuePair<String, Style> i in this.Styles) {
				textBox.Range.SetStyle(i.Value, i.Key);
			}

			AddUsingAndCommentFoldingmarkers(textBox);

			//manager.DynamicItems.Clear();
			//KlischeeAnalyzer.Run(MainTextBoxManager, textBox.Text, e.ChangedRange.ToLine);

			//manager?.RebuildMenu();

			//SetDynamicHightliting(textBox);

			/*foreach (Bookmark i in textBox.Bookmarks) {
				textBox.GetLine(i.LineIndex).ClearStyle(Settings.UnactiveBreakpoint, Settings.Highliting.Type);
				textBox.GetLine(i.LineIndex).SetStyle(Settings.UnactiveBreakpoint);
			}*/

			/*if (this.previousLine != -1) {
				BreakpointOnLine(this.previousLine);
			}*/

			manager.RebuildMenu();
		}

		private void SetDynamicHightliting(FastColoredTextBox textBox) {
			foreach (Range x in textBox.GetRanges(@"\b(type)\s+(?<range>[\w_]+?)\b")) {
				textBox.Range.SetStyle(Settings.Type, @"\b" + x.Text + @"\b");
			}

			/*	foreach (Range x in textBox.GetRanges(@"\b(fun)\s+(?<range>[\w_]+?)\b")) {
					textBox.Range.SetStyle(Settings.Highliting.Function, @"\b" + x.Text + @"\b");
				}

				foreach (Range x in textBox.GetRanges(@"\b(module)\s+(?<range>[\w_]+?)\b")) {
					textBox.Range.SetStyle(Settings.Highliting.Module, @"\b" + x.Text + @"\b");
				}

				foreach (Range x in textBox.GetRanges(@"\b(var)\s+(?<range>[\w_]+?)\b")) {
					textBox.Range.SetStyle(Settings.Highliting.Variable, @"\b" + x.Text + @"\b");
				}

				foreach (Range x in textBox.GetRanges(@"\b(const)\s+(?<range>[\w_]+?)\b")) {
					textBox.Range.SetStyle(Settings.Highliting.Constant, @"\b" + x.Text + @"\b");
				}*/
		}

		private void AddUsingAndCommentFoldingmarkers(FastColoredTextBox textBox) {
			for (Int32 i = 0; i < textBox.LinesCount; i++) {
				Line line = textBox[i];

				if (Regex.IsMatch(line.Text, "^using.*", RegexOptions.Multiline)) {
					Int32 importSectionBegin = i;
					i++;

					while (Regex.IsMatch(textBox[i].Text, "\\s*using.*")) {
						i++;
					}

					Int32 importSectionEnd = i - 1;

					if (importSectionEnd - importSectionBegin > 0) {
						textBox[importSectionBegin].FoldingStartMarker = "m" + importSectionBegin;
						textBox[importSectionEnd].FoldingEndMarker = "m" + importSectionBegin;
					}
				}
				else if (Regex.IsMatch(line.Text, "^#.*", RegexOptions.Multiline)) {
					Int32 commentBegin = i;
					i++;
					try {
						while (Regex.IsMatch(textBox[i].Text, "^#.*")) {
							i++;
						}
					}
					catch {

					}
					Int32 commentEnd = i - 1;

					if (commentEnd - commentBegin > 0) {
						textBox[commentBegin].FoldingStartMarker = "m" + commentBegin;
						textBox[commentEnd].FoldingEndMarker = "m" + commentBegin;
					}
				}
			}
		}

		private void Indent(FastColoredTextBox textBox) {
			Int32 currentIndent = 0;
			Int32 lastNonEmptyLine = 0;
			Boolean identOn = false;

			for (Int32 i = 0; i < textBox.LinesCount; i++) {
				Line line = textBox[i];

				if (Regex.IsMatch(line.Text, ".*<#\\s*ident\\s*#>.*")) {
					identOn = true;
				}
				else if (Regex.IsMatch(line.Text, ".*<#\\s*!\\s*ident\\s*#>.*")) {
					identOn = false;
				}

				if (identOn) {
					Int32 spacesCount = line.StartSpacesCount;

					if (spacesCount == line.Count) {
						continue;
					}

					if (currentIndent < spacesCount) {
						textBox[lastNonEmptyLine].FoldingStartMarker = "m" + currentIndent;
					}
					else if (currentIndent > spacesCount) {
						textBox[lastNonEmptyLine].FoldingEndMarker = "m" + spacesCount;
					}

					currentIndent = spacesCount;
					lastNonEmptyLine = i;
				}
			}
		}

		internal override List<AutocompleteItem> GetAutocompleteItems(TextBoxManager manager) {
			List<AutocompleteItem> items = new List<AutocompleteItem> {
				new AutocompleteItem("let", (Int32)Images.VARIABLE, "let", "let", "let keyword"),
				new AutocompleteItem("if", (Int32)Images.VARIABLE, "if", "if", "if keyword"),
				new AutocompleteItem("else", (Int32)Images.VARIABLE, "else", "else", "else keyword"),
				new AutocompleteItem("for", (Int32)Images.VARIABLE, "for", "for", "for keyword"),
				new AutocompleteItem("while", (Int32)Images.VARIABLE, "while", "while", "while keyword"),
				new AutocompleteItem("record", (Int32)Images.VARIABLE, "record", "record", "record keyword"),

				new AutocompleteItem("[const]", (Int32)Images.VARIABLE, "[const]", "[const]", "let const()"),

				new AutocompleteItem("print", (Int32)Images.FUNCTION, "print", "fun print(...args, sep=\" \", onend=NL)", "Выводит аргументы в стандартный поток вывода"),

				new AutocompleteItem("num", (Int32)Images.TYPE, "num", "record num(): num", "[description not founded]"),
				new AutocompleteItem("str", (Int32)Images.TYPE, "str", "record str(): str", "[description not founded]"),
				new AutocompleteItem("map", (Int32)Images.TYPE, "map", "record map(): map", "[description not founded]"),
				new AutocompleteItem("bool", (Int32)Images.TYPE, "bool", "record bool(): bool", "[description not founded]"),
				new AutocompleteItem("seq", (Int32)Images.TYPE, "seq", "record seq(): seq", "[description not founded]"),
				new AutocompleteItem("nil", (Int32)Images.TYPE, "nil", "record nil(): nil", "[description not founded]"),
				new AutocompleteItem("exception", (Int32)Images.TYPE, "exception", "record exception(message): exception", "[description not founded]"),
				new AutocompleteItem("fun", (Int32)Images.TYPE, "fun", "record fun(): fun", "[description not founded]"),

				new AutocompleteItem("vec", (Int32)Images.TYPE, "vec", "record vec(...args, from=nil, to=nil, step=nil, len=nil): vec", "Стандартный тип вектор"),
				new Mod("vec.op_plus", (Int32)Images.FUNCTION, "op_plus", "fun vec.op_plus(other: seq | vec): vec", "Делает поверхностную копию вектора, добавляет other в его конец и возвращает его"),
				new Mod("vec.op_minus", (Int32)Images.FUNCTION, "op_minus", "fun vec.op_minus(other: seq | vec): vec", "Находит разность векторов как разность множеств"),

				new Mod("vec.op_ustar", (Int32)Images.FUNCTION, "op_ustar", "fun vec.op_ustar(): vec", "Возвращает длину вектора"),

				new Mod("vec.op_eql", (Int32)Images.FUNCTION, "op_eql", "fun vec.op_eql(other): bool", "Проверяет объекты на идентичность"),

				new Mod("vec.op_not_eql", (Int32)Images.FUNCTION, "op_not_eql", "fun vec.op_not_eql(other): bool", "Проверяет объекты на идентичность"),

				new Mod("vec.op_band", (Int32)Images.FUNCTION, "op_band", "fun vec.op_band(other: seq | vec)", "Находит объединение векторов как объединение множеств"),
				new Mod("vec.op_bor", (Int32)Images.FUNCTION, "op_bor", "fun vec.op_bor(other: seq | vec)", "Находит пересечение векторов как пересечение множеств"),

				new Mod("vec.op_aplus", (Int32)Images.FUNCTION, "op_aplus", "fun vec.op_aplus(other)", "[description not founded]"),

				new Mod("vec.op_geti", (Int32)Images.FUNCTION, "op_geti", "fun vec.op_geti(x: num | seq | vec | fun=nil, y=nil, z=nil)", "[description not founded]"),
				new Mod("vec.op_seti", (Int32)Images.FUNCTION, "op_seti", "fun vec.op_seti(x: num=nil)", "[description not founded]"),

				new Mod("vec.seq", (Int32)Images.FUNCTION, "seq", "fun vec.seq(): seq", "[description not founded]"),
				new Mod("vec.str", (Int32)Images.FUNCTION, "str", "fun vec.str(): str", "[description not founded]"),

				new Mod("vec.sort", (Int32)Images.FUNCTION, "sort", "fun vec.sort(by=nil): vec", "[description not founded]"),
				new Mod("vec.sort_w", (Int32)Images.FUNCTION, "sort_w", "fun vec.sort_w(by=nil): nil", "[description not founded]"),

				new Mod("vec.pop", (Int32)Images.FUNCTION, "pop", "fun vec.pop(by=nil)", "[description not founded]"),

				new Mod("vec.remove_at", (Int32)Images.FUNCTION, "remove_at", "fun vec.remove_at(index)", "[description not founded]"),
			};

			return items;
		}
	}

	public enum Images {
		FUNCTION,
		TYPE,
		VARIABLE
	}

	class Mod : AutocompleteItem {
		public Mod(String s) : base(s) { }

		public Mod(String text, Int32 imageIndex) : base(text, imageIndex) {
		}

		public Mod(String text, Int32 imageIndex, String menuText) : base(text, imageIndex, menuText) {
		}

		public Mod(String text, Int32 imageIndex, String menuText, String toolTipTitle, String toolTipText) : base(text, imageIndex, menuText, toolTipTitle, toolTipText) {
		}

		public override CompareResult Compare(String fragmentText) {
			Int32 i = fragmentText.LastIndexOf('.');
			if (i < 0) {
				return CompareResult.HIDDEN;
			}

			if (this.Text.Contains(".")) {
				if (!this.Text.StartsWith(fragmentText) && !Extended(fragmentText)) {
					return CompareResult.HIDDEN;
				}
				return CompareResult.VISIBLE;
			}

			String lastPart = fragmentText.Substring(i + 1);
			if (lastPart == "") {
				return CompareResult.VISIBLE;
			}

			if (this.Text.StartsWith(lastPart, StringComparison.InvariantCultureIgnoreCase) || Extended(lastPart)) {
				return CompareResult.VISIBLE_AND_SELECTED;
			}

			return CompareResult.HIDDEN;
		}

		public override String GetTextForReplace() {
			return this.Text;
		}
	}
}
