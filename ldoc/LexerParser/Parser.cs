﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Lumen.Lang.Expressions;
using Lumen.Lang;

namespace ldoc {
    internal sealed class Parser {
        private readonly List<Token> tokens;
        private readonly Int32 size;
        private readonly String fileName;
        private Int32 position;
        private Int32 line;

        internal Parser(List<Token> tokens, String fileName) {
            this.fileName = fileName;
            this.tokens = tokens;
            this.size = tokens.Count;
            this.line = 0;
        }

        internal List<Expression> Parsing() {
            List<Expression> result = new List<Expression>();

            while (!this.Match(TokenType.EOF)) {
                result.Add(this.Expression());
                this.Match(TokenType.EOC);
            }

            return result;
        }

        private Expression Expression() {
            StringBuilder sb = new StringBuilder();

            while (this.LookMatch(0, TokenType.DOC)) {
                sb.Append(this.Consume(TokenType.DOC).Text);
            }

            if(sb.Length > 0) {
                return new DocComment(this.Expression(), sb.ToString());
            }

			Token current = this.GetToken(0);

			switch (current.Type) {
				case TokenType.RETURN:
					this.Match(TokenType.RETURN);
					if (this.Match(TokenType.EOC) || this.Match(TokenType.EOF)) {
						return new Return(UnitExpression.Instance);
					}
					return new Return(this.Expression());
				case TokenType.MATCH:
					this.Match(TokenType.MATCH);
					return this.ParseMatch();
				case TokenType.NEXT:
					this.Match(TokenType.NEXT);
					return Next.Instance;
				case TokenType.BREAK:
					this.Match(TokenType.BREAK);
					if (this.LookMatch(0, TokenType.NUMBER)) {
						return new Break((Int32)Double.Parse(this.Consume(TokenType.NUMBER).Text));
					}
					return new Break(1);
				case TokenType.TYPE:
					this.Match(TokenType.TYPE);
					return this.ParseTypeDeclaration();
				case TokenType.CLASS:
					this.Match(TokenType.CLASS);
					return this.ParseTypeClassDeclaration();
				case TokenType.OPEN:
					this.Match(TokenType.OPEN);
					return new OpenModule(this.Expression());
				case TokenType.IMPORT:
					this.Match(TokenType.IMPORT);
					return this.ParseRef();
				case TokenType.IF:
					this.Match(TokenType.IF);
					return this.ParseIf();
				case TokenType.MODULE:
					this.Match(TokenType.MODULE);
					return this.ParseModule();
				case TokenType.WHILE:
					this.Match(TokenType.WHILE);
					return this.ParseWhile();
				case TokenType.LET:
					this.Match(TokenType.LET);
					return this.ParseDeclaration();
				case TokenType.FOR:
					this.Match(TokenType.FOR);
					return this.ParseFor();
				default:
					return this.LogikOr();
			}
		}

		private Expression ParseTypeDeclaration() {
			List<ConstructorMetadata> constructors = new List<ConstructorMetadata>();

			String typeName = this.Consume(TokenType.WORD).Text;

			this.Consume(TokenType.EQUALS);

			while (!this.LookMatch(0, TokenType.WHERE) && !this.Match(TokenType.EOC) && !this.Match(TokenType.EOF)) {
				List<ParameterMetadata> paramters = new List<ParameterMetadata>();
				String constructorName = this.Consume(TokenType.WORD).Text;

				while (!this.Match(TokenType.BAR) && !this.LookMatch(0, TokenType.EOC) && !this.LookMatch(0, TokenType.WHERE) && !this.Match(TokenType.EOF)) {
					Boolean isMutable = this.Match(TokenType.MUTABLE);
					String parameterName = this.Consume(TokenType.WORD).Text;
					paramters.Add(new ParameterMetadata(parameterName, isMutable));
				}

				constructors.Add(new ConstructorMetadata(constructorName, paramters));
			}

			List<Expression> derivings = new List<Expression>();

			List<Expression> members = new List<Expression>();

			if (this.Match(TokenType.WHERE)) {
				this.Match(TokenType.DO);

				while (!this.Match(TokenType.END) && !this.Match(TokenType.EOF)) {
					if (this.Match(TokenType.DERIVING)) {
						derivings.Add(this.Expression());
					}
					else {
						members.Add(this.Expression());
					}
					this.Match(TokenType.EOC);
				}
			}

			return new TypeDeclaration(typeName, constructors, members, derivings);
		}

		private Expression ParseTypeClassDeclaration() {
			String name = this.Consume(TokenType.WORD).Text;
			String parameter = Consume(TokenType.WORD).Text;

			List<Expression> members = new List<Expression>();

			if (this.Match(TokenType.WHERE)) {
				this.Match(TokenType.DO);

				while (!this.Match(TokenType.END) && !this.Match(TokenType.EOF)) {
					members.Add(this.Expression());
					this.Match(TokenType.EOC);
				}
			}

			return new TypeClassDeclaration(name, parameter, members);
		}

		private Expression ParseMatch() {
			Expression matchedExpression = this.Expression();
			this.Match(TokenType.COLON);
			this.Match(TokenType.EOC);

			Dictionary<IPattern, Expression> patterns = new Dictionary<IPattern, Expression>();

			while (this.LookMatch(0, TokenType.BAR)) {
				this.Match(TokenType.BAR);

				IPattern pattern = this.ParsePattern();
				this.Match(TokenType.EQUALS);

				Expression body = this.Expression();
				this.Match(TokenType.EOC);

				patterns.Add(pattern, body);
			}

			return new MatchE(matchedExpression, patterns);
		}

		private IPattern ParsePattern() {
			IPattern result = null;

			if (this.Match(TokenType.VOID)) {
				result = new UnitPattern();
			}
			else if (this.Match(TokenType.LBRACKET)) {
				if (this.Match(TokenType.RBRACKET)) {
					result = new EmptyListPattern();
				}
				else {
					List<IPattern> expressions = new List<IPattern>();
					while (!this.Match(TokenType.RBRACKET)) {
						expressions.Add(this.ParsePattern());
						this.Match(TokenType.SPLIT);
					}

					result = new ListPattern(expressions);
				}
			}
			else if (this.LookMatch(0, TokenType.NUMBER)) {
				result = new ValuePattern(new Number(Double.Parse(this.Consume(TokenType.NUMBER).Text)));
			}
			else if (this.Match(TokenType.LPAREN)) {
				result = this.ParsePattern();
				this.Match(TokenType.RPAREN);
			}
			else if (this.LookMatch(0, TokenType.WORD)) {
				String name = this.Consume(TokenType.WORD).Text;

				if (name == "_") {
					result = new DiscordPattern();
				}
				else if (this.Match(TokenType.COLONCOLON)) {
					String tailName = this.Consume(TokenType.WORD).Text;
					result = new HeadTailPattern(name, tailName);
				}
				else if (new[] { "true", "false" }.Contains(name)) {
					result = new VariablePattern(name);
				}
				else if (Char.IsLower(name[0])) {
					result = new NamePattern(name);
				}
				else {
					List<IPattern> subPatterns = new List<IPattern>();

					while (!this.LookMatch(0, TokenType.RPAREN) && !LookMatch(0, TokenType.CONTEXT) && !LookMatch(0, TokenType.EQUALS)) {
						subPatterns.Add(this.ParsePattern());
					}

					this.Match(TokenType.RPAREN);
					result = new DeconstructPattern(name, subPatterns);
				}
			}

			if (this.Match(TokenType.COLON)) {
				List<Expression> exps = new List<Expression> { Expression() };
				while (Match(TokenType.SPLIT)) {
					exps.Add(Expression());
				}
				result = new TypePattern(result, exps);
			}

			if (this.Match(TokenType.AS)) {
				String ide = this.Consume(TokenType.WORD).Text;
				result = new AsPattern(result, ide);
			}

			if (this.Match(TokenType.WHERE)) {
				Expression exp = this.Primary();
				result = new WherePattern(result, exp);
			}

			while (this.Match(TokenType.BAR)) {
				IPattern second = this.ParsePattern();
				result = new OrPattern(result, second);
			}

			return result;
		}

		private Expression ParseRef() {
			if (this.LookMatch(0, TokenType.TEXT)) {
				return new Import(this.Consume(TokenType.TEXT).Text, this.fileName, this.line);
			}

			StringBuilder pathBuilder = new StringBuilder(this.Consume(TokenType.WORD).Text);

			while (this.Match(TokenType.DOT)) {
				pathBuilder.Append("\\").Append(this.Consume(TokenType.WORD).Text);
			}

			return new Import(pathBuilder.ToString(), this.fileName, this.line);
		}

		private Expression ParseVariableDeclaration(IPattern pattern, Boolean isMutable) {
			Expression expression = UnitExpression.Instance;

			if (this.Match(TokenType.EQUALS)) {
				expression = this.Expression();
			}

			return new VariableDeclaration(pattern, expression, isMutable, this.fileName, this.line);
		}

		private Expression ParseModule() {
			String name = this.Consume(TokenType.WORD).Text;
			List<Expression> declarations = new List<Expression>();
			this.Match(TokenType.WHERE);
			this.Match(TokenType.DO);
			while (!this.Match(TokenType.END)) {
				declarations.Add(this.Expression());
				this.Match(TokenType.EOC);
			}
			return new ModuleDeclaration(name, declarations);
		}

		#region
		/// <summary> Declaration with let </summary>
		private Expression ParseDeclaration() {
			Int32 line = this.line;

			Boolean isMutable = this.Match(TokenType.MUTABLE);

			Boolean isLazy = this.Match(TokenType.AMP);

			IPattern pattern = this.ParsePattern();

			// It's variable
			if (this.LookMatch(0, TokenType.EQUALS)) {
				return this.ParseVariableDeclaration(pattern, isMutable);
			}

			List<IPattern> arguments;

			String name;
			if (pattern == null) {
				name = this.GetToken(0).Text;
				this.position++;
			}
			else {
				name = pattern.ToString();
			}

			this.Match(TokenType.EOC); // why?
			arguments = new List<IPattern>();
			while (!this.LookMatch(0, TokenType.EQUALS) && !this.LookMatch(0, TokenType.EOC) && !this.LookMatch(0, TokenType.END)) {
				arguments.Add(this.ParsePattern());
			}

			Expression body = null;

			if (this.Match(TokenType.EQUALS)) {
				body = this.Expression();
			}

			return new FunctionDeclaration(name, arguments, body, isLazy, line, this.fileName);
		}

		/// <summary> For cycle </summary>
		private Expression ParseFor() {
			Boolean varIsDeclared = false;
			IPattern pattern = ParsePattern();

			this.Consume(TokenType.IN);

			Expression container = this.Expression();

			this.Match(TokenType.COLON);

			Expression body = this.Expression();

			return new ForCycle(pattern, null, varIsDeclared, container, body);
		}

		private Expression ParseIf() {
			Expression condition = this.Expression();

			this.Match(TokenType.COLON);

			Expression trueBody = this.Expression();

			Expression falseBody = UnitExpression.Instance;

			this.Match(TokenType.EOC);

			if (this.Match(TokenType.ELSE)) {
				this.Match(TokenType.COLON);
				falseBody = this.Expression();
			}

			return new ConditionE(condition, trueBody, falseBody);
		}

		private Expression ParseWhile() {
			Expression condition = this.Expression();
			this.Match(TokenType.COLON);
			Expression body = this.Expression();
			return new WhileExpression(condition, body);
		}

		#endregion

		private Expression LogikOr() {
			Expression result = this.LogikXor();

			while (true) {
				if (this.Match(TokenType.OR)) {
					result = new BinaryExpression(result, this.LogikXor(), Op.OR, this.line, this.fileName);
					continue;
				}
				break;
			}

			return result;
		}

		private Expression LogikXor() {
			Expression result = this.LogikAnd();

			while (true) {
				if (this.Match(TokenType.XOR)) {
					result = new BinaryExpression(result, this.LogikAnd(), Op.XOR, this.line, this.fileName);
					continue;
				}
				break;
			}
			return result;
		}

		private Expression LogikAnd() {
			Expression result = this.Assigment();

			while (true) {
				if (this.Match(TokenType.AND)) {
					result = new BinaryExpression(result, this.Assigment(), Op.AND, this.line, this.fileName);
					continue;
				}
				break;
			}
			return result;
		}

		private Expression Assigment() {
			return this.Is();
		}

		private Expression Is() {
			Expression expr = this.Range();


			// (a.>>- b).>>- 
			// ((a >>- b) >>- c) >>- d
			while (this.Match(TokenType.FUNCTORBIND)) {
				expr = new DotApplicate(
					new DotExpression(expr, ">>-", this.fileName, this.line), new List<Expression> { this.Range() }, line, fileName);
			}

			while (this.Match(TokenType.BFUNCTORBIND)) {
				expr = new DotApplicate(
					new DotExpression(expr, "<$>", this.fileName, this.line), new List<Expression> { this.Range() }, line, fileName);
			}

			while (this.Match(TokenType.BIND)) {
				expr = new DotApplicate(
					new DotExpression(expr, ">>=", this.fileName, this.line), new List<Expression> { this.Range() }, line, fileName);
			}

			while (this.Match(TokenType.APPLICATIVEF)) {
				expr = new DotApplicate(
					new DotExpression(expr, "<*>", this.fileName, this.line), new List<Expression> { this.Range() }, line, fileName);
			}

			while (this.Match(TokenType.FPIPE)) {
				expr = new Applicate(this.Range(), new List<Expression> { expr }, this.line, this.fileName);
			}

			while (this.Match(TokenType.BPIPE)) {
				expr = new Applicate(expr, new List<Expression> { this.Range() }, this.line, this.fileName);
			}

			/*  if (this.Match(TokenType.IS)) {
                  if (this.Match(TokenType.NOT)) {
                      return new BinaryExpression(new IsExpression(expr, this.ParseType()), null, "@not", this.line, this.fileName);
                  } else {
                      return new IsExpression(expr, this.ParseType());
                  }
              }
              */
			return expr;
		}

		private Expression Range() {
			Expression result = this.Equality();

			if (this.Match(TokenType.DOTDOT)) {
				return new BinaryExpression(result, this.Equality(), Op.RANGE_EXCLUSIVE, this.line, this.fileName);
			}

			if (this.Match(TokenType.DOTDOTDOT)) {
				return new BinaryExpression(result, this.Equality(), Op.RANGE_INCLUSIVE, this.line, this.fileName);
			}

			return result;
		}

		private Expression Equality() {
			Expression result = this.Conditional();
			if (this.Match(TokenType.EQUALS)) {
				Int32 line = this.GetToken(-1).Line;
				result = new BinaryExpression(result, this.Conditional(), Op.EQUALS, line, this.fileName);
				if (this.Match(TokenType.EQUALS)) {
					line = this.GetToken(-1).Line;
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)result).expressionTwo, this.BitwiseXor(), Op.EQUALS, line, this.fileName), Op.AND, line, this.fileName);
				}
				while (this.Match(TokenType.EQUALS)) {
					line = this.GetToken(-1).Line;
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)((BinaryExpression)result).expressionTwo).expressionTwo, this.BitwiseXor(), Op.EQUALS, line, this.fileName), Op.AND, line, this.fileName);
				}
			}

			if (this.Match(TokenType.EQMATCH)) {
				result = new BinaryExpression(result, this.Conditional(), Op.MATCH, this.line, this.fileName);
				if (this.Match(TokenType.EQMATCH)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)result).expressionTwo, this.BitwiseXor(), "==", this.line, this.fileName), "and", this.line, this.fileName);
				}

				while (this.Match(TokenType.EQMATCH)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)((BinaryExpression)result).expressionTwo).expressionTwo, this.BitwiseXor(), "==", this.line, this.fileName), "and", this.line, this.fileName);
				}
			}

			if (this.Match(TokenType.EQNOTMATCH)) {
				result = new BinaryExpression(result, this.Conditional(), "!~", this.line, this.fileName);
				if (this.Match(TokenType.EQNOTMATCH)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)result).expressionTwo, this.BitwiseXor(), "!~", this.line, this.fileName), "and", this.line, this.fileName);
				}

				while (this.Match(TokenType.EQNOTMATCH)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)((BinaryExpression)result).expressionTwo).expressionTwo, this.BitwiseXor(), "!~", this.line, this.fileName), "and", this.line, this.fileName);
				}
			}

			if (this.Match(TokenType.NOT_EQUALS)) {
				result = new BinaryExpression(result, this.Conditional(), Op.NOT_EQL, this.line, this.fileName);
				if (this.Match(TokenType.NOT_EQUALS)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)result).expressionTwo, this.BitwiseXor(), "!=", this.line, this.fileName), "and", this.line, this.fileName);
				}

				while (this.Match(TokenType.NOT_EQUALS)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)((BinaryExpression)result).expressionTwo).expressionTwo, this.BitwiseXor(), "!=", this.line, this.fileName), "and", this.line, this.fileName);
				}
			}

			if (this.Match(TokenType.SHIP)) {
				result = new BinaryExpression(result, this.Conditional(), Op.SHIP, this.line, this.fileName);
				if (this.Match(TokenType.SHIP)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)result).expressionTwo, this.BitwiseXor(), "<=>", this.line, this.fileName), "and", this.line, this.fileName);
				}

				while (this.Match(TokenType.SHIP)) {
					result = new BinaryExpression(result, new BinaryExpression(((BinaryExpression)((BinaryExpression)result).expressionTwo).expressionTwo, this.BitwiseXor(), "<=>", this.line, this.fileName), "and", this.line, this.fileName);
				}
			}

			return result;
		}

		private Expression Conditional() {
			Expression expr = this.BitwiseXor();

			while (true) {
				if (this.Match(TokenType.LT)) {
					expr = new BinaryExpression(expr, this.BitwiseXor(), Op.LT, this.line, this.fileName);
					if (this.Match(TokenType.LT)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)expr).expressionTwo, this.BitwiseXor(), "<", this.line, this.fileName), "and", this.line, this.fileName);
					}

					while (this.Match(TokenType.LT)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)((BinaryExpression)expr).expressionTwo).expressionTwo, this.BitwiseXor(), "<", this.line, this.fileName), "and", this.line, this.fileName);
					}

					continue;
				}

				if (this.Match(TokenType.LTEQ)) {
					expr = new BinaryExpression(expr, this.BitwiseXor(), Op.LTEQ, this.line, this.fileName);
					if (this.Match(TokenType.LTEQ)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)expr).expressionTwo, this.BitwiseXor(), "<=", this.line, this.fileName), "and", this.line, this.fileName);
					}

					while (this.Match(TokenType.LTEQ)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)((BinaryExpression)expr).expressionTwo).expressionTwo, this.BitwiseXor(), "<=", this.line, this.fileName), "and", this.line, this.fileName);
					}

					continue;
				}

				if (this.Match(TokenType.GT)) {
					expr = new BinaryExpression(expr, this.BitwiseXor(), Op.GT, this.line, this.fileName);
					if (this.Match(TokenType.GT)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)expr).expressionTwo, this.BitwiseXor(), ">", this.line, this.fileName), "and", this.line, this.fileName);
					}

					while (this.Match(TokenType.GT)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)((BinaryExpression)expr).expressionTwo).expressionTwo, this.BitwiseXor(), ">", this.line, this.fileName), "and", this.line, this.fileName);
					}

					continue;
				}

				if (this.Match(TokenType.GTEQ)) {
					expr = new BinaryExpression(expr, this.BitwiseXor(), Op.GTEQ, this.line, this.fileName);
					if (this.Match(TokenType.GTEQ)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)expr).expressionTwo, this.BitwiseXor(), ">=", this.line, this.fileName), "and", this.line, this.fileName);
					}

					while (this.Match(TokenType.GTEQ)) {
						expr = new BinaryExpression(expr, new BinaryExpression(((BinaryExpression)((BinaryExpression)expr).expressionTwo).expressionTwo, this.BitwiseXor(), ">=", this.line, this.fileName), "and", this.line, this.fileName);
					}

					continue;
				}
				break;
			}
			return expr;
		}

		private Expression BitwiseXor() {
			Expression expr = this.BitwiseOr();

			if (this.Match(TokenType.BXOR)) {
				Int32 line = this.GetToken(-1).Line;
				expr = new BinaryExpression(expr, this.BitwiseOr(), Op.UNARY_XOR, line, this.fileName);
			}

			return expr;
		}

		private Expression BitwiseOr() {
			Expression expr = this.BitwiseAnd();
			return expr;
		}

		private Expression BitwiseAnd() {
			Expression expr = this.Bitwise();
			if (this.Match(TokenType.AMP)) {
				Int32 line = this.GetToken(-1).Line;
				expr = new BinaryExpression(expr, this.Bitwise(), Op.BAND, line, this.fileName);
			}
			return expr;
		}

		private Expression Bitwise() {
			// Вычисляем выражение
			Expression expr = this.Additive();
			while (true) {
				if (this.Match(TokenType.BLEFT)) {
					Int32 line = this.GetToken(-1).Line;
					expr = new BinaryExpression(expr, this.Additive(), Op.LSH, line, this.fileName);
					continue;
				}
				if (this.Match(TokenType.BRIGTH)) {
					Int32 line = this.GetToken(-1).Line;
					expr = new BinaryExpression(expr, this.Additive(), Op.RSH, line, this.fileName);
					continue;
				}
				break;
			}
			return expr;
		}

		private Expression Additive() {
			// Умножение uber alles
			Expression expr = this.Multiplicate();
			while (true) {
				if (this.Match(TokenType.PLUS)) {
					expr = new BinaryExpression(expr, this.Multiplicate(), Op.PLUS, this.line, this.fileName);
					continue;
				}

				if (this.Match(TokenType.MINUS)) {
					expr = new BinaryExpression(expr, this.Multiplicate(), Op.MINUS, this.line, this.fileName);
					continue;
				}
				break;
			}
			return expr;
		}

		private Expression Multiplicate() {
			Expression expr = this.Exponentiation();
			while (true) {
				if (this.Match(TokenType.STAR)) {
					Int32 line = this.GetToken(-1).Line;
					expr = new BinaryExpression(expr, this.Unary(), Op.STAR, line, this.fileName);
					continue;
				}

				if (this.Match(TokenType.SLASH)) {
					Int32 line = this.GetToken(-1).Line;
					expr = new BinaryExpression(expr, this.Unary(), Op.SLASH, line, this.fileName);
					continue;
				}

				if (this.Match(TokenType.MOD)) {
					Int32 line = this.GetToken(-1).Line;
					expr = new BinaryExpression(expr, this.Unary(), Op.MOD, line, this.fileName);
					continue;
				}
				break;
			}
			return expr;
		}

		private Expression Exponentiation() {
			Expression expr = this.Unary();

			while (true) {
				if (this.Match(TokenType.BXOR)) {
					Int32 line = this.GetToken(-1).Line;
					expr = new BinaryExpression(expr, this.Unary(), Op.POW, line, this.fileName);
					continue;
				}

				break;
			}

			return expr;
		}

		private Expression Unary() {
			if (this.Match(TokenType.DOTDOTDOT)) {
				return new SpreadE(this.DoubleColon());
			}

			if (this.Match(TokenType.MINUS)) {
				return new BinaryExpression(this.DoubleColon(), null, Op.UMINUS, this.line, this.fileName);
			}

			if (this.Match(TokenType.NOT)) {
				return new BinaryExpression(this.DoubleColon(), null, Op.NOT, this.line, this.fileName);
			}

			if (this.Match(TokenType.PLUS)) {
				return new BinaryExpression(this.DoubleColon(), null, Op.UPLUS, this.line, this.fileName);
			}

			if (this.Match(TokenType.STAR)) {
				return new BinaryExpression(this.DoubleColon(), null, Op.USTAR, this.line, this.fileName);
			}

			/*if (Match(TokenType.SLASH)) {
				return new BinaryExpression(Convertabli(), null, "@/", this.line, this.fileName);
			}
			*/
			if (this.Match(TokenType.TILDE)) {
				return new BinaryExpression(this.DoubleColon(), null, Op.BNOT, this.line, this.fileName);
			}

			if (this.Match(TokenType.BXOR)) {
				return new BinaryExpression(this.DoubleColon(), null, Op.UNARY_XOR, this.line, this.fileName);
			}

			if (this.Match(TokenType.AMP)) {
				return new BinaryExpression(this.DoubleColon(), null, Op.BAND, this.line, this.fileName);
			}

			return this.DoubleColon();
		}

		private Expression DoubleColon() {
			Expression result = this.Application();

			if (this.Match(TokenType.COLONCOLON)) {
				Expression right = this.Expression();

				result = new AddE(result, right);
			}

			return result;
		}

		private Boolean ValidToken() {
			return this.LookMatch(0, TokenType.LPAREN)
				|| this.LookMatch(0, TokenType.BIG_NUMBER)
				|| this.LookMatch(0, TokenType.BNUMBER)
				|| this.LookMatch(0, TokenType.HARDNUMBER)
				|| this.LookMatch(0, TokenType.LBRACKET)
				|| this.LookMatch(0, TokenType.ARRAY_OPEN)
				|| this.LookMatch(0, TokenType.NUMBER)
				|| this.LookMatch(0, TokenType.TEXT)
				|| this.LookMatch(0, TokenType.VOID)
				|| this.LookMatch(0, TokenType.WORD);
		}

		private Expression Application() {
			Expression result = this.Dot();

			if (this.ValidToken()) {
				List<Expression> args = new List<Expression>();

				while (this.ValidToken()) {
					args.Add(this.Dot());
				}

				result = new Applicate(result, args, this.line, this.fileName);
				this.Match(TokenType.EOC);
			}

			return result;
		}

		private Expression Dot(Expression inn = null) {
			Expression res = inn ?? this.Primary();

			while (true) {
				Int32 line = this.line;
				// (...).
				if (this.Match(TokenType.DOT)) {
					// (...).x
					if (this.LookMatch(0, TokenType.WORD)) {
						res = new DotExpression(res, this.Consume(TokenType.WORD).Text, this.fileName, line);
					}
					// (...).[...]
					while (this.LookMatch(0, TokenType.LBRACKET)) {
						res = this.Slice(res);
					}
					// (...).x 4 
					if (this.ValidToken()) {
						List<Expression> args = new List<Expression>();

						while (this.ValidToken()) {
							args.Add(this.Dot());
						}

						res = new DotApplicate(res as DotExpression, args, line, fileName);
					}
					continue;
				}
				break;
			}

			return res;
		}

		private Expression Slice(Expression inn) {
			Expression res = inn;

			while (this.LookMatch(0, TokenType.LBRACKET)) {
				res = this.Element(res);
				if (this.Match(TokenType.ASSIGN)) {
					List<Expression> args = (res as GetIndexE).indices;
					args.Add(this.Expression());
					return new DotApplicate(new DotExpression((res as GetIndexE).res, Op.SETI, this.fileName, this.line), args, line, fileName);
				}
			}

			if (this.LookMatch(0, TokenType.DOT)) {
				res = this.Dot(res);
			}

			return res;
		}

		private Expression Primary() {
			Token Current = this.GetToken(0);

			// Identifiters
			if (this.Match(TokenType.WORD)) {
				// Lambdas x -> ...
				if (this.Match(TokenType.LAMBDA)) {
					NamePattern argument = new NamePattern(Current.Text);
					return new LambdaFunction(new List<IPattern> { argument }, this.Expression());
				}

				return new IdExpression(Current.Text, Current.Line, this.fileName);
			}

			if (this.LookMatch(0, TokenType.LBRACKET) && this.LookMatch(1, TokenType.FOR)) {
				this.Match(TokenType.LBRACKET);
				this.Match(TokenType.FOR);

				this.Match(TokenType.LET);
				this.Match(TokenType.MUTABLE);
				String varName = this.Consume(TokenType.WORD).Text;

				this.Consume(TokenType.IN);

				Expression Expressions = this.Expression();

				this.Match(TokenType.COLON);

				Expression Statement = this.Expression();

				this.Match(TokenType.RBRACKET);

				return new ListGenerator(new SequenceGenerator(varName, Expressions, Statement));
			}

			// Lists
			if (this.Match(TokenType.LBRACKET)) {
				List<Expression> elements = new List<Expression>();

				while (!this.Match(TokenType.RBRACKET)) {
					if (this.Match(TokenType.EOF)) {
						throw new LumenException(Exceptions.UNCLOSED_LIST_LITERAL, line: Current.Line, fileName: fileName);
					}

					elements.Add(this.Expression());
					this.Match(TokenType.SPLIT);
				}

				return new ListE(elements);
			}

			// Tail recursion
			if (this.Match(TokenType.TAIL_REC)) {
				List<Expression> args = new List<Expression>();

				while (this.ValidToken()) {
					args.Add(this.Dot());
				}

				Expression res = new TailRecursion(args, this.fileName, this.line);
				return res;
			}

			if (this.Match(TokenType.REC)) {
				List<Expression> args = new List<Expression>();

				while (this.ValidToken()) {
					args.Add(this.Dot());
				}

				Expression res = new Recursion(args, this.fileName, this.line);
				return res;
			}

			// Arrays
			if (this.Match(TokenType.ARRAY_OPEN)) {
				List<Expression> elements = new List<Expression>();
				while (!this.Match(TokenType.ARRAY_CLOSED)) {
					if (this.Match(TokenType.EOF)) {
						throw new LumenException(Exceptions.UNCLOSED_ARRAY_LITERAL, line: Current.Line, fileName: fileName);
					}

					elements.Add(this.Expression());
					this.Match(TokenType.SPLIT);
				}

				return new ArrayE(elements);
			}

			// Number literals
			if (this.Match(TokenType.NUMBER)) {
				return new ValueE(Double.Parse(Current.Text, System.Globalization.NumberFormatInfo.InvariantInfo));
			}

			// Sequence generators
			if (this.LookMatch(0, TokenType.LPAREN) && this.LookMatch(1, TokenType.FOR)) {
				this.Match(TokenType.LPAREN);
				this.Match(TokenType.FOR);

				this.Match(TokenType.LET);
				String varName = this.Consume(TokenType.WORD).Text;

				this.Consume(TokenType.IN);

				Expression Expressions = this.Expression();

				this.Match(TokenType.COLON);

				Expression Statement = this.Expression();

				this.Match(TokenType.RPAREN);

				return new SequenceGenerator(varName, Expressions, Statement);
			}

			// Lambdas () -> ...
			if (this.LookMatch(0, TokenType.VOID) && this.LookMatch(1, TokenType.LAMBDA)) {
				this.Match(TokenType.VOID);
				this.Match(TokenType.LAMBDA);
				return new LambdaFunction(new List<IPattern>(), this.Expression());
			}

			// Lambdas (x) -> ...
			if (this.LookMatch(0, TokenType.LPAREN) && this.LookMatch(1, TokenType.WORD)
				&& this.LookMatch(2, TokenType.RPAREN) && this.LookMatch(3, TokenType.LAMBDA)) {
				this.Match(TokenType.LPAREN);
				NamePattern arg = new NamePattern(this.Consume(TokenType.WORD).Text);
				this.Match(TokenType.RPAREN);
				this.Match(TokenType.LAMBDA);
				return new LambdaFunction(new List<IPattern> { arg }, this.Expression());
			}


			// Lambdas (x, ...) -> ...
			if (this.LookMatch(0, TokenType.LPAREN) && this.LookMatch(1, TokenType.WORD)
				&& this.LookMatch(2, TokenType.SPLIT)) {
				this.Match(TokenType.LPAREN);

				List<IPattern> args = new List<IPattern>();

				while (!this.Match(TokenType.RPAREN)) {
					args.Add(new NamePattern(this.Consume(TokenType.WORD).Text));
					this.Match(TokenType.SPLIT);

					if (this.Match(TokenType.EOF)) {
						throw new LumenException(Exceptions.UNCLOSED_LAMBDA, line: Current.Line, fileName: fileName);
					}
				}

				this.Consume(TokenType.LAMBDA);
				return new LambdaFunction(args, this.Expression());
			}

			if (this.Match(TokenType.VOID)) {
				return UnitExpression.Instance;
			}

			if (this.Match(TokenType.LPAREN)) {
				Expression result = this.Expression();
				this.Match(TokenType.RPAREN);
				return result;
			}

			if (this.Match(TokenType.TEXT)) {
				return new StringE(Current.Text);
			}

			return this.BlockExpression();
		}

		private Expression BlockExpression() {
			if (this.LookMatch(0, TokenType.DO)) {
				return this.AloneORBlock();
			}

			if (this.Match(TokenType.EOC)) {
				return UnitExpression.Instance;
			}

			throw new LumenException(Exceptions.UNEXCEPTED_TOKEN.F(this.GetToken(0).Type), fileName: this.fileName, line: this.line);
		}

		private Expression AloneORBlock() {
			BlockE Block = new BlockE();
			this.Match(TokenType.DO);

			Int32 line = this.line;
			while (!this.Match(TokenType.END)) {
				if (this.Match(TokenType.EOF)) {
					throw new LumenException("пропущена закрывающая фигурная скобка") {
						Line = line,
						File = this.fileName
					};
				}

				Block.Add(this.Expression());
				this.Match(TokenType.EOC);
			}

			// Optimization
			if (Block.expressions.Count == 1) {
				if (Block.expressions[0] is VariableDeclaration vd) {
					return vd.assignableExpression;
				}
				else {
					return Block.expressions[0];
				}
			}

			return Block;
		}

		private Expression Element(Expression res) {
			this.Match(TokenType.LBRACKET);

			List<Expression> Indices = new List<Expression>();

			if (this.Match(TokenType.RBRACKET)) {
				return new DotApplicate(new DotExpression(res, Op.GETI, this.fileName, this.line), Indices, line, fileName);
			}

			do {
				if (this.Match(TokenType.RBRACKET)) {
					Indices.Add(UnitExpression.Instance);
					break;
				}
				else if (this.Match(TokenType.SPLIT)) {
					Indices.Add(UnitExpression.Instance);
					if (this.Match(TokenType.RBRACKET)) {
						Indices.Add(UnitExpression.Instance);
						break;
					}
				}
				else {
					Indices.Add(this.Expression());
					if (this.Match(TokenType.SPLIT)) {
						if (this.Match(TokenType.RBRACKET)) {
							Indices.Add(UnitExpression.Instance);
							break;
						}
					}
				}
			} while (!this.Match(TokenType.RBRACKET));

			return new GetIndexE(res, Indices, line, fileName);
		}

		private Boolean Match(TokenType type) {
			Token current = this.GetToken(0);

			if (type != current.Type) {
				return false;
			}

			this.line = current.Line;
			this.position++;
			return true;
		}

		private Boolean LookMatch(Int32 pos, TokenType type) {
			return this.GetToken(pos).Type == type;
		}

		private Token GetToken(Int32 offset) {
			Int32 position = this.position + offset;

			if (position >= this.size) {
				return new Token(TokenType.EOF, "");
			}

			return this.tokens[position];
		}

		private Token Consume(TokenType type) {
			Token Current = this.GetToken(0);
			this.line = Current.Line;

			if (type != Current.Type) {
				throw new LumenException(Exceptions.WAIT_ANOTHER_TOKEN.F(type.ToString(), Current.Type.ToString()), fileName: this.fileName, line: this.line);
			}

			this.position++;
			return Current;
		}
	}
}
// 1229 -> 1143