﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Lumen.Lang;
using Lumen.Lang.Expressions;

using String = System.String;

namespace Lumen.Lmi {
    public class OptimizationScope : Scope {
        public List<String> whileConstants = new List<String> { "true", "false", "null" };
        public Dictionary<String, Expression> constsValues = new Dictionary<String, Expression> {
            ["true"] = new ValueLiteral(Const.TRUE),
            ["false"] = new ValueLiteral(Const.FALSE),
            ["null"] = new ValueLiteral(Const.UNIT)
        };
        public List<String> notUsed = new List<String>();
        public Boolean isPrimary = true;
    }

    public static class Provider {
        public static event Action<String, String, Int32, String> OnError;
        public static List<ProfileResult> profileResults = new List<ProfileResult>();
        public static Boolean isRelease;
        private static String path;

        public static void SetPath(String path) {
            Provider.path = path;
            //namespace Lumen.Lang.Prelude.Instance.Set("$:", (String)path);
        }

        public static Value Eval(String code) {
            profileResults = new List<ProfileResult>();
            return Interpriter.Start(code);
        }

        public static Value Eval(String code, Scope e) {
            profileResults = new List<ProfileResult>();

            if (e == null) {
                e = new Scope();
            }

            return Interpriter.Start(path, e);
        }

        public static Value Eval(String code, Scope e, String path) {
            profileResults = new List<ProfileResult>();
            return Interpriter.Start(path, e);
        }

        public static void Reise(String type, String message, Int32 line, String file) {
            OnError?.Invoke(type, message, line, file);
        }

        public static Value Get(String name) {
            return Interpriter.mainScope[name];
        }

        public static void Set(String name, Value value) {
            Interpriter.mainScope[name] = value;
        }

        public static Boolean IsCompleted(String source) {
            List<Token> Result;
            try {
                Result = new Lexer(source, "").Tokenization();
            } catch (LumenException) {
                return false;
            }

            if (Result.Count > 0) {
                TokenType Last = Result[Result.Count - 1].Type;
                switch (Last) {
                    case TokenType.AMP:
                        return false;
                    case TokenType.AND:
                        return false;
                    case TokenType.BAR:
                        return false;
                    case TokenType.BLEFT:
                        return false;
                    case TokenType.BREAK:
                        return false;
                    case TokenType.BRIGTH:
                        return false;
                    case TokenType.BXOR:
                        return false;
                    case TokenType.COLON:
                        return false;
                    case TokenType.LET:
                        return false;
                    case TokenType.BLOCK_START:
                        return false;
                    case TokenType.DOT:
                        return false;
                    case TokenType.DOTDOT:
                        return false;
                    case TokenType.DOTDOTDOT:
                        return false;
                    case TokenType.ELSE:
                        return false;
                    case TokenType.ASSIGN:
                        return false;
                    case TokenType.EQUALS:
                        return false;
                    case TokenType.EQMATCH:
                        return false;
                    case TokenType.EQNOTMATCH:
                        return false;
                    case TokenType.NOT:
                        return false;
                    case TokenType.NOT_EQUALS:
                        return false;
                    case TokenType.FOR:
                        return false;
                    case TokenType.FPIPE:
                        return false;
                    case TokenType.GT:
                        return false;
                    case TokenType.GTEQ:
                        return false;
                    case TokenType.IF:
                        return false;
                    case TokenType.IN:
                        return false;
                    case TokenType.IS:
                        return false;
                    case TokenType.LAMBDA:
                        return false;
                    case TokenType.LBRACKET:
                        return false;
                    case TokenType.LPAREN:
                        return false;
                    case TokenType.LT:
                        return false;
                    case TokenType.LTEQ:
                        return false;
                    case TokenType.SHIP:
                        return false;
                    case TokenType.MINUS:
                        return false;
                    case TokenType.MOD:
                        return false;
                    case TokenType.MODULE:
                        return false;
                    case TokenType.OR:
                        return false;
                    case TokenType.PLUS:
                        return false;
                    case TokenType.POWER:
                        return false;
                    case TokenType.RETURN:
                        return false;
                    case TokenType.SLASH:
                        return false;
                    case TokenType.STAR:
                        return false;
                    case TokenType.TILDE:
                        return false;
                    case TokenType.OPEN:
                        return false;
                    case TokenType.XOR:
                        return false;
                }
            }

            if (Result.FindAll(new Predicate<Token>(i => i.Type == TokenType.LPAREN)).Count != Result.FindAll(new Predicate<Token>(i => i.Type == TokenType.RPAREN)).Count) {
                return false;
            }

            if (Result.FindAll(new Predicate<Token>(i => i.Type == TokenType.LBRACKET)).Count != Result.FindAll(new Predicate<Token>(i => i.Type == TokenType.RBRACKET)).Count) {
                return false;
            }

            if (Result.FindAll(new Predicate<Token>(i => i.Type == TokenType.BLOCK_START)).Count != Result.FindAll(new Predicate<Token>(i => i.Type == TokenType.BLOCK_END)).Count) {
                return false;
            }

            return true;
        }

        public class Win32 {
            [DllImport("kernel32.dll")]
            public static extern Boolean AllocConsole();
            [DllImport("kernel32.dll")]
            public static extern Boolean FreeConsole();
        }
    }

    public class ProfileResult {
        public String name;
        public Int64 incl_time;
        public Int64 incl_memory;
        public Int64 excl_time;
        public Int64 excl_memory;
        public Int32 div;
    }
}
