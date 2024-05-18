using Nethermind.Int256;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Scripting
{
    /// <summary>
    /// Parses assembly 
    /// </summary>
    public class DVMAParserV1
    {
        public enum V1TokenType
        {
            UNK = 0,
            LABEL = 1,
            OP = 2,
            NUMD = 3,
            NUMH = 4,
            COLON = 5,
            EOF = 6,
        }

        public struct DVMATokenV1
        {
            public string Value;
            public DVMOpcodeV1 OpcodeV1;
            public V1TokenType Type;

            public DVMATokenV1()
            {
                Value = string.Empty;
                OpcodeV1 = DVMOpcodeV1.HALT;
                Type = V1TokenType.UNK;
            }
        }

        public class DVMALexerV1
        {
            private StreamReader _stream;

            private int c;
            private List<int> chars;

            private DVMATokenV1 _eof = new DVMATokenV1 { OpcodeV1 = DVMOpcodeV1.HALT, Type = V1TokenType.EOF, Value = string.Empty };

            public DVMALexerV1(Stream stream)
            {
                _stream = new StreamReader(stream);
                c = 0;
                chars = new List<int>();
            }

            public DVMALexerV1(string path)
            {
                _stream = new StreamReader(File.OpenRead(path));
                c = 0;
                chars = new List<int>();
            }

            public void Close()
            {
                _stream.Close();
            }

            private void ReadChar()
            {
                if (c == -1) return;
                c = _stream.Read();
                chars.Add(c);
            }

            private void ResetBuffer()
            {
                chars = new List<int> { c };
            }

            private static bool Whitespace(int c)
            {
                var _c = (char)c;
                return _c == ' ' || _c == '\t' || _c == '\r' || _c == '\n';
            }

            private static bool IsIdent(int c)
            {
                var _c = (char)c;
                return (_c == '_') || (_c >= 'A' && _c <= 'Z') || (_c >= 'a' && _c <= 'z') || (_c >= '0' && _c <= '9');
            }

            public DVMATokenV1 Read()
            {
                if (c == -1) return _eof;
redo:           
                if (c == 0) ReadChar();

                while (c == -1 || Whitespace(c)) ReadChar();

                if (c == -1) return _eof;

                ResetBuffer();
                DVMATokenV1 tok = new DVMATokenV1();

                switch ((char)c)
                {
                    case ';':
                        {
                            while (c != -1 && c != '\n') ReadChar();
                            goto redo;
                        }
                    case ':':
                        ReadChar();
                        tok.Type = V1TokenType.COLON;
                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        {
                            (bool success, bool dec) = ParseNum();
                            if (!success) goto redo;
                            tok.Type = dec ? V1TokenType.NUMD : V1TokenType.NUMH;
                            tok.Value = new string(chars.Select(x => (char)x).ToArray()).Trim();
                        }
                        break;
                    default:
                        {
                            if (IsIdent(c))
                            {
                                while (IsIdent(c)) ReadChar();
                                tok.Value = new string(chars.Select(x => (char)x).ToArray()[..^1]).Trim();
                                int op = GetOp(tok.Value);
                                if (op == -1)
                                {
                                    tok.Type = V1TokenType.LABEL;
                                }
                                else
                                {
                                    tok.Type = V1TokenType.OP;
                                    tok.OpcodeV1 = (DVMOpcodeV1)op;
                                }
                            }
                            else
                            {
                                while (!Whitespace(c) && c != ';') ReadChar();
                                tok.Type = V1TokenType.UNK;
                                tok.Value = new string(chars.Select(x => (char)x).ToArray()[..^1]).Trim();
                            }
                        }
                        break;
                }

                return tok;
            }

            private int GetOp(string name)
            {
                if (!keyops.TryGetValue(name, out DVMOpcodeV1 value))
                {
                    return -1;
                }
                else
                {
                    return (int)value;
                }
            }

            private static Dictionary<string, DVMOpcodeV1> keyops = new Dictionary<string, DVMOpcodeV1>
            {
                { "HALT", DVMOpcodeV1.HALT },
                { "HLT", DVMOpcodeV1.HALT },
                { "ADD", DVMOpcodeV1.ADD },
                { "SUB", DVMOpcodeV1.SUB },
                { "MUL", DVMOpcodeV1.MUL },
                { "DIV", DVMOpcodeV1.DIV },
                { "SDIV", DVMOpcodeV1.SDIV },
                { "MOD", DVMOpcodeV1.MOD },
                { "SMOD", DVMOpcodeV1.SMOD },
                { "MADD", DVMOpcodeV1.MADD },
                { "MMUL", DVMOpcodeV1.MMUL },
                { "MEXP", DVMOpcodeV1.MEXP },
                { "SGEX", DVMOpcodeV1.SGEX },
                { "LT", DVMOpcodeV1.LT },
                { "GT", DVMOpcodeV1.GT },
                { "SLT", DVMOpcodeV1.SLT },
                { "SGT", DVMOpcodeV1.SGT },
                { "EQ", DVMOpcodeV1.EQ },
                { "ISZ", DVMOpcodeV1.ISZ },
                { "AND", DVMOpcodeV1.AND },
                { "OR", DVMOpcodeV1.OR },
                { "XOR", DVMOpcodeV1.XOR },
                { "NOT", DVMOpcodeV1.NOT },
                { "BMSK", DVMOpcodeV1.BMSK },
                { "BYTE", DVMOpcodeV1.BMSK },
                { "SHL", DVMOpcodeV1.SHL },
                { "SHR", DVMOpcodeV1.SHR },
                { "SAR", DVMOpcodeV1.SAR },
                { "LOAD", DVMOpcodeV1.LOAD },
                { "STOR", DVMOpcodeV1.STOR },
                { "STOR8", DVMOpcodeV1.STOR8 },
                { "JMP", DVMOpcodeV1.JMP },
                { "JMPC", DVMOpcodeV1.JMPC },
                { "JMPI", DVMOpcodeV1.JMPC },
                { "CALL", DVMOpcodeV1.CALL },
                { "CALLI", DVMOpcodeV1.CALLI },
                { "CALLC", DVMOpcodeV1.CALLI },
                { "RET", DVMOpcodeV1.RET },
                { "RDML", DVMOpcodeV1.RDML },
                { "RDMCP", DVMOpcodeV1.RDMCP },
                { "CODCP", DVMOpcodeV1.CODCP },
                { "DATL", DVMOpcodeV1.DATL },
                { "DATCP", DVMOpcodeV1.DATCP },
                { "PC", DVMOpcodeV1.PC },
                { "MCP", DVMOpcodeV1.MCP },
                { "MEMCP", DVMOpcodeV1.MCP },
                { "PUSH0", DVMOpcodeV1.PUSH0 },
                { "PUSH1", DVMOpcodeV1.PUSH1 },
                { "PUSH2", DVMOpcodeV1.PUSH2 },
                { "PUSH3", DVMOpcodeV1.PUSH3 },
                { "PUSH4", DVMOpcodeV1.PUSH4 },
                { "PUSH5", DVMOpcodeV1.PUSH5 },
                { "PUSH6", DVMOpcodeV1.PUSH6 },
                { "PUSH7", DVMOpcodeV1.PUSH7 },
                { "PUSH8", DVMOpcodeV1.PUSH8 },
                { "PUSH9", DVMOpcodeV1.PUSH9 },
                { "PUSH10", DVMOpcodeV1.PUSH10 },
                { "PUSH11", DVMOpcodeV1.PUSH11 },
                { "PUSH12", DVMOpcodeV1.PUSH12 },
                { "PUSH13", DVMOpcodeV1.PUSH13 },
                { "PUSH14", DVMOpcodeV1.PUSH14 },
                { "PUSH15", DVMOpcodeV1.PUSH15 },
                { "PUSH16", DVMOpcodeV1.PUSH16 },
                { "PUSH17", DVMOpcodeV1.PUSH17 },
                { "PUSH18", DVMOpcodeV1.PUSH18 },
                { "PUSH19", DVMOpcodeV1.PUSH19 },
                { "PUSH20", DVMOpcodeV1.PUSH20 },
                { "PUSH21", DVMOpcodeV1.PUSH21 },
                { "PUSH22", DVMOpcodeV1.PUSH22 },
                { "PUSH23", DVMOpcodeV1.PUSH23 },
                { "PUSH24", DVMOpcodeV1.PUSH24 },
                { "PUSH25", DVMOpcodeV1.PUSH25 },
                { "PUSH26", DVMOpcodeV1.PUSH26 },
                { "PUSH27", DVMOpcodeV1.PUSH27 },
                { "PUSH28", DVMOpcodeV1.PUSH28 },
                { "PUSH29", DVMOpcodeV1.PUSH29 },
                { "PUSH30", DVMOpcodeV1.PUSH30 },
                { "PUSH31", DVMOpcodeV1.PUSH31 },
                { "PUSH32", DVMOpcodeV1.PUSH32 },
                { "DUP1", DVMOpcodeV1.DUP1 },
                { "DUP2", DVMOpcodeV1.DUP2 },
                { "DUP3", DVMOpcodeV1.DUP3 },
                { "DUP4", DVMOpcodeV1.DUP4 },
                { "DUP5", DVMOpcodeV1.DUP5 },
                { "DUP6", DVMOpcodeV1.DUP6 },
                { "DUP7", DVMOpcodeV1.DUP7 },
                { "DUP8", DVMOpcodeV1.DUP8 },
                { "DUP9", DVMOpcodeV1.DUP9 },
                { "DUP10", DVMOpcodeV1.DUP10 },
                { "DUP11", DVMOpcodeV1.DUP11 },
                { "DUP12", DVMOpcodeV1.DUP12 },
                { "DUP13", DVMOpcodeV1.DUP13 },
                { "DUP14", DVMOpcodeV1.DUP14 },
                { "DUP15", DVMOpcodeV1.DUP15 },
                { "DUP16", DVMOpcodeV1.DUP16 },
                { "SWAP1", DVMOpcodeV1.SWAP1 },
                { "SWAP2", DVMOpcodeV1.SWAP2 },
                { "SWAP3", DVMOpcodeV1.SWAP3 },
                { "SWAP4", DVMOpcodeV1.SWAP4 },
                { "SWAP5", DVMOpcodeV1.SWAP5 },
                { "SWAP6", DVMOpcodeV1.SWAP6 },
                { "SWAP7", DVMOpcodeV1.SWAP7 },
                { "SWAP8", DVMOpcodeV1.SWAP8 },
                { "SWAP9", DVMOpcodeV1.SWAP9 },
                { "SWAP10", DVMOpcodeV1.SWAP10 },
                { "SWAP11", DVMOpcodeV1.SWAP11 },
                { "SWAP12", DVMOpcodeV1.SWAP12 },
                { "SWAP13", DVMOpcodeV1.SWAP13 },
                { "SWAP14", DVMOpcodeV1.SWAP14 },
                { "SWAP15", DVMOpcodeV1.SWAP15 },
                { "SWAP16", DVMOpcodeV1.SWAP16 },
                { "POP", DVMOpcodeV1.POP },
                { "JMPD", DVMOpcodeV1.JMPD },
                { "CODSZ", DVMOpcodeV1.CODSZ },
                { "DATSZ", DVMOpcodeV1.DATSZ },
                { "RDMSZ", DVMOpcodeV1.RDMSZ },
                { "ADDR", DVMOpcodeV1.ADDR },
                { "VALUE", DVMOpcodeV1.VALUE },
                { "INDEX", DVMOpcodeV1.INDEX },
                { "CTXFEE", DVMOpcodeV1.CTXFEE },
                { "CTXHASH", DVMOpcodeV1.CTXHASH },
                { "CTXINTL", DVMOpcodeV1.CTXINTL },
                { "CTXINTH", DVMOpcodeV1.CTXINTH },
                { "CTXTKEY", DVMOpcodeV1.CTXTKEY },
                { "NUMIN", DVMOpcodeV1.NUMIN },
                { "INREFID", DVMOpcodeV1.INREFID },
                { "INREFIX", DVMOpcodeV1.INREFIX },
                { "INADDR", DVMOpcodeV1.INADDR },
                { "INVAL", DVMOpcodeV1.INVAL },
                { "INRSC", DVMOpcodeV1.INRSC },
                { "INDATL", DVMOpcodeV1.INDATL },
                { "INDATCP", DVMOpcodeV1.INDATCP },
                { "INDATH", DVMOpcodeV1.INDATH },
                { "INDATSZ", DVMOpcodeV1.INDATSZ },
                { "NUMRIN", DVMOpcodeV1.NUMRIN },
                { "RINREFID", DVMOpcodeV1.RINREFID },
                { "RINREFIX", DVMOpcodeV1.RINREFIX },
                { "RINADDR", DVMOpcodeV1.RINADDR },
                { "RINVAL", DVMOpcodeV1.RINVAL },
                { "RINRSC", DVMOpcodeV1.RINRSC },
                { "RINDATL", DVMOpcodeV1.RINDATL },
                { "RINDATCP", DVMOpcodeV1.RINDATCP },
                { "RINDATH", DVMOpcodeV1.RINDATH },
                { "RINDATSZ", DVMOpcodeV1.RINDATSZ },
                { "NUMOUT", DVMOpcodeV1.NUMOUT },
                { "OUTADDR", DVMOpcodeV1.OUTADDR },
                { "OUTVAL", DVMOpcodeV1.OUTVAL },
                { "OUTRSC", DVMOpcodeV1.OUTRSC },
                { "OUTDATL", DVMOpcodeV1.OUTDATL },
                { "OUTDATCP", DVMOpcodeV1.OUTDATCP },
                { "OUTDATH", DVMOpcodeV1.OUTDATH },
                { "OUTDATSZ", DVMOpcodeV1.OUTDATSZ },
                { "NUMPIN", DVMOpcodeV1.NUMPIN },
                { "PINTAG", DVMOpcodeV1.PINTAG },
                { "PINIXA", DVMOpcodeV1.PINIXA },
                { "NUMPOUT", DVMOpcodeV1.NUMPOUT },
                { "POUTUKEY", DVMOpcodeV1.POUTUKEY },
                { "POUTCOM", DVMOpcodeV1.POUTCOM },
                { "POUTAMT", DVMOpcodeV1.POUTAMT },
                { "NUMSIG", DVMOpcodeV1.NUMSIG },
                { "SIGADDR", DVMOpcodeV1.SIGADDR },
                { "SHA2", DVMOpcodeV1.SHA2 },
                { "KECC", DVMOpcodeV1.KECC },
                { "RIPEMD", DVMOpcodeV1.RIPEMD },
                { "EDVER", DVMOpcodeV1.EDVER },
                { "EDSGN", DVMOpcodeV1.EDSGN },
                { "COMM", DVMOpcodeV1.COMM },
                { "NOP", DVMOpcodeV1.NOP },
                { "SETERR", DVMOpcodeV1.SETERR },
                { "SETSAFE", DVMOpcodeV1.SETSAFE },
                { "FAIL", DVMOpcodeV1.FAIL },
                { "SUCC", DVMOpcodeV1.SUCC },
                { "IREX", DVMOpcodeV1.IREX },
                { "INVLD", DVMOpcodeV1.INVLD },
                { "BOOM", DVMOpcodeV1.BOOM },
            };

            private (bool Num, bool Dec) ParseNum()
            {
                ReadChar();
                if (c == -1)
                {
                    return (true, true);
                }

                bool dec = false;
                if ((char)c == 'x')
                {
                    ReadChar();
                    ResetBuffer();
                    while ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')) ReadChar();
                }
                else if (c >= '0' && c <= '9')
                {
                    dec = true;
                    while (c >= '0' && c <= '9') ReadChar();
                }

                if (c == -1)
                {
                    return (true, dec);
                }
                else if (Whitespace(c))
                {
                    return (true, dec);
                }
                else
                {
                    return (false, dec);
                }
            }
        }

        private DVMALexerV1 Lexer;
        private int Offset;
        private List<Node> Nodes = new();
        private List<Node> ResolveLabelPushes = new();
        private Dictionary<string, int> LabelToOffset = new();

        private class Node
        {
            public int sz = 0;
            public DVMOpcodeV1 op;
            public UInt256 val = UInt256.Zero; // value to push if push
            public string Label = string.Empty; // label pointing to instruction
            public string LabelArg = string.Empty; // 
        }

        public DVMAParserV1(Stream s)
        {
            Lexer = new DVMALexerV1(s);
            Offset = 0;
        }

        public DVMAParserV1(string path)
        {
            Lexer = new DVMALexerV1(path);
            Offset = 0;
        }

        public void Close()
        {
            Lexer.Close();
        }

        public static byte[] ParseString(string code)
        {
            var parser = new DVMAParserV1(new MemoryStream(Encoding.ASCII.GetBytes(code)));
            var rv = parser.Parse();
            parser.Close();
            return rv;
        }

        public static byte[] ParseFile(string path)
        {
            var parser = new DVMAParserV1(path);
            var rv = parser.Parse();
            parser.Close();
            return rv;
        }

        public DVMATokenV1 ParseOp(DVMATokenV1 tok, string lbl = "")
        {
            if (tok.Type != V1TokenType.OP)
            {
                throw new Exception("Expected OP");
            }
            var n = new Node { op = tok.OpcodeV1, Label = lbl, sz = 1 };
            if ((byte)n.op >= 0x30 && (byte)n.op <= 0x4f)
            {
                n.sz += ((int)n.op - 0x2f);

                // expect number or label
                tok = Lexer.Read();
                switch (tok.Type)
                {
                    case V1TokenType.NUMD:
                        {
                            n.val = UInt256.Parse(tok.Value);
                            if (n.val.BitLen > ((n.sz - 1)*8))
                            {
                                throw new Exception("Cannot fit numeric immediate into specified width");
                            }
                        }
                        break;
                    case V1TokenType.NUMH:
                        {
                            n.val = UInt256.Parse(tok.Value.ToLower(), System.Globalization.NumberStyles.HexNumber);
                            if (n.val.BitLen > ((n.sz - 1) * 8))
                            {
                                throw new Exception("Cannot fit numeric immediate into specified width");
                            }
                        }
                        break;
                    case V1TokenType.LABEL:
                        {
                            n.LabelArg = tok.Value;
                            ResolveLabelPushes.Add(n);
                        }
                        break;
                    default:
                        throw new Exception("PUSH expects numeric or label immediate");
                }
            }

            Nodes.Add(n);
            Offset += n.sz;
            return tok;
        }

        public byte[] Parse()
        {
            for (DVMATokenV1 tok = Lexer.Read(); tok.Type != V1TokenType.EOF; tok = Lexer.Read())
            {
                switch (tok.Type)
                {
                    case V1TokenType.EOF:
                        break;
                    case V1TokenType.LABEL:
                        {
                            var lbl = tok.Value;
                            var offset = Offset;
                            tok = Lexer.Read();
                            if (tok.Type != V1TokenType.COLON)
                            {
                                throw new Exception("Label expects a colon to follow");
                            }
                            tok = Lexer.Read();
                            tok = ParseOp(tok, lbl);
                            if (LabelToOffset.ContainsKey(lbl))
                            {
                                throw new Exception($"Repeated label: {lbl}");
                            }
                            LabelToOffset.Add(lbl, offset);
                        }
                        break;
                    case V1TokenType.OP:
                        {
                            tok = ParseOp(tok);
                        }
                        break;
                    default:
                        throw new Exception("Unexpected token");
                }
            }

            // resolve label arguments
            foreach (var n in ResolveLabelPushes)
            {
                if (!LabelToOffset.TryGetValue(n.LabelArg, out var offset))
                {
                    throw new Exception($"Unresolved label in PUSH: {n.LabelArg}");
                }
                else
                {
                    n.val = (UInt256)offset;
                    if (n.val.BitLen > ((n.sz - 1) * 8))
                    {
                        throw new Exception("Cannot fit numeric immediate into specified width");
                    }
                }
            }

            // build bytecode
            MemoryStream ms = new MemoryStream();
            foreach (var n in Nodes)
            {
                ms.WriteByte((byte)n.op);
                if ((byte)n.op >= 0x30 && (byte)n.op <= 0x4f)
                {
                    var b = n.val.ToBigEndian();
                    var l = n.sz - 1;
                    ms.Write(b.AsSpan()[(32 - l)..]);
                }
            }

            return ms.ToArray();
        }
    }
}
