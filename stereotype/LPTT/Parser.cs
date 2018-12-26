﻿using System;
using System.Collections.Generic;
using System.Text;

using Lumen.Lang.Expressions;
using Lumen.Lang.Std;
namespace Stereotype {
	internal sealed partial class Parser {
		private List<Token> tokens;
		private Int32 size;
		private Int32 position;
		private Int32 line;
		private String fileName;

		internal Parser(List<Token> Tokens) {
			this.tokens = Tokens;
			this.size = Tokens.Count;
			this.line = 0;
		}

		internal List<Expression> Parsing(String fileName) {
			this.fileName = fileName;

			List<Expression> result = new List<Expression>();
			while (!Match(TokenType.EOF)) {
				result.Add(Expression());
				Match(TokenType.EOC);
			}
			return result;
		}

		internal Value Parsing(Scope x, String fileName) {
			this.fileName = fileName;

			Value result = null;
			while (!Match(TokenType.EOF)) {
				result = Expression().Eval(x);
				Match(TokenType.EOC);
			}
			return result;
		}

		private Expression Expression() {
			Expression result = null;

			if (Match(TokenType.DOC)) {
				StringBuilder sb = new StringBuilder();
				sb.Append(GetToken(-1).Text);
				while (LookMatch(0, TokenType.DOC)) {
					sb.Append(Consume(TokenType.DOC).Text);
				}

				return new DocE(sb.ToString(), Expression());
			}

			if (Match(TokenType.VAR)) {
				result = ParseVariableDeclaration();
			}
			else if (Match(TokenType.CONST)) {
				result = ParseConstantDeclaration();
			}
			else if (Match(TokenType.RAISE)) {
				result = ParseRaise();
			}
			else if (Match(TokenType.BREAK)) {
				Int32 x = Int32.Parse(Consume(TokenType.NUMBER).Text);
				result = new Break(x);
			}
			else if (Match(TokenType.CONTINUE)) {
				result = new NextE();
			}
			else if (Match(TokenType.RETURN)) {
				result = new Return(Expression());
			}
			else if (Match(TokenType.TYPE)) {
				result = ParseTypeDeclaration();
			}
			else if (Match(TokenType.USING)) {
				result = ParseUsing();
			}
			else if (Match(TokenType.TRY)) {
				result = ParseTry();
			}
			else if (Match(TokenType.IF)) {
				result = ParseIf();
			}
			else if (Match(TokenType.ONCE)) {
				result = new Once(Expression());
			}
			else if (Match(TokenType.MODULE)) {
				result = ParseModule();
			}
			else if (Match(TokenType.WHILE)) {
				result = ParseWhile();
			}
			else if (Match(TokenType.FUN)) {
				result = ParseFunDeclaration();
			}
			else if (Match(TokenType.FOR)) {
				result = ParseFor();
			}
			else {
				result = LogikOr();
			}

			if (Match(TokenType.BUT)) {
				result = new ButE(result, Expression());
			}

			return result;
		}

		private Expression ParseUsing() {
			Boolean isDynamic = false;

			if (Match(TokenType.LBRACKET)) {
				isDynamic = true;
			}

			StringBuilder sb = new StringBuilder(Consume(TokenType.WORD).Text);

			while (Match(TokenType.DOT)) {
				sb.Append("\\").Append(Consume(TokenType.WORD).Text);
			}

			Match(TokenType.RBRACKET);

			if (isDynamic)
				return new DynamicLoad(sb.ToString());

			return new Import(sb.ToString(), this.fileName, this.line);
		}

		private Expression ParseRaise() {
			Int32 line = GetToken(-1).Line;

			Expression exp = LookMatch(0, TokenType.EOC) ? new IdExpression("$!", line, this.fileName) : Expression();

			return new RaiseE(exp, this.fileName, line);
		}

		private Expression ParseConstantDeclaration() {
			Expression result;
			String name = Consume(TokenType.WORD).Text;

			Expression e = new UnknownExpression();

			Expression type = null;

			if (Match(TokenType.COLON)) {
				type = Match(TokenType.AUTO) ? new Auto() : ParseType();
			}

			if (Match(TokenType.EQ)) {
				e = Expression();
			}

			result = new ConstantDeclaration(name, type, e, this.line, this.fileName);
			return result;
		}

		private VariableDeclaration ParseVariableDeclaration() {
			VariableDeclaration result;
			Expression type;
			String id = Consume(TokenType.WORD).Text;

			Expression expression = new UnknownExpression();

			type = null;

			if (Match(TokenType.COLON)) {
				type = ParseType();
			}

			if (Match(TokenType.EQ)) {
				expression = Expression();
			}

			result = new VariableDeclaration(id, type, expression, this.fileName, this.line);
			return result;
		}

		private Expression ParseModule() {
			Expression res;
			String name = Consume(TokenType.WORD).Text;
			List<Expression> expressions = new List<Expression>();
			Match(TokenType.DO);
			while (!Match(TokenType.END)) {
				expressions.Add(Expression());
			}
			res = new Namespace(name, expressions);
			return res;
		}

		private Expression ParseTypeDeclaration() {
			Int32 line = this.line;

			String nameType = Consume(TokenType.WORD).Text;

			// Generic only
			Dictionary<String, Expression> generic = null;
			Boolean isGeneric = false;

			if (Match(TokenType.LBRACKET)) {
				isGeneric = true;
				generic = new Dictionary<String, Expression>();
				while (!Match(TokenType.RBRACKET)) {
					String id = Consume(TokenType.WORD).Text;
					Expression defaultValue = null;
					if (Match(TokenType.EQ)) {
						defaultValue = Expression();
					}
					generic[id] = defaultValue;
					Match(TokenType.SPLIT);
				}
			}

			Dictionary<String, Expression> exemplareFields = new Dictionary<String, Expression>();
			List<Expression> typeFields = new List<Expression>();
			List<Expression> exemplareAttributes = new List<Expression>();
			List<String> inMembers = new List<String>();
			List<String> finalMembers = new List<String>();
			List<Expression> parents = new List<Expression>();

			// RECORDS?

			if (Match(TokenType.LT)) {
				do {
					parents.Add(ParseType());
				} while (Match(TokenType.SPLIT));
			}

			if (Match(TokenType.DO)) {
				while (!Match(TokenType.END)) {
					if (Match(TokenType.EOF)) {
						throw new Lumen.Lang.Std.Exception("end of file (type parsing)") {
							line = this.line,
							file = this.fileName
						};
					}

					if (Match(TokenType.FINAL)) {
						ParseFinalMember(exemplareFields, exemplareAttributes, typeFields, finalMembers);
					}
					else if (Match(TokenType.IN)) {
						ParseInMember(exemplareFields, exemplareAttributes, inMembers);
					}
					else if (Match(TokenType.VAR)) { // exemplare field
						VariableDeclaration exp = ParseVariableDeclaration();

						if (exp.type != null) {
							String newName = "@" + exp.id;
							exemplareAttributes.Add(CreateGetter(exp.id, exp.type));
							exemplareAttributes.Add(CreateSetter(exp.id, exp.type));
							exemplareFields.Add(newName, exp.exp);
						}
						else {
							exemplareFields.Add(exp.id, exp.exp);
						}
					}
					// type field or type fun
					else if (Match(TokenType.TYPE)) {
						typeFields.Add(Expression());
					}
					// Метод.
					else if (LookMatch(0, TokenType.FUN)) {
						exemplareAttributes.Add(Expression());
					}
					// Константа
					else if (LookMatch(0, TokenType.CONST)) {
						typeFields.Add(Expression());
					}
					else {
						Match(GetToken(0).Type);
					}

					Match(TokenType.EOC);
				}
			}

			return new StructE(nameType, exemplareFields, typeFields, exemplareAttributes, parents, inMembers, finalMembers, line, this.fileName);
		}

		private void ParseInMember(Dictionary<String, Expression> exemplareFields, List<Expression> exemplareAttributes, List<String> inMembers) {
			if (Match(TokenType.VAR)) {
				String nameField = Consume(TokenType.WORD).Text;

				Expression type = null;
				if (Match(TokenType.COLON)) {
					type = ParseType();
				}

				Expression e = null;
				if (Match(TokenType.EQ)) {
					e = Expression();
				}

				if (type != null) {
					exemplareAttributes.Add(CreateGetter(nameField, type));
					exemplareAttributes.Add(CreateSetter(nameField, type));
					exemplareFields.Add("@" + nameField, e);
					inMembers.Add("get_" + nameField);
					inMembers.Add("set_" + nameField);
				}
				else {
					exemplareFields.Add(nameField, e);
					inMembers.Add(nameField);
				}
			}
			else if (LookMatch(0, TokenType.FUN)) {
				inMembers.Add(GetToken(1).Text);
				exemplareAttributes.Add(Expression());
			}
		}

		private void ParseFinalMember(Dictionary<String, Expression> exemplareFields, List<Expression> exemplareAttributes, List<Expression> typeFields, List<String> finalMembers) {
			if (Match(TokenType.FUN)) { 
				finalMembers.Add(GetToken(0).Text);
				exemplareAttributes.Add(ParseFunDeclaration());
			}
			else if (Match(TokenType.VAR)) {
				VariableDeclaration exp = ParseVariableDeclaration();

				if (exp.type != null) {
					String newName = "@" + exp.id;
					exemplareAttributes.Add(CreateGetter(exp.id, exp.type));
					exemplareAttributes.Add(CreateSetter(exp.id, exp.type));
					exemplareFields.Add(newName, exp.exp);
					finalMembers.Add(newName);
				}
				else {
					exemplareFields.Add(exp.id, exp.exp);
					finalMembers.Add(exp.id);
				}
			}
			else if (Match(TokenType.TYPE)) {
				if (Match(TokenType.FUN)) {
					finalMembers.Add(GetToken(0).Text);
					typeFields.Add(ParseFunDeclaration());
				}
				else if (Match(TokenType.VAR)) {
					VariableDeclaration exp = ParseVariableDeclaration();

					if (exp.type != null) {
						String newName = "@" + exp.id;
						typeFields.Add(CreateGetter(exp.id, exp.type));
						typeFields.Add(CreateSetter(exp.id, exp.type));
						typeFields.Add(exp);
						finalMembers.Add(newName);
					}
					else {
						typeFields.Add(exp);
						finalMembers.Add(exp.id);
					}
				}
				typeFields.Add(Expression());
			}
		}

		private FunctionDefineStatement CreateGetter(String name, Expression type) {
			return new FunctionDefineStatement("get_" + name, new List<ArgumentMetadataGenerator>(), new DotExpression(new IdExpression("this", this.line, this.fileName), "@" + name), type);
		}

		private FunctionDefineStatement CreateSetter(String name, Expression type) {
			return new FunctionDefineStatement("set_" + name, new List<ArgumentMetadataGenerator> { new ArgumentMetadataGenerator("value", type, null) }, new DotAssigment(new DotExpression(new IdExpression("this", this.line, this.fileName), "@" + name), new IdExpression("value", this.line, this.fileName), this.fileName, this.line), type);
		}

		private Expression ParseLigthTypeDeclaration(String nameType, Boolean g, Dictionary<String, Expression> generic) {
			String parameter = Consume(TokenType.WORD).Text;
			Expression type = null;
			if (Match(TokenType.COLON)) {
				type = ParseType();
			}
			Match(TokenType.RPAREN);
			Expression body = Expression();

			return new LigthType(nameType, parameter, type, body);
		}

		#region

		private Expression ParseFunDeclaration() {
			String name = null;
			Int32 line = this.line;

			List<ArgumentMetadataGenerator> arguments = null;

			switch (GetToken(0).Type) {
				case TokenType.WORD:
					name = Consume(TokenType.WORD).Text;
					break;
				default:
					name = GetToken(0).Text;
					this.position++;
					break;
			}

			Dictionary<String, Expression> generic = null;
			Boolean isGeneric = false;

			if (Match(TokenType.LBRACKET)) {
				isGeneric = true;
				generic = new Dictionary<String, Expression>();
				while (!Match(TokenType.RBRACKET)) {
					String id = Consume(TokenType.WORD).Text;
					Expression defaultValue = null;
					if (Match(TokenType.EQ)) {
						defaultValue = Expression();
					}
					generic[id] = defaultValue;
					Match(TokenType.SPLIT);
				}
			}

			if (Match(TokenType.LPAREN)) {
				arguments = ParseArgs(TokenType.RPAREN);
			}

			Expression returnedType = null;

			if (Match(TokenType.COLON)) {
				returnedType = ParseType();
				Match(TokenType.EOC);
			}

			List<Expression> otherContacts = new List<Expression>();

			if (LookMatch(0, TokenType.WORD) && GetToken(0).Text == "where") {
				Match(TokenType.WORD);
				do {
					otherContacts.Add(Expression());
				} while (Match(TokenType.SPLIT));
				Match(TokenType.EOC);
			}
			Expression ex = null;
			if (Match(TokenType.LAMBDA) || LookMatch(0, TokenType.DO)) {
				ex = Expression();
			}

			FunctionDefineStatement res = new FunctionDefineStatement(name, arguments, ex, returnedType, otherContacts, line, this.fileName);

			if (isGeneric) {
				return new GenericFunctionMaker(res, generic);
			}
			return res;
		}

		private Expression ParseFor() {
			String varName;
			Expression varType = null;
			Boolean declaredVar = false;

			if (Match(TokenType.VAR)) {
				varName = Consume(TokenType.WORD).Text;

				if (Match(TokenType.COLON)) {
					varType = ParseType();
				}

				declaredVar = true;
			}
			else {
				varName = Consume(TokenType.WORD).Text;
			}

			Consume(TokenType.IN);

			Expression Expressions = Expression();

			Match(TokenType.COLON);

			Expression Statement = Expression();

			return new ForE(varName, varType, declaredVar, Expressions, Statement);
		}

		private Expression ParseTry() {
			Expression tryExpression = Expression();
			Match(TokenType.COLON);
			IDictionary<Expression, Expression> exceptCases = new Dictionary<Expression, Expression>();
			Expression finallyCases = null;

			while (Match(TokenType.CATCH)) {
				if (Match(TokenType.COLON) || LookMatch(0, TokenType.DO)) {
					exceptCases.Add(new UnknownExpression(), Expression());
				}
				else {
					Expression cls = Expression();
					Match(TokenType.COLON);
					exceptCases.Add(cls, Expression());
				}
			}

			if (Match(TokenType.FINALLY)) {
				finallyCases = Expression();
			}

			return new TCFExpression(tryExpression, exceptCases, finallyCases);
		}

		private Expression AloneORBlock() {
			BlockE Block = new BlockE();
			Match(TokenType.DO);

			Int32 line = this.line;
			while (!Match(TokenType.END)) {
				if (Match(TokenType.EOF)) {
					throw new Lumen.Lang.Std.Exception("пропущена закрывающая фигурная скобка", stack: Interpriter.mainScope) {
						line = line,
						file = this.fileName
					};
				}

				Block.Add(Expression());
				Match(TokenType.EOC);
			}

			// Optimization
			if (Block.expressions.Count == 1) {
				if (Block.expressions[0] is VariableDeclaration vd) {
					return vd.exp;
				}
				else {
					return Block.expressions[0];
				}
			}

			return Block;
		}

		private Expression ParseIf() {
			Expression condition = Expression();

			Match(TokenType.COLON);

			Expression trueBody = Expression();

			Expression falseBody = new UnknownExpression();

			if (Match(TokenType.ELSE)) {
				Match(TokenType.COLON);
				falseBody = Expression();
			}

			return new ConditionE(condition, trueBody, falseBody);
		}

		private Expression ParseWhile() {
			Expression condition = Expression();
			Match(TokenType.COLON);
			Expression body = Expression();
			return new WhileExpression(condition, body);
		}

		#endregion

		private Expression LogikOr() {
			Expression result = LogikXor();

			while (true) {
				if (Match(TokenType.OR)) {
					result = new BinaryExpression(result, LogikXor(), Op.OR, this.line, this.fileName);
					continue;
				}
				break;
			}
			return result;
		}

		private Expression LogikXor() {
			Expression result = LogikAnd();

			while (true) {
				if (Match(TokenType.XOR)) {
					result = new BinaryExpression(result, LogikAnd(), Op.XOR, this.line, this.fileName);
					continue;
				}
				break;
			}
			return result;
		}

		private Expression LogikAnd() {
			Expression result = Assigment();

			while (true) {
				if (Match(TokenType.AND)) {
					result = new BinaryExpression(result, Assigment(), Op.AND, this.line, this.fileName);
					continue;
				}
				break;
			}
			return result;
		}

		private Expression Assigment() {
			if (LookMatch(0, TokenType.WORD) && LookMatch(1, TokenType.INC)) {
				String n = Consume(TokenType.WORD).Text;
				Consume(TokenType.INC);
				return new Assigment(n, new BinaryExpression(new IdExpression(n, this.line, this.fileName), null, "++", this.line, this.fileName), this.line, this.fileName);
			}

			if (LookMatch(0, TokenType.WORD) && LookMatch(1, TokenType.DEC)) {
				String n = Consume(TokenType.WORD).Text;
				Consume(TokenType.DEC);
				return new Assigment(n, new BinaryExpression(new IdExpression(n, this.line, this.fileName), null, "--", this.line, this.fileName), this.line, this.fileName);
			}

			if (LookMatch(0, TokenType.WORD) && LookMatch(1, TokenType.EQ)) {
				String id = Consume(TokenType.WORD).Text;
				Int32 line = this.line;
				Consume(TokenType.EQ);
				return new Assigment(id, Expression(), line, this.fileName);
			}

			if (LookMatch(0, TokenType.WORD) && LookMatch(1, TokenType.OPEQ)) {
				String id = Consume(TokenType.WORD).Text;
				String operation = Consume(TokenType.OPEQ).Text;
				Int32 line = this.line;
				return new CombineAssigment(id, Expression(), operation, line, this.fileName);
			}

			return Is();
		}

		private Expression Is() {
			Expression expr = Range();

			while (Match(TokenType.FPIPE)) {
				expr = new Applicate(Range(), new List<Lumen.Lang.Expressions.Expression> { expr }, this.line);
			}

			if (Match(TokenType.IS)) {
				if (Match(TokenType.EXCL)) {
					return new BinaryExpression(new IsExpression(expr, ParseType()), null, "@not", this.line, this.fileName);
				}
				else {
					return new IsExpression(expr, ParseType());
				}
			}

			return expr;
		}

		private Expression Range() {
			Expression result = Equality();

			if (Match(TokenType.DOTDOT)) {
				return new BinaryExpression(result, Equality(), Op.DOTE, this.line, this.fileName);
			}

			if (Match(TokenType.DOTDOTDOT)) {
				return new BinaryExpression(result, Equality(), Op.DOTI, this.line, this.fileName);
			}

			return result;
		}

		private Expression Equality() {
			Expression result = Conditional();
			if (Match(TokenType.EQEQ)) {
				Int32 line = GetToken(-1).Line;
				result = new BinaryExpression(result, Conditional(), Op.EQL, line, this.fileName);
				if (Match(TokenType.EQEQ)) {
					line = GetToken(-1).Line;
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)result).expressionTwo, BitwiseXor(), Op.EQL, line, this.fileName), Op.AND, line, this.fileName);
				}
				while (Match(TokenType.EQEQ)) {
					line = GetToken(-1).Line;
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)((BinaryExpression)result).expressionTwo).expressionTwo, BitwiseXor(), Op.EQL, line, this.fileName), Op.AND, line, this.fileName);
				}
			}

			if (Match(TokenType.EQMATCH)) {
				result = new BinaryExpression(result, Conditional(), Op.MATCH, this.line, this.fileName);
				if (Match(TokenType.EQMATCH)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)result).expressionTwo, BitwiseXor(), "==", this.line, this.fileName), "and", this.line, this.fileName);
				}

				while (Match(TokenType.EQMATCH)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)((BinaryExpression)result).expressionTwo).expressionTwo, BitwiseXor(), "==", this.line, this.fileName), "and", this.line, this.fileName);
				}
			}

			if (Match(TokenType.EQNOTMATCH)) {
				result = new BinaryExpression(result, Conditional(), "!~", this.line, this.fileName);
				if (Match(TokenType.EQNOTMATCH)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)result).expressionTwo, BitwiseXor(), "!~", this.line, this.fileName), "and", this.line, this.fileName);
				}

				while (Match(TokenType.EQNOTMATCH)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)((BinaryExpression)result).expressionTwo).expressionTwo, BitwiseXor(), "!~", this.line, this.fileName), "and", this.line, this.fileName);
				}
			}

			if (Match(TokenType.EXCLEQ)) {
				result = new BinaryExpression(result, Conditional(), Op.NOT_EQL, this.line, this.fileName);
				if (Match(TokenType.EXCLEQ)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)result).expressionTwo, BitwiseXor(), "!=", this.line, this.fileName), "and", this.line, this.fileName);
				}

				while (Match(TokenType.EXCLEQ)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)((BinaryExpression)result).expressionTwo).expressionTwo, BitwiseXor(), "!=", this.line, this.fileName), "and", this.line, this.fileName);
				}
			}

			if (Match(TokenType.LTEQGT)) {
				result = new BinaryExpression(result, Conditional(), Op.SHIP, this.line, this.fileName);
				if (Match(TokenType.LTEQGT)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)result).expressionTwo, BitwiseXor(), "<=>", this.line, this.fileName), "and", this.line, this.fileName);
				}

				while (Match(TokenType.LTEQGT)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)((BinaryExpression)result).expressionTwo).expressionTwo, BitwiseXor(), "<=>", this.line, this.fileName), "and", this.line, this.fileName);
				}
			}

			return result;
		}

		private Expression Conditional() {
			Expression expr = BitwiseXor();

			while (true) {
				if (Match(TokenType.LT)) {
					expr = new BinaryExpression(expr, BitwiseXor(), Op.LT, this.line, this.fileName);
					if (Match(TokenType.LT)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)expr).expressionTwo, BitwiseXor(), "<", this.line, this.fileName), "and", this.line, this.fileName);
					}

					while (Match(TokenType.LT)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)((BinaryExpression)expr).expressionTwo).expressionTwo, BitwiseXor(), "<", this.line, this.fileName), "and", this.line, this.fileName);
					}

					continue;
				}

				if (Match(TokenType.LTEQ)) {
					expr = new BinaryExpression(expr, BitwiseXor(), Op.LTEQ, this.line, this.fileName);
					if (Match(TokenType.LTEQ)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)expr).expressionTwo, BitwiseXor(), "<=", this.line, this.fileName), "and", this.line, this.fileName);
					}

					while (Match(TokenType.LTEQ)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)((BinaryExpression)expr).expressionTwo).expressionTwo, BitwiseXor(), "<=", this.line, this.fileName), "and", this.line, this.fileName);
					}

					continue;
				}

				if (Match(TokenType.GT)) {
					expr = new BinaryExpression(expr, BitwiseXor(), Op.GT, this.line, this.fileName);
					if (Match(TokenType.GT)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)expr).expressionTwo, BitwiseXor(), ">", this.line, this.fileName), "and", this.line, this.fileName);
					}

					while (Match(TokenType.GT)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)((BinaryExpression)expr).expressionTwo).expressionTwo, BitwiseXor(), ">", this.line, this.fileName), "and", this.line, this.fileName);
					}

					continue;
				}

				if (Match(TokenType.GTEQ)) {
					expr = new BinaryExpression(expr, BitwiseXor(), Op.GTEQ, this.line, this.fileName);
					if (Match(TokenType.GTEQ)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)expr).expressionTwo, BitwiseXor(), ">=", this.line, this.fileName), "and", this.line, this.fileName);
					}

					while (Match(TokenType.GTEQ)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)((BinaryExpression)expr).expressionTwo).expressionTwo, BitwiseXor(), ">=", this.line, this.fileName), "and", this.line, this.fileName);
					}

					continue;
				}
				break;
			}
			return expr;
		}

		private Expression BitwiseXor() {
			Expression expr = BitwiseOr();

			if (Match(TokenType.BXOR)) {
				Int32 line = GetToken(-1).Line;
				expr = new BinaryExpression(expr, BitwiseOr(), Op.BXOR, line, this.fileName);
			}

			return expr;
		}

		private Expression BitwiseOr() {
			Expression expr = BitwiseAnd();
			if (Match(TokenType.BAR)) {
				Int32 line = GetToken(-1).Line;
				expr = new BinaryExpression(expr, BitwiseAnd(), Op.BOR, line, this.fileName);
			}
			return expr;
		}

		private Expression BitwiseAnd() {
			Expression expr = Bitwise();
			if (Match(TokenType.AMP)) {
				Int32 line = GetToken(-1).Line;
				expr = new BinaryExpression(expr, Bitwise(), Op.BAND, line, this.fileName);
			}
			return expr;
		}

		private Expression Bitwise() {
			// Вычисляем выражение
			Expression expr = Additive();
			while (true) {
				if (Match(TokenType.BLEFT)) {
					Int32 line = GetToken(-1).Line;
					expr = new BinaryExpression(expr, Additive(), Op.LSH, line, this.fileName);
					continue;
				}
				if (Match(TokenType.BRIGTH)) {
					Int32 line = GetToken(-1).Line;
					expr = new BinaryExpression(expr, Additive(), Op.RSH, line, this.fileName);
					continue;
				}
				break;
			}
			return expr;
		}

		private Expression Additive() {
			// Умножение uber alles
			Expression expr = Multiplicate();
			while (true) {
				if (Match(TokenType.USEROPERATOR)) {
					String text = GetToken(-1).Text;
					expr = new BinaryExpression(expr, Multiplicate(), text, this.line, this.fileName);
					continue;
				}

				if (Match(TokenType.PLUS)) {
					expr = new BinaryExpression(expr, Multiplicate(), Op.PLUS, this.line, this.fileName);
					continue;
				}

				if (Match(TokenType.MINUS)) {
					expr = new BinaryExpression(expr, Multiplicate(), Op.MINUS, this.line, this.fileName);
					continue;
				}
				break;
			}
			return expr;
		}

		private Expression Multiplicate() {
			Expression expr = Exponentiation();
			while (true) {
				if (Match(TokenType.STAR)) {
					Int32 line = GetToken(-1).Line;
					expr = new BinaryExpression(expr, Unary(), Op.STAR, line, this.fileName);
					continue;
				}
				if (Match(TokenType.SLASH)) {
					Int32 line = GetToken(-1).Line;
					expr = new BinaryExpression(expr, Unary(), Op.SLASH, line, this.fileName);
					continue;
				}
				if (Match(TokenType.DIV)) {
					Int32 line = GetToken(-1).Line;
					expr = new BinaryExpression(expr, Unary(), Op.DIV, line, this.fileName);
					continue;
				}
				if (Match(TokenType.MOD)) {
					Int32 line = GetToken(-1).Line;
					expr = new BinaryExpression(expr, Unary(), Op.MOD, line, this.fileName);
					continue;
				}
				break;
			}
			return expr;
		}

		private Expression Exponentiation() {
			Expression expr = Unary();

			while (true) {
				if (Match(TokenType.OPEQ)) {
					Int32 line = GetToken(-1).Line;
					String text = GetToken(-1).Text;
					expr = new BinaryExpression(expr, Unary(), text, line, this.fileName);
					continue;
				}

				if (Match(TokenType.POW)) {
					Int32 line = GetToken(-1).Line;
					expr = new BinaryExpression(expr, Unary(), Op.POW, line, this.fileName);
					continue;
				}

				break;
			}

			return expr;
		}

		private Expression Unary() {
			if (LookMatch(0, TokenType.DOTDOTDOT) && LookMatch(1, TokenType.WORD) && LookMatch(2, TokenType.LAMBDA)) {
				Match(TokenType.DOTDOTDOT);
				ArgumentMetadataGenerator arg = new ArgumentMetadataGenerator("*" + Consume(TokenType.WORD).Text, null, null);
				Match(TokenType.LAMBDA);
				return new AnonymeDefine(new List<ArgumentMetadataGenerator> { arg }, Expression());
			}

			if (Match(TokenType.DOTDOTDOT)) {
				return new SpreadE(Convertabli());
			}

			if (Match(TokenType.MINUS)) {
				return new BinaryExpression(Convertabli(), null, Op.UMINUS, this.line, this.fileName);
			}

			if (Match(TokenType.EXCL)) {
				return new BinaryExpression(Convertabli(), null, Op.NOT, this.line, this.fileName);
			}

			if (Match(TokenType.PLUS)) {
				return new BinaryExpression(Convertabli(), null, Op.UPLUS, this.line, this.fileName);
			}

			if (Match(TokenType.STAR)) {
				return new BinaryExpression(Convertabli(), null, Op.USTAR, this.line, this.fileName);
			}

			/*if (Match(TokenType.SLASH)) {
				return new BinaryExpression(Convertabli(), null, "@/", this.line, this.fileName);
			}
			*/
			if (Match(TokenType.TILDE)) {
				return new BinaryExpression(Convertabli(), null, Op.BNOT, this.line, this.fileName);
			}

			if (Match(TokenType.BXOR)) {
				return new BinaryExpression(Convertabli(), null, Op.BXOR, this.line, this.fileName);
			}

			if (Match(TokenType.AMP)) {
				return new BinaryExpression(Convertabli(), null, Op.BAND, this.line, this.fileName);
			}

			return Convertabli();
		}

		private Expression Convertabli() {
			return Application();
		}

		private Expression Application() {
			Expression res = Slice();

			while (Match(TokenType.LPAREN)) {
				if (Match(TokenType.RPAREN)) {
					res = new Applicate(res, new List<Expression>(), this.line);
					continue;
				}

				List<Expression> args = new List<Expression>();

				while (!Match(TokenType.RPAREN)) {
					args.Add(Expression());
					Match(TokenType.SPLIT);
					if (Match(TokenType.EOF)) {
						throw new Lumen.Lang.Std.Exception("при вызове функции пропущен токен ')'", stack: Interpriter.mainScope);
					}
				}

				res = new Applicate(res, args, this.line);
			}

			while (Match(TokenType.DOT)) {
				if (LookMatch(0, TokenType.WORD)) {
					res = new DotExpression(res, Consume(TokenType.WORD).Text);
				}
				else if (Match(TokenType.NEW)) {
					res = new DotExpression(res, "new");
				}
				else if (Match(TokenType.TYPE)) {
					res = new DotExpression(res, "type");
				}

				if (Match(TokenType.LPAREN)) {
					List<Expression> exps = new List<Expression>();

					while (!Match(TokenType.RPAREN)) {
						exps.Add(Expression());
						Match(TokenType.SPLIT);
					}
					res = new DotApplicate(res, exps);
					continue;
				}
			}

			return res;
		}

		private Expression Slice(Expression inn = null) {
			Expression res = inn ?? Dot();

			while (LookMatch(0, TokenType.LBRACKET)) {
				res = Element(res);
				if (Match(TokenType.EQ)) {
					List<Expression> args = (res as DotApplicate).exps;
					args.Add(Expression());
					return new DotApplicate(new DotExpression(((res as DotApplicate).res as DotExpression).expression, Op.SETI), args);
				}
			}

			if (LookMatch(0, TokenType.DOT)) {
				res = Dot(res);
			}

			return res;
		}

		private Expression Dot(Expression inn = null) {
			Expression res = inn ?? Primary();

			while (true) {
				Int32 line = this.line;
				if (Match(TokenType.DOT)) {
					if (LookMatch(0, TokenType.WORD)) {
						res = new DotExpression(res, Consume(TokenType.WORD).Text, this.fileName, line);
					}
					else if (Match(TokenType.NEW)) {
						res = new DotExpression(res, "new");
					}
					else if (Match(TokenType.TYPE)) {
						res = new DotExpression(res, "type");
					}
					else if (Match(TokenType.CONTINUE)) {
						res = new DotExpression(res, "next");
					}

					if (Match(TokenType.EQ)) {
						res = new DotAssigment((DotExpression)res, Expression(), this.fileName, this.line);
					}

					if (LookMatch(0, TokenType.LBRACKET)) {
						res = Slice(res);
					}

					if (Match(TokenType.LPAREN)) {
						List<Expression> exps = new List<Expression>();

						while (!Match(TokenType.RPAREN)) {
							exps.Add(Expression());
							Match(TokenType.SPLIT);
						}
						res = new DotApplicate(res, exps);
						continue;
					}
					continue;
				}
				else if (Match(TokenType.OPTIONAl)) {
					Expression xr = res;
					if (LookMatch(0, TokenType.WORD)) {
						res = new DotExpression(res, Consume(TokenType.WORD).Text, this.fileName, line);
					}
					else if (Match(TokenType.NEW)) {
						res = new DotExpression(res, "new");
					}
					else if (Match(TokenType.TYPE)) {
						res = new DotExpression(res, "type");
					}
					else if (Match(TokenType.CONTINUE)) {
						res = new DotExpression(res, "next");
					}

					if (Match(TokenType.EQ)) {
						res = new DotAssigment((DotExpression)res, Expression(), this.fileName, this.line);
					}

					if (LookMatch(0, TokenType.LBRACKET)) {
						res = Slice(res);
					}

					res = new ConditionE(new IsExpression(xr, new UnknownExpression()), new UnknownExpression(), res);

					if (Match(TokenType.LPAREN)) {
						List<Expression> exps = new List<Expression>();

						while (!Match(TokenType.RPAREN)) {
							exps.Add(Expression());
							Match(TokenType.SPLIT);
						}
						res = new DotApplicate(res, exps);
						continue;
					}
					continue;
				}
				break;
			}

			return res;
		}

		private Expression Primary() {
			Token Current = GetToken(0);

			// Создание объекта.
			if (Match(TokenType.NEW)) {
				Int32 line = this.line;
				if (LookMatch(0, TokenType.DO)) {
					if (LookMatch(1, TokenType.END)) {
						Match(TokenType.DO);
						Match(TokenType.END);
						return new ObjectE(new UnknownExpression());
					}

					Expression e = Expression();

					if (Match(TokenType.COLON)) {
						return new ObjectE(e, Expression());
					}
					else {
						return new ObjectE(e);
					}
				}

				Expression exp = ParseType();
				List<Expression> args = new List<Lumen.Lang.Expressions.Expression>();

				if (Match(TokenType.LPAREN)) {
					while (!Match(TokenType.RPAREN)) {
						args.Add(Expression());
						Match(TokenType.SPLIT);
					}
				}

				if (exp is DotExpression) {
					exp = new DotApplicate(exp, args);
				}
				else {
					exp = new Applicate(exp, args, this.line);
				}

				List<Expression> initor = new List<Expression>();
				if (Match(TokenType.DO)) {
					while (!Match(TokenType.END)) {
						initor.Add(Expression());
						Match(TokenType.SPLIT);
					}
				}

				return new InstanceCreateE(exp, initor, line, this.fileName);
			}

			if (Match(TokenType.NUMBER)) {
				return new ValueE(BigFloat.Parse(Current.Text));
			}

			if (LookMatch(0, TokenType.LPAREN) && LookMatch(1, TokenType.FOR)) {
				Match(TokenType.LPAREN);
				Match(TokenType.FOR);

				String varName;
				Expression varType = null;

				if (Match(TokenType.VAR)) {
					varName = Consume(TokenType.WORD).Text;

					if (Match(TokenType.COLON)) {
						varType = ParseType();
					}
				}
				else {
					varName = Consume(TokenType.WORD).Text;
				}

				Consume(TokenType.IN);

				Expression Expressions = Expression();

				Match(TokenType.COLON);

				Expression Statement = Expression();

				return new ListForGen(varName, Expressions, Statement);
			}

			// (x) =>
			if (LookMatch(0, TokenType.LPAREN) && LookMatch(1, TokenType.WORD) && LookMatch(2, TokenType.RPAREN) && LookMatch(3, TokenType.LAMBDA)) {
				Match(TokenType.LPAREN);
				ArgumentMetadataGenerator arg = new ArgumentMetadataGenerator(Consume(TokenType.WORD).Text, null, null);
				Match(TokenType.RPAREN);
				Match(TokenType.LAMBDA);
				return new AnonymeDefine(new List<ArgumentMetadataGenerator> { arg }, Expression());
			}

			// x =>
			if (LookMatch(0, TokenType.WORD) && LookMatch(1, TokenType.LAMBDA)) {
				ArgumentMetadataGenerator arg = new ArgumentMetadataGenerator(Consume(TokenType.WORD).Text, null, null);
				Match(TokenType.LAMBDA);
				return new AnonymeDefine(new List<ArgumentMetadataGenerator> { arg }, Expression());
			}

			if (Match(TokenType.LAMBDA)) {
				return new AnonymeDefine(new List<ArgumentMetadataGenerator>(), Expression());
			}

			// () =>
			if (LookMatch(0, TokenType.LPAREN) && (LookMatch(1, TokenType.RPAREN)) && LookMatch(2, TokenType.LAMBDA)) {
				Match(TokenType.LPAREN);
				Match(TokenType.RPAREN);
				Match(TokenType.LAMBDA);
				return new AnonymeDefine(new List<ArgumentMetadataGenerator>(), Expression());
			}

			// (x:
			if (LookMatch(0, TokenType.LPAREN) && LookMatch(1, TokenType.WORD) && LookMatch(2, TokenType.COLON)) {
				List<ArgumentMetadataGenerator> args = ParseArgs(TokenType.RPAREN);

				Expression returnedType = null;

				if (Match(TokenType.COLON)) {
					returnedType = ParseType();
					Match(TokenType.EOC);
				}

				Match(TokenType.LAMBDA);

				return new AnonymeDefine(args, Expression(), returnedType, new List<Expression>());
			}

			// (x = 
			if (LookMatch(0, TokenType.LPAREN) && LookMatch(1, TokenType.WORD) && LookMatch(2, TokenType.EQ)) {
				Match(TokenType.LPAREN);
				String nameOfFirstArgument = Consume(TokenType.WORD).Text;
				Match(TokenType.EQ);
				Expression exp = Expression();
				// (x = exp)

				Boolean x = !Match(TokenType.SPLIT);
				Boolean y = LookMatch(1, TokenType.COLON) || LookMatch(1, TokenType.LAMBDA);
				Boolean z = LookMatch(0, TokenType.RPAREN);
				if (x && (z && !y)) {
					Match(TokenType.RPAREN);
					return new Assigment(nameOfFirstArgument, exp, this.line, this.fileName);
				}

				List<ArgumentMetadataGenerator> args = ParseArgs(TokenType.RPAREN);

				args.Insert(0, new ArgumentMetadataGenerator(nameOfFirstArgument, null, exp));

				Expression returnedType = null;

				if (Match(TokenType.COLON)) {
					returnedType = ParseType();
					Match(TokenType.EOC);
				}

				Match(TokenType.LAMBDA);

				return new AnonymeDefine(args, Expression(), returnedType, new List<Expression>());
			}

			// (.: || (x, || (... .,
			if ((LookMatch(0, TokenType.LPAREN) && LookMatch(1, TokenType.WORD) && LookMatch(2, TokenType.SPLIT))
				|| (LookMatch(0, TokenType.LPAREN) && LookMatch(1, TokenType.DOTDOTDOT) && LookMatch(3, TokenType.SPLIT))) {
				Match(TokenType.LPAREN);

				List<ArgumentMetadataGenerator> args = ParseArgs(TokenType.RPAREN);

				Boolean x = false;

				if (Match(TokenType.LAMBDA)) {
					x = true;
				}
				else {
					Match(TokenType.EQ);
				}

				Expression exp = Expression();

				if (x) {
					return new AnonymeDefine(args, exp);
				}
				else {

				}
			}

			// TODO decommented
			// (.) => (&.) =>
			/*  if ((LookMatch(0, TokenType.LPAREN) && LookMatch(1, TokenType.WORD) && LookMatch(2, TokenType.RPAREN) && LookMatch(3, TokenType.LAMBDA)) || (LookMatch(0, TokenType.LPAREN) && LookMatch(2, TokenType.WORD) && LookMatch(3, TokenType.RPAREN) && LookMatch(4, TokenType.LAMBDA)))
			  {
				  Arguments Args = new Arguments();

				  Match(TokenType.LPAREN);

				  while (!Match(TokenType.RPAREN))
				  {
					  if (Match(TokenType.DOTDOTDOT))
					  {
						  string NameAlert = Consume(TokenType.WORD).Text;
						  if (Match(TokenType.EQ))
							  Args.SetArg("*" + NameAlert, Expression().Eval(IK.MainScope));
						  else
							  Args.SetArg("*" + NameAlert, null);
						  continue;
					  }

					  if (Match(TokenType.AMP))
					  {
						  string NameAlert = Consume(TokenType.WORD).Text;

						  if (Match(TokenType.EQ))
							  Args.SetArg("&" + NameAlert, Expression().Eval(IK.MainScope));
						  else
							  Args.SetArg("&" + NameAlert, null);

						  Match(TokenType.SPLIT);
						  continue;
					  }

					  string NameVariable = Consume(TokenType.WORD).Text;

					  if (Match(TokenType.EQ))
						  Args.SetArg(NameVariable, Expression().Eval(IK.MainScope));
					  else
						  Args.SetArg(NameVariable);

					  Match(TokenType.SPLIT);

					  if (Match(TokenType.EOF))
						  throw new HException("пропущена закрывающая круглая скобка < объявление функции ", "ParsingException");
				  }

				  return new AnonymeDefine(Args, Expression());
			  }
			  */

			if (Match(TokenType.LPAREN)) {
				Expression result = Expression();
				if (Match(TokenType.RPAREN)) {
					return result;
				}
			}

			if (Match(TokenType.AUTO)) {
				return new Auto();
			}

			if (Match(TokenType.TEXT)) {
				return new StringE(Current.Text);
			}

			if (Match(TokenType.WORD)) {
				return new IdExpression(Current.Text, Current.Line, this.fileName);
			}

			return BlockExpression();
		}

		private Expression BlockExpression() {
			if (LookMatch(0, TokenType.DO)) {
				return AloneORBlock();
			}

			throw new System.Exception();
		}

		private Expression Element(Expression res) {
			Match(TokenType.LBRACKET);

			List<Expression> Indices = new List<Expression>();

			if (Match(TokenType.RBRACKET)) {
				return new DotApplicate(new DotExpression(res, Op.GETI), Indices);
			}

			do {
				if (Match(TokenType.RBRACKET)) {
					Indices.Add(new UnknownExpression());
					break;
				}
				else if (Match(TokenType.SPLIT)) {
					Indices.Add(new UnknownExpression());
					if (Match(TokenType.RBRACKET)) {
						Indices.Add(new UnknownExpression());
						break;
					}
				}
				else {
					Indices.Add(Expression());
					if (Match(TokenType.SPLIT)) {
						if (Match(TokenType.RBRACKET)) {
							Indices.Add(new UnknownExpression());
							break;
						}
					}
				}
			} while (!Match(TokenType.RBRACKET));

			return new DotApplicate(new DotExpression(res, Op.GETI), Indices);
		}

		private Expression ParseType() {
			if (Match(TokenType.AUTO)) {
				return new Auto();
			}

			Expression result = new IdExpression(Consume(TokenType.WORD).Text, this.line, this.fileName);

			while (Match(TokenType.DOT)) {
				result = new DotExpression(result, Consume(TokenType.WORD).Text);
			}

			while (Match(TokenType.LBRACKET)) {
				List<Expression> exps = new List<Expression>();
				while (!Match(TokenType.RBRACKET)) {
					exps.Add(Expression());
					Match(TokenType.SPLIT);
				}
				result = new DotApplicate(new DotExpression(result, Op.GETI), exps);
			}

			return result;
		}

		private List<ArgumentMetadataGenerator> ParseArgs(TokenType border) {
			List<ArgumentMetadataGenerator> result = new List<ArgumentMetadataGenerator>();

			while (!Match(border)) {
				String nameOfArgument = null;
				Expression typeOfArgument = null;

				if (Match(TokenType.DOTDOTDOT)) {
					nameOfArgument = Consume(TokenType.WORD).Text;

					if (Match(TokenType.COLON)) {
						typeOfArgument = ParseType();
					}

					if (Match(TokenType.EQ)) {
						result.Add(new ArgumentMetadataGenerator("*" + nameOfArgument, typeOfArgument, Expression()));
					}
					else if (Match(TokenType.COLONEQ)) {
						result.Add(new ArgumentMetadataGenerator("*" + nameOfArgument, typeOfArgument, new ExpressionE(Expression())));
					}
					else {
						result.Add(new ArgumentMetadataGenerator("*" + nameOfArgument, typeOfArgument, null));
					}

					Match(TokenType.SPLIT);

					continue;
				}

				nameOfArgument = Consume(TokenType.WORD).Text;

				typeOfArgument = null;

				if (Match(TokenType.COLON)) {
					typeOfArgument = ParseType();
				}

				if (Match(TokenType.EQ)) {
					result.Add(new ArgumentMetadataGenerator(nameOfArgument, typeOfArgument, Expression()));
				}
				else if (Match(TokenType.COLONEQ)) {
					result.Add(new ArgumentMetadataGenerator(nameOfArgument, typeOfArgument, new ExpressionE(Expression())));
				}
				else {
					result.Add(new ArgumentMetadataGenerator(nameOfArgument, typeOfArgument, null));
				}

				Match(TokenType.SPLIT);

				if (Match(TokenType.EOF)) {
					throw new Lumen.Lang.Std.Exception("пропущена закрывающая круглая скобка", stack: Interpriter.mainScope);
				}
			}

			return result;
		}

		private Boolean Match(TokenType type) {
			Token current = GetToken(0);

			if (type != current.Type) {
				return false;
			}

			this.line = current.Line;
			this.position++;
			return true;
		}

		private Boolean LookMatch(Int32 pos, TokenType type) {
			return GetToken(pos).Type == type;
		}

		private Token GetToken(Int32 NewPosition) {
			Int32 position = this.position + NewPosition;

			if (position >= this.size) {
				return new Token(TokenType.EOF, "");
			}

			return this.tokens[position];
		}

		private Token Consume(TokenType type) {
			Token Current = GetToken(0);
			this.line = Current.Line;

			if (type != Current.Type) {
				throw new Lumen.Lang.Std.Exception($"ожидался токен " + type.ToString() + " встречен " + Current.Type.ToString(), stack: Interpriter.mainScope) {
					line = this.line,
					file = this.fileName
				};
			}

			this.position++;
			return Current;
		}
	}
}