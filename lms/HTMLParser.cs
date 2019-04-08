﻿using System;
using System.Text;
using System.IO;

namespace Lumen.Anatomy {
    public class HTMLParser {
        private readonly String source;

        private readonly StringBuilder builder;
        private Int32 position;

        private readonly String file;

        private readonly StringBuilder finalBuilder;

        private readonly String resultPath;

        public HTMLParser(String fileName, String resultPath) {
            this.file = Path.GetFullPath(fileName);
            this.resultPath = resultPath;
            this.source = File.ReadAllText(fileName);
            this.builder = new StringBuilder();
            this.position = 0;
            this.finalBuilder = new StringBuilder("let mut res = []" + Environment.NewLine);
        }

        public String Run() {
            Char current = this.Current();
            while (current != '\0') {
                if (current == '{') {
                    if (this.At(1) == '%') {
                        current = this.Next();
                        current = this.Next();
                        this.finalBuilder.Append("res <- res + [\"" + this.builder.ToString().Replace("\"", "\\\"") + "\"]").Append(Environment.NewLine);
                        this.builder.Clear();

                        this.BuildCode(false);
                    } else if (this.At(1) == '=') {
                        current = this.Next();
                        current = this.Next();
                        this.finalBuilder.Append("res <- res + [\"" + this.builder.ToString().Replace("\"", "\\\"") + "\"]").Append(Environment.NewLine);
                        this.builder.Clear();

                        this.BuildCode(true);
                    } else {
                        this.builder.Append(current);
                    }
                } else {
                    this.builder.Append(current);
                }
                current = this.Next();
            }

            this.finalBuilder.Append("res <- res + [\"" + this.builder.ToString().Replace("\"", "\\\"") + "\"]").Append(Environment.NewLine);

            this.finalBuilder.Append($"writeFile (res * \"\") \"{(this.resultPath.Replace("\\", "\\\\"))}\"");

            return this.finalBuilder.ToString();
        }

        private void BuildCode(Boolean os) {
            Char current = this.Current();

            while (true) {
                if (os && current == '=' && this.At(1) == '}') {
                    break;
                }

                if (!os && current == '%' && this.At(1) == '}') {
                    break;
                }

                this.builder.Append(current);
                current = this.Next();
            }

            this.Next();
            if (os) {
                this.finalBuilder.Append("res <- res + [" + this.builder.ToString() + "]").Append(Environment.NewLine);
            } else {
                this.finalBuilder.Append(this.builder.ToString()).Append(Environment.NewLine);
            }
            this.builder.Clear();
        }

        public Char At(Int32 position) {
            return this.source[this.position + position];
        }

        public Char Next() {
            if (this.position == this.source.Length) {
                return '\0';
            }
            this.position++;
            return this.Current();
        }

        private Char Current() {
            if (this.position == this.source.Length) {
                return '\0';
            }
            return this.source[this.position];
        }
    }
}
