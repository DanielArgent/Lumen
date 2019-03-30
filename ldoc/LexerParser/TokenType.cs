﻿using System;

namespace ldoc {
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

        DO,
        END,

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
        WITH,
        REF,
        AS,
        DOC
    }
}