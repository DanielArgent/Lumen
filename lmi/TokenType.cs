﻿namespace Lumen.Lmi {
	public enum TokenType {
        OPEN,

        NUMBER,
        HARDNUMBER,

        WORD,
        TEXT,
        IS,

        COLON,
        PLUS,
        MINUS,
        STAR,
        SLASH,
        POWER,
        ASSIGN,
        EQUALS,
        LT,
        LTEQ,
        NOT,
        NOT_EQUALS,
        GT,
        GTEQ,
        MOD,
        DOT,
        XOR,
        BAR,
        OR,
        AMP,
        AND,

        LPAREN,
        RPAREN,

        BLOCK_START,
        BLOCK_END,

        LBRACKET,
        RBRACKET,

        EOC,

        SPLIT,

        LET,

        RETURN,

        LAMBDA,

        IF,
        ELSE,
        WHILE,
        FOR,
        IN,
        BREAK,
        CONTINUE,

        EOF,
        SHIP,
        TILDE,
        BLEFT,
        BRIGTH,
        BXOR,
        EQMATCH,
        EQNOTMATCH,
        DOTDOT,
        DOTDOTDOT,
        MODULE,
        NEXT,
        FPIPE,
        BNUMBER,
        BIG_NUMBER,
        ASYNC,
        VOID,
        BPIPE,
        ATTRIBUTE_OPEN,
        ATTRIBUTE_CLOSE,
        COLONCOLON,
        MATCH,
        TYPE,
        WHERE,
        IMPORT,
        AS,
        ARRAY_OPEN,
        ARRAY_CLOSED,
        TAIL_REC,
        CONTEXT,
        IMPLEMENTS,
		CLASS,
		REC,
		YIELD,
		FROM,
		BANG,
		MIDDLE_PRIORITY_RIGTH,
		ATTRIBUTE,
		WHEN,
		QUESTION,
		USE,
		RAISE,
		FUN,
		TRY,
		EXCEPT,
		FINALLY
	}
}
