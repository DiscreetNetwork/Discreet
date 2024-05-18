using Discreet.Cipher;
using Discreet.Coin;
using Discreet.Coin.Models;
using Discreet.Coin.Script;
using Discreet.Common;
using Discreet.Common.Exceptions;
using Discreet.Common.Serialize;
using Nethermind.Int256;
using System;

namespace Discreet.Scripting
{
    public class DVM
    {
        public static readonly int MAX_INSTRUCTIONS_V1 = 10_000_000;
        public static readonly int MAX_MEMORY_V1 = 1024 * 1024 * 16;
        public static readonly int MAX_STACK_V1 = 2048;

        protected static readonly Datum _nullDatum = Datum.Default();

        protected byte[] _memory;
        protected UInt256[] _stack;

        protected ChainScript _script;
        protected Datum _datum;
        protected Datum _redeemer;
        protected ScriptContext _ctx;

        protected uint _pc;
        protected uint _sp;
        protected UInt256? success;

        protected bool strict = false;
        protected bool debug = false; // FIXME: set to false after testing

        public DVM(ChainScript script)
        {
            _stack = new UInt256[MAX_STACK_V1];
            _script = script;
            InitializeMemory();
        }

        protected void InitializeMemory()
        {
            var codeDataLen = _script.Data.Length;
            int startMem = 32768;
            while (codeDataLen > startMem && startMem <= MAX_MEMORY_V1)
            {
                startMem <<= 1;
            }

            _memory = new byte[startMem];
            Buffer.BlockCopy(_script.Data, 0, _memory, 0, _script.Data.Length);
        }

        protected VerifyException CheckMemory(int offset, int dest = -1, int size = 32)
        {
            var maxLoc = Math.Max(offset + size, dest + size);
            var memSize = _memory.Length;

            while (maxLoc >= memSize && memSize <= MAX_MEMORY_V1)
            {
                memSize <<= 1;
            }

            if (memSize > MAX_MEMORY_V1)
            {
                return new VerifyException("DVM", "Memory size exceeded limit");
            }

            // resize if needed
            if (memSize != _memory.Length)
            {
                var _newMemory = new byte[memSize];
                Buffer.BlockCopy(_memory, 0, _newMemory, 0, _memory.Length);
                _memory = _newMemory;
            }

            return null;
        }

        protected VerifyException LoadFromDatum(UInt256 _destination, UInt256 _offset, Datum datum, UInt256 _size)
        {
            if (_offset > int.MaxValue)
            {
                return new VerifyException("DVM", "offset too large");
            }

            if (_destination > int.MaxValue)
            {
                return new VerifyException("DVM", "destination too large");
            }

            if (_size > int.MaxValue)
            {
                return new VerifyException("DVM", "size too large");
            }

            var offset = (int)_offset;
            var size = (int)_size;
            var destination = (int)_destination;

            CheckMemory(-1, destination, size);

            if (strict)
            {
                if (offset + size > datum.Data.Length)
                {
                    return new VerifyException("DVM", "Loading from datum exceeds datum bound");
                }

                Buffer.BlockCopy(datum.Data, offset, _memory, destination, size);
            }
            else
            {
                if (offset > datum.Data.Length)
                {
                    // write zero to memory location
                    Buffer.BlockCopy(new byte[size], 0, _memory, destination, size);
                }
                else if (offset + size > datum.Data.Length)
                {
                    byte[] dat = new byte[size];
                    Buffer.BlockCopy(datum.Data, offset, dat, 0, datum.Data.Length - offset);
                    Buffer.BlockCopy(dat, 0, _memory, destination, size);
                }
                else
                {
                    Buffer.BlockCopy(datum.Data, offset, _memory, destination, size);
                }
            }

            return null;
        }

        protected VerifyException LoadFromDatumToStack(UInt256 _offset, Datum datum)
        {
            if (_offset > int.MaxValue)
            {
                return new VerifyException("DVM", "offset too large");
            }

            var offset = (int)_offset;

            if (strict)
            {
                if (offset + 32 > datum.Data.Length)
                {
                    return new VerifyException("DVM", "Loading from datum exceeds datum bound");
                }

                _stack[_sp++] = new UInt256(datum.Data.AsSpan(offset, 32), true);
            }
            else
            {
                if (offset > datum.Data.Length)
                {
                    // write zero to memory location
                    _stack[_sp++] = UInt256.Zero;
                }
                else if (offset + 32 > datum.Data.Length)
                {
                    byte[] dat = new byte[32];
                    Buffer.BlockCopy(datum.Data, offset, dat, 0, datum.Data.Length - offset);
                    _stack[_sp++] = new UInt256(dat.AsSpan(), true);
                }
                else
                {
                    _stack[_sp++] = new UInt256(datum.Data.AsSpan(offset, 32), true);
                }
            }

            return null;
        }

        public VerifyException Execute(int index, Datum datum, Datum redeemer, ScriptContext ctx)
        {
            _redeemer = redeemer;
            _ctx = ctx;
            _datum = datum;

            _pc = 0;
            _sp = 0;
            var _cost = 0;
            bool jmp = false;

            while (success == null && _cost < MAX_INSTRUCTIONS_V1)
            {
                var op = _script.Code[_pc];
                switch ((DVMOpcodeV1)op)
                {
                    case DVMOpcodeV1.HALT:
                        {
                            if (_sp <= 0)
                            {
                                return new VerifyException("DVM", "Halt received with empty stack; default to FAIL");
                            }
                            success = _stack[--_sp];
                        }
                        break;
                    case DVMOpcodeV1.ADD:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "ADD has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            _stack[_sp++] = _a + _b;
                        }
                        break;
                    case DVMOpcodeV1.SUB:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "SUB has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            _stack[_sp++] = _a - _b;
                        }
                        break;
                    case DVMOpcodeV1.MUL:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "MUL has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            _stack[_sp++] = _a * _b;
                        }
                        break;
                    case DVMOpcodeV1.DIV:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "DIV has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            _stack[_sp++] = _a / _b;
                        }
                        break;
                    case DVMOpcodeV1.SDIV:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "SDIV has insufficient stack arguments; expected 2");
                            }

                            var _a = new Int256(_stack[--_sp]);
                            var _b = new Int256(_stack[--_sp]);
                            Int256.Divide(_a, _b, out var res);
                            _stack[_sp++] = (UInt256)res;
                        }
                        break;
                    case DVMOpcodeV1.MOD:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "MOD has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            UInt256.Mod(_a, _b, out var res);
                            _stack[_sp++] = res;
                        }
                        break;
                    case DVMOpcodeV1.SMOD:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "SMOD has insufficient stack arguments; expected 2");
                            }

                            var _a = new Int256(_stack[--_sp]);
                            var _b = new Int256(_stack[--_sp]);
                            Int256.Mod(_a, _b, out var res);
                            _stack[_sp++] = (UInt256)res;
                        }
                        break;
                    case DVMOpcodeV1.MADD:
                        {
                            if (_sp < 3)
                            {
                                return new VerifyException("DVM", "MADD has insufficient stack arguments; expected 3");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            var _n = _stack[--_sp];
                            UInt256.AddMod(_a, _b, _n, out var res);
                            _stack[_sp++] = res;
                        }
                        break;
                    case DVMOpcodeV1.MMUL:
                        {
                            if (_sp < 3)
                            {
                                return new VerifyException("DVM", "MMUL has insufficient stack arguments; expected 3");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            var _n = _stack[--_sp];
                            UInt256.MultiplyMod(_a, _b, _n, out var res);
                            _stack[_sp++] = res;
                        }
                        break;
                    case DVMOpcodeV1.MEXP:
                        {
                            if (_sp < 3)
                            {
                                return new VerifyException("DVM", "MEXP has insufficient stack arguments; expected 3");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            var _n = _stack[--_sp];
                            UInt256.ExpMod(_a, _b, _n, out var res);
                            _stack[_sp++] = res;
                        }
                        break;
                    case DVMOpcodeV1.SGEX:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "SGEX has insufficient stack arguments; expected 2");
                            }
                            var _i = _stack[--_sp];
                            var _v = _stack[--_sp];

                            if (_i > 31)
                            {
                                _stack[_sp++] = _v;
                            }
                            else
                            {
                                var _bytl = (int)_i.u0;
                                // check the leading bit of the byte
                                var _uix = (_bytl >> 3); // tells us which variable to check
                                ulong _bv;
                                switch (_uix)
                                {
                                    case 0:
                                        _bv = _v.u0;
                                        break;
                                    case 1:
                                        _bv = _v.u1;
                                        break;
                                    case 2:
                                        _bv = _v.u2;
                                        break;
                                    case 3:
                                        _bv = _v.u3;
                                        break;
                                    default:
                                        return new VerifyException("DVM", "Strange error with logic in SGEX");
                                }
                                var _vOff = ((_bytl & 7) << 3) + 7;
                                var _zOrF = (_bv >> _vOff); // gets sign bit

                                if (_zOrF == 0)
                                {
                                    _stack[_sp++] = _v;
                                }
                                else
                                {
                                    var _mask = ~((1ul << (_vOff + 1)) - 1);
                                    _bv = _mask | _bv;
                                    var _l0 = (_uix > 0) ? _v.u0 : ((_uix == 0) ? _bv : ulong.MaxValue);
                                    var _l1 = (_uix > 1) ? _v.u0 : ((_uix == 1) ? _bv : ulong.MaxValue);
                                    var _l2 = (_uix > 2) ? _v.u0 : ((_uix == 2) ? _bv : ulong.MaxValue);
                                    var _l3 = (_uix == 3) ? _bv : ulong.MaxValue;
                                    _stack[_sp++] = new UInt256(_l0, _l1, _l2, _l3);
                                }
                            }
                        }
                        break;
                    case DVMOpcodeV1.LT:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "LT has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            _stack[_sp++] = _a < _b ? UInt256.One : UInt256.Zero;
                        }
                        break;
                    case DVMOpcodeV1.GT:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "GT has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            _stack[_sp++] = _a > _b ? UInt256.One : UInt256.Zero;
                        }
                        break;
                    case DVMOpcodeV1.SLT:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "SLT has insufficient stack arguments; expected 2");
                            }
                            var _a = new Int256(_stack[--_sp]);
                            var _b = new Int256(_stack[--_sp]);
                            _stack[_sp++] = _a < _b ? UInt256.One : UInt256.Zero;
                        }
                        break;
                    case DVMOpcodeV1.SGT:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "SGT has insufficient stack arguments; expected 2");
                            }
                            var _a = new Int256(_stack[--_sp]);
                            var _b = new Int256(_stack[--_sp]);
                            _stack[_sp++] = _a > _b ? UInt256.One : UInt256.Zero;
                        }
                        break;
                    case DVMOpcodeV1.EQ:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "EQ has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            _stack[_sp++] = _a == _b ? UInt256.One : UInt256.Zero;
                        }
                        break;
                    case DVMOpcodeV1.ISZ:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "ISZ has insufficient stack arguments; expected 3");
                            }
                            var _a = _stack[--_sp];
                            _stack[_sp++] = _a.IsZero ? UInt256.One : UInt256.Zero;
                        }
                        break;
                    case DVMOpcodeV1.AND:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "AND has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            _stack[_sp++] = _a & _b;
                        }
                        break;
                    case DVMOpcodeV1.OR:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "OR has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            _stack[_sp++] = _a | _b;
                        }
                        break;
                    case DVMOpcodeV1.XOR:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "XOR has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];
                            _stack[_sp++] = _a ^ _b;
                        }
                        break;
                    case DVMOpcodeV1.NOT:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "NOT has insufficient stack arguments; expected 1");
                            }
                            var _a = _stack[--_sp];
                            _stack[_sp++] = ~_a;
                        }
                        break;
                    case DVMOpcodeV1.BMSK:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "BMSK has insufficient stack arguments; expected 2");
                            }
                            var _i = _stack[--_sp];
                            var _x = _stack[--_sp];
                            
                            if (_i > 31)
                            {
                                _stack[_sp++] = UInt256.Zero;
                            }
                            else
                            {
                                var _b = (int)_i;
                                var _xb = _x.ToBigEndian();
                                _stack[_sp++] = (UInt256)(_xb[31 - _b]);
                            }
                        }
                        break;
                    case DVMOpcodeV1.SHL:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "SHL has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];

                            if (_b > 255)
                            {
                                _stack[--_sp] = UInt256.Zero;
                            }
                            else
                            {
                                _stack[_sp++] = _a << (int)_b;
                            }
                        }
                        break;
                    case DVMOpcodeV1.SHR:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "SHR has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];

                            if (_b > 255)
                            {
                                _stack[--_sp] = UInt256.Zero;
                            }
                            else
                            {
                                _stack[_sp++] = _a >> (int)_b;
                            }
                        }
                        break;
                    case DVMOpcodeV1.SAR:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "SHR has insufficient stack arguments; expected 2");
                            }
                            var _a = _stack[--_sp];
                            var _b = _stack[--_sp];

                            if (_b > 255)
                            {
                                _stack[--_sp] = UInt256.Zero;
                            }
                            else
                            {
                                Int256.RightShift(new Int256(_a), (int)_b, out var res);
                                _stack[_sp++] = (UInt256)res;
                            }
                        }
                        break;
                    case DVMOpcodeV1.LOAD:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "LOAD has insufficient stack arguments; expected 1");
                            }

                            var _offset = _stack[--_sp];

                            if (_offset > MAX_MEMORY_V1)
                            {
                                return new VerifyException("DVM", "LOAD offset out of bounds");
                            }
                            else
                            {
                                var _offsetInt = (int)_offset;
                                var _memErr = CheckMemory(_offsetInt);

                                if (_memErr != null) return _memErr;

                                _stack[_sp++] = new UInt256(_memory.AsSpan(_offsetInt, 32), true);
                            }
                        }
                        break;
                    case DVMOpcodeV1.STOR:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "STOR has insufficient stack arguments; expected 2");
                            }

                            var _offset = _stack[--_sp];
                            var _value = _stack[--_sp];

                            if (_offset > MAX_MEMORY_V1)
                            {
                                return new VerifyException("DVM", "STOR offset out of bounds");
                            }
                            else
                            {
                                var _offsetInt = (int)_offset;
                                var _memErr = CheckMemory(-1, _offsetInt);

                                if (_memErr != null) return _memErr;

                                Buffer.BlockCopy(_value.ToBigEndian(), 0, _memory, _offsetInt, 32);
                            }
                        }
                        break;
                    case DVMOpcodeV1.STOR8:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "STOR8 has insufficient stack arguments; expected 2");
                            }

                            var _offset = _stack[--_sp];
                            var _value = _stack[--_sp];

                            if (_offset > MAX_MEMORY_V1)
                            {
                                return new VerifyException("DVM", "STOR8 offset out of bounds");
                            }
                            else
                            {
                                var _offsetInt = (int)_offset;
                                var _memErr = CheckMemory(-1, _offsetInt, 1);

                                if (_memErr != null) return _memErr;

                                _memory[_offsetInt] = (byte)(_value & 0xFF);
                            }
                        }
                        break;
                    case DVMOpcodeV1.JMP:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "JMP has insufficient stack arguments; expected 1");
                            }

                            var _dest = _stack[--_sp];
                            if (_dest >= _script.Code.Length)
                            {
                                return new VerifyException("DVM", "JMP location out of bounds");
                            }

                            _pc = (uint)_dest;
                            jmp = true;
                        }
                        break;
                    case DVMOpcodeV1.JMPC:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "JMPC has insufficient stack arguments; expected 2");
                            }

                            var _dest = _stack[--_sp];
                            var _cond = _stack[--_sp];

                            if (!_cond.IsZero)
                            {
                                if (_dest >= _script.Code.Length)
                                {
                                    return new VerifyException("DVM", "JMPC location out of bounds");
                                }

                                _pc = (uint)_dest;
                                jmp = true;
                            }
                        }
                        break;
                    case DVMOpcodeV1.CALL:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "CALL has insufficient stack arguments; expected 1");
                            }

                            var _dest = _stack[--_sp];

                            if (_dest >= _script.Code.Length)
                            {
                                return new VerifyException("DVM", "CALL location out of bounds");
                            }

                            _stack[_sp++] = (_pc + 1);
                            _pc = (uint)_dest;
                            jmp = true;
                        }
                        break;
                    case DVMOpcodeV1.CALLI:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "CALLI has insufficient stack arguments; expected 1");
                            }

                            var _dest = _stack[--_sp];
                            var _argsz = _stack[--_sp];

                            if (_dest >= _script.Code.Length)
                            {
                                return new VerifyException("DVM", "CALLI location out of bounds");
                            }

                            if (_argsz > _sp)
                            {
                                return new VerifyException("DVM", "CALLI argument size larger than stack space");
                            }

                            var _argszInt = (int)_argsz;
                            UInt256[] _args = new UInt256[_argszInt];
                            Array.Copy(_stack, (_sp - _argszInt), _args, 0, _argszInt);

                            _stack[_sp - _argszInt] = _pc + 1;
                            _sp++;
                            Array.Copy(_args, 0, _stack, (_sp - _argszInt), _argszInt);
                            _pc = (uint)_dest;
                            jmp = true;
                        }
                        break;
                    case DVMOpcodeV1.RET:
                        {
                            var _newPc = _stack[--_sp];

                            if (_newPc >= _script.Code.Length)
                            {
                                return new VerifyException("DVM", "RET failed to retrieve a valid return destination");
                            }

                            _pc = (uint)_newPc;
                            jmp = true;
                        }
                        break;
                    case DVMOpcodeV1.RDML:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "RDML has insufficient stack arguments; expected 1");
                            }

                            var _offset = _stack[--_sp];

                            var _err = LoadFromDatumToStack(_offset, redeemer);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.RDMCP:
                        {
                            if (_sp < 3)
                            {
                                return new VerifyException("DVM", "RDMCP has insufficient stack arguments; expected 3");
                            }

                            var _dest = _stack[--_sp];
                            var _offset = _stack[--_sp];
                            var _sz = _stack[--_sp];

                            var _err = LoadFromDatum(_dest, _offset, redeemer, _sz);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.CODCP:
                        {
                            if (_sp < 3)
                            {
                                return new VerifyException("DVM", "CODCP has insufficient stack arguments; expected 3");
                            }

                            var _dest = _stack[--_sp];
                            var _offset = _stack[--_sp];
                            var _sz = _stack[--_sp];

                            var _err = LoadFromDatum(_dest, _offset, new Datum { Version = 0, Data = _script.Code}, _sz);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.DATL:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "DATL has insufficient stack arguments; expected 1");
                            }

                            var _offset = _stack[--_sp];

                            var _err = LoadFromDatumToStack(_offset, datum);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.DATCP:
                        {
                            if (_sp < 3)
                            {
                                return new VerifyException("DVM", "DATCP has insufficient stack arguments; expected 3");
                            }

                            var _dest = _stack[--_sp];
                            var _offset = _stack[--_sp];
                            var _sz = _stack[--_sp];

                            var _err = LoadFromDatum(_dest, _offset, datum, _sz);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.PC:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = _pc;
                        }
                        break;
                    case DVMOpcodeV1.MCP:
                        {
                            if (_sp < 3)
                            {
                                return new VerifyException("DVM", "MCP has insufficient stack arguments; expected 3");
                            }

                            var _dest = _stack[--_sp];
                            var _offset = _stack[--_sp];
                            var _sz = _stack[--_sp];

                            if (_offset > MAX_MEMORY_V1)
                            {
                                return new VerifyException("DVM", "MCP offset out of bounds");
                            }

                            if ((_dest + _sz) > MAX_MEMORY_V1)
                            {
                                return new VerifyException("DVM", "MCP destination out of bounds");
                            }

                            var _offsetInt = (int)_offset;
                            var _szInt = (int)_sz;
                            var _destInt = (int)_dest;

                            var _err = CheckMemory(_offsetInt, _destInt, _szInt);
                            if (_err != null)
                            {
                                return _err;
                            }

                            byte[] dat = new byte[_szInt];
                            Buffer.BlockCopy(_memory, _offsetInt, dat, 0, _szInt);
                            Buffer.BlockCopy(dat, 0, _memory, _destInt, _szInt);
                        }
                        break;
                    case DVMOpcodeV1.PUSH0:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = UInt256.Zero;
                        }
                        break;
                    case DVMOpcodeV1.PUSH1:
                    case DVMOpcodeV1.PUSH2:
                    case DVMOpcodeV1.PUSH3:
                    case DVMOpcodeV1.PUSH4:
                    case DVMOpcodeV1.PUSH5:
                    case DVMOpcodeV1.PUSH6:
                    case DVMOpcodeV1.PUSH7:
                    case DVMOpcodeV1.PUSH8:
                    case DVMOpcodeV1.PUSH9:
                    case DVMOpcodeV1.PUSH10:
                    case DVMOpcodeV1.PUSH11:
                    case DVMOpcodeV1.PUSH12:
                    case DVMOpcodeV1.PUSH13:
                    case DVMOpcodeV1.PUSH14:
                    case DVMOpcodeV1.PUSH15:
                    case DVMOpcodeV1.PUSH16:
                    case DVMOpcodeV1.PUSH17:
                    case DVMOpcodeV1.PUSH18:
                    case DVMOpcodeV1.PUSH19:
                    case DVMOpcodeV1.PUSH20:
                    case DVMOpcodeV1.PUSH21:
                    case DVMOpcodeV1.PUSH22:
                    case DVMOpcodeV1.PUSH23:
                    case DVMOpcodeV1.PUSH24:
                    case DVMOpcodeV1.PUSH25:
                    case DVMOpcodeV1.PUSH26:
                    case DVMOpcodeV1.PUSH27:
                    case DVMOpcodeV1.PUSH28:
                    case DVMOpcodeV1.PUSH29:
                    case DVMOpcodeV1.PUSH30:
                    case DVMOpcodeV1.PUSH31:
                    case DVMOpcodeV1.PUSH32:
                        {
                            var _blen = (int)(op - (byte)DVMOpcodeV1.PUSH0);

                            if (_pc + _blen >= _script.Code.Length)
                            {
                                return new VerifyException("DVM", "PUSH script overflow");
                            }

                            byte[] _data = new byte[32];
                            Array.Copy(_script.Code, _pc + 1, _data, 32 - _blen, _blen);

                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = new UInt256(_data.AsSpan(), true);
                            _pc += (uint)_blen; // add size of immediate to program counter
                        }
                        break;
                    case DVMOpcodeV1.DUP1:
                    case DVMOpcodeV1.DUP2:
                    case DVMOpcodeV1.DUP3:
                    case DVMOpcodeV1.DUP4:
                    case DVMOpcodeV1.DUP5:
                    case DVMOpcodeV1.DUP6:
                    case DVMOpcodeV1.DUP7:
                    case DVMOpcodeV1.DUP8:
                    case DVMOpcodeV1.DUP9:
                    case DVMOpcodeV1.DUP10:
                    case DVMOpcodeV1.DUP11:
                    case DVMOpcodeV1.DUP12:
                    case DVMOpcodeV1.DUP13:
                    case DVMOpcodeV1.DUP14:
                    case DVMOpcodeV1.DUP15:
                    case DVMOpcodeV1.DUP16:
                        {
                            var _offlen = (int)(op - (byte)DVMOpcodeV1.DUP1) + 1;

                            if (_sp < _offlen)
                            {
                                return new VerifyException("DVM", "DUP parameter too low on stack (underflow)");
                            }

                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp] = _stack[_sp - _offlen];
                            _sp++;
                        }
                        break;
                    case DVMOpcodeV1.SWAP1:
                    case DVMOpcodeV1.SWAP2:
                    case DVMOpcodeV1.SWAP3:
                    case DVMOpcodeV1.SWAP4:
                    case DVMOpcodeV1.SWAP5:
                    case DVMOpcodeV1.SWAP6:
                    case DVMOpcodeV1.SWAP7:
                    case DVMOpcodeV1.SWAP8:
                    case DVMOpcodeV1.SWAP9:
                    case DVMOpcodeV1.SWAP10:
                    case DVMOpcodeV1.SWAP11:
                    case DVMOpcodeV1.SWAP12:
                    case DVMOpcodeV1.SWAP13:
                    case DVMOpcodeV1.SWAP14:
                    case DVMOpcodeV1.SWAP15:
                    case DVMOpcodeV1.SWAP16:
                        {
                            var _offlen = (int)(op - (byte)DVMOpcodeV1.SWAP1) + 2;

                            if (_sp < _offlen)
                            {
                                return new VerifyException("DVM", "SWAP parameter too low on stack (underflow)");
                            }

                            (_stack[_sp - 1], _stack[_sp - _offlen]) = (_stack[_sp - _offlen], _stack[_sp - 1]);
                        }
                        break;
                    case DVMOpcodeV1.POP:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "POP stack underflow");
                            }

                            _sp--;
                        }
                        break;
                    case DVMOpcodeV1.JMPD:
                        break; // currently unused
                    case DVMOpcodeV1.CODSZ:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)_script.Code.Length;
                        }
                        break;
                    case DVMOpcodeV1.DATSZ:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)datum.Data.Length;
                        }
                        break;
                    case DVMOpcodeV1.RDMSZ:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)redeemer.Data.Length;
                        }
                        break;
                    case DVMOpcodeV1.ADDR:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            byte[] dat = new byte[32];
                            Buffer.BlockCopy(ctx.Inputs[index].Resolved.Address.Bytes(), 0, dat, 7, 25);
                            _stack[_sp++] = new UInt256(dat, true);
                        }
                        break;
                    case DVMOpcodeV1.VALUE:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)ctx.Inputs[index].Resolved.Amount;
                        }
                        break;
                    case DVMOpcodeV1.INDEX:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)index;
                        }
                        break;
                    case DVMOpcodeV1.CTXFEE:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)ctx.Fee;
                        }
                        break;
                    case DVMOpcodeV1.CTXHASH:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = new UInt256(ctx.SigningHash.Bytes.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.CTXINTL:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)ctx.ValidityInterval.Item1;
                        }
                        break;
                    case DVMOpcodeV1.CTXINTH:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)ctx.ValidityInterval.Item2;
                        }
                        break;
                    case DVMOpcodeV1.CTXTKEY:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            if (ctx.TransactionKey != null && ctx.TransactionKey.Value.bytes != null)
                            {
                                _stack[_sp++] = new UInt256(ctx.TransactionKey.Value.bytes.AsSpan(), true);
                            }
                            else
                            {
                                _stack[_sp++] = UInt256.Zero;
                            }
                        }
                        break;
                    case DVMOpcodeV1.NUMIN:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)(ctx.Inputs?.Length ?? 0);
                        }
                        break;
                    case DVMOpcodeV1.INREFID:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "INREFID has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Inputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "INREFID index greater than number of inputs");
                            }

                            _stack[_sp++] = new UInt256(ctx.Inputs[(int)_index].Reference.TxSrc.Bytes.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.INREFIX:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "INREFIX has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Inputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "INREFIX index greater than number of inputs");
                            }

                            _stack[_sp++] = (UInt256)ctx.Inputs[(int)_index].Reference.Offset;
                        }
                        break;
                    case DVMOpcodeV1.INADDR:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "INADDR has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Inputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "INADDR index greater than number of inputs");
                            }

                            byte[] dat = new byte[32];
                            Buffer.BlockCopy(ctx.Inputs[(int)_index].Resolved.Address.Bytes(), 0, dat, 7, 25);
                            _stack[_sp++] = new UInt256(dat, true);
                        }
                        break;
                    case DVMOpcodeV1.INVAL:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "INVAL has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Inputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "INVAL index greater than number of inputs");
                            }

                            _stack[_sp++] = (UInt256)ctx.Inputs[(int)_index].Resolved.Amount;
                        }
                        break;
                    case DVMOpcodeV1.INRSC:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "INRSC has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Inputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "INRSC index greater than number of inputs");
                            }

                            byte[] dat = new byte[32];
                            if (ctx.Inputs[(int)_index].Resolved.ReferenceScript != null)
                            {
                                Buffer.BlockCopy(new ScriptAddress(ctx.Inputs[(int)_index].Resolved.ReferenceScript).Bytes(), 0, dat, 7, 25);
                            }
                            _stack[_sp++] = new UInt256(dat, true);
                        }
                        break;
                    case DVMOpcodeV1.INDATL:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "INDATL has insufficient stack arguments; expected 2");
                            }

                            var _index = _stack[--_sp];
                            var _offset = _stack[--_sp];

                            if (_index >= (ctx.Inputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "INDATL index greater than number of inputs");
                            }

                            var _err = LoadFromDatumToStack(_offset, ctx.Inputs[(int)_index].Resolved.Datum ?? _nullDatum);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.INDATCP:
                        {
                            if (_sp < 4)
                            {
                                return new VerifyException("DVM", "INDATCP has insufficient stack arguments; expected 4");
                            }

                            var _index = _stack[--_sp];
                            var _dest = _stack[--_sp];
                            var _offset = _stack[--_sp];
                            var _sz = _stack[--_sp];

                            if (_index >= (ctx.Inputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "INDATCP index greater than number of inputs");
                            }

                            var _err = LoadFromDatum(_dest, _offset, ctx.Inputs[(int)_index].Resolved.Datum ?? _nullDatum, _sz);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.INDATH:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "INDATH has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Inputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "INDATH index greater than number of inputs");
                            }

                            var _indexInt = (int)_index;
                            if (ctx.Inputs[_indexInt].Resolved.DatumHash == null && ctx.Inputs[_indexInt].Resolved.Datum != null)
                            {
                                ctx.Inputs[_indexInt].Resolved.DatumHash = ctx.Inputs[_indexInt].Resolved.Datum.Hash();
                            }
                            byte[] data = (ctx.Inputs[_indexInt].Resolved.DatumHash == null) ? null : ctx.Inputs[_indexInt].Resolved.DatumHash.Value.Bytes;
                            _stack[_sp++] = new UInt256(data.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.INDATSZ:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "INDATSZ has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Inputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "INDATSZ index greater than number of inputs");
                            }

                            _stack[_sp++] = (UInt256)(ctx.Inputs[(int)_index].Resolved.Datum?.Data?.Length ?? 0);
                        }
                        break;
                    case DVMOpcodeV1.NUMRIN:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)(ctx.ReferenceInputs?.Length ?? 0);
                        }
                        break;
                    case DVMOpcodeV1.RINREFID:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "RINREFID has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.ReferenceInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "RINREFID index greater than number of inputs");
                            }

                            _stack[_sp++] = new UInt256(ctx.ReferenceInputs[(int)_index].Reference.TxSrc.Bytes.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.RINREFIX:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "RINREFIX has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.ReferenceInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "RINREFIX index greater than number of inputs");
                            }

                            _stack[_sp++] = (UInt256)ctx.ReferenceInputs[(int)_index].Reference.Offset;
                        }
                        break;
                    case DVMOpcodeV1.RINADDR:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "RINADDR has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.ReferenceInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "RINADDR index greater than number of inputs");
                            }

                            byte[] dat = new byte[32];
                            Buffer.BlockCopy(ctx.ReferenceInputs[(int)_index].Resolved.Address.Bytes(), 0, dat, 7, 25);
                            _stack[_sp++] = new UInt256(dat, true);
                        }
                        break;
                    case DVMOpcodeV1.RINVAL:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "RINVAL has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.ReferenceInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "RINVAL index greater than number of inputs");
                            }

                            _stack[_sp++] = (UInt256)ctx.ReferenceInputs[(int)_index].Resolved.Amount;
                        }
                        break;
                    case DVMOpcodeV1.RINRSC:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "RINRSC has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.ReferenceInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "RINRSC index greater than number of inputs");
                            }

                            byte[] dat = new byte[32];
                            if (ctx.ReferenceInputs[(int)_index].Resolved.ReferenceScript != null)
                            {
                                Buffer.BlockCopy(new ScriptAddress(ctx.ReferenceInputs[(int)_index].Resolved.ReferenceScript).Bytes(), 0, dat, 7, 25);
                            }
                            _stack[_sp++] = new UInt256(dat, true);
                        }
                        break;
                    case DVMOpcodeV1.RINDATL:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "RINDATL has insufficient stack arguments; expected 2");
                            }

                            var _index = _stack[--_sp];
                            var _offset = _stack[--_sp];

                            if (_index >= (ctx.ReferenceInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "RINDATL index greater than number of inputs");
                            }

                            var _err = LoadFromDatumToStack(_offset, ctx.ReferenceInputs[(int)_index].Resolved.Datum ?? _nullDatum);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.RINDATCP:
                        {
                            if (_sp < 4)
                            {
                                return new VerifyException("DVM", "RINDATCP has insufficient stack arguments; expected 4");
                            }

                            var _index = _stack[--_sp];
                            var _dest = _stack[--_sp];
                            var _offset = _stack[--_sp];
                            var _sz = _stack[--_sp];

                            if (_index >= (ctx.ReferenceInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "RINDATCP index greater than number of inputs");
                            }

                            var _err = LoadFromDatum(_dest, _offset, ctx.ReferenceInputs[(int)_index].Resolved.Datum ?? _nullDatum, _sz);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.RINDATH:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "RINDATH has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.ReferenceInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "RINDATH index greater than number of inputs");
                            }

                            var _indexInt = (int)_index;
                            if (ctx.ReferenceInputs[_indexInt].Resolved.DatumHash == null && ctx.ReferenceInputs[_indexInt].Resolved.Datum != null)
                            {
                                ctx.ReferenceInputs[_indexInt].Resolved.DatumHash = ctx.ReferenceInputs[_indexInt].Resolved.Datum.Hash();
                            }
                            byte[] data = (ctx.ReferenceInputs[_indexInt].Resolved.DatumHash == null) ? null : ctx.ReferenceInputs[_indexInt].Resolved.DatumHash.Value.Bytes;
                            _stack[_sp++] = new UInt256(data.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.RINDATSZ:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "RINDATSZ has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.ReferenceInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "RINDATSZ index greater than number of inputs");
                            }

                            _stack[_sp++] = (UInt256)(ctx.ReferenceInputs[(int)_index].Resolved.Datum?.Data?.Length ?? 0);
                        }
                        break;
                    case DVMOpcodeV1.NUMOUT:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)(ctx.Outputs?.Length ?? 0);
                        }
                        break;
                    case DVMOpcodeV1.OUTADDR:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "OUTADDR has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Outputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "OUTADDR index greater than number of inputs");
                            }

                            byte[] dat = new byte[32];
                            Buffer.BlockCopy(ctx.Outputs[(int)_index].Address.Bytes(), 0, dat, 7, 25);
                            _stack[_sp++] = new UInt256(dat, true);
                        }
                        break;
                    case DVMOpcodeV1.OUTVAL:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "OUTVAL has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Outputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "OUTVAL index greater than number of inputs");
                            }

                            _stack[_sp++] = (UInt256)ctx.Outputs[(int)_index].Amount;
                        }
                        break;
                    case DVMOpcodeV1.OUTRSC:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "OUTRSC has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Outputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "OUTRSC index greater than number of inputs");
                            }

                            byte[] dat = new byte[32];
                            if (ctx.Outputs[(int)_index].ReferenceScript != null)
                            {
                                Buffer.BlockCopy(new ScriptAddress(ctx.Outputs[(int)_index].ReferenceScript).Bytes(), 0, dat, 7, 25);
                            }
                            _stack[_sp++] = new UInt256(dat, true);
                        }
                        break;
                    case DVMOpcodeV1.OUTDATL:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "OUTDATL has insufficient stack arguments; expected 2");
                            }

                            var _index = _stack[--_sp];
                            var _offset = _stack[--_sp];

                            if (_index >= (ctx.Outputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "OUTDATL index greater than number of inputs");
                            }

                            var _err = LoadFromDatumToStack(_offset, ctx.Outputs[(int)_index].Datum ?? _nullDatum);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.OUTDATCP:
                        {
                            if (_sp < 4)
                            {
                                return new VerifyException("DVM", "OUTDATCP has insufficient stack arguments; expected 4");
                            }

                            var _index = _stack[--_sp];
                            var _dest = _stack[--_sp];
                            var _offset = _stack[--_sp];
                            var _sz = _stack[--_sp];

                            if (_index >= (ctx.Outputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "OUTDATCP index greater than number of inputs");
                            }

                            var _err = LoadFromDatum(_dest, _offset, ctx.Outputs[(int)_index].Datum ?? _nullDatum, _sz);
                            if (_err != null)
                            {
                                return _err;
                            }
                        }
                        break;
                    case DVMOpcodeV1.OUTDATH:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "OUTDATH has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Outputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "OUTDATH index greater than number of inputs");
                            }

                            var _indexInt = (int)_index;
                            if (ctx.Outputs[_indexInt].DatumHash == null && ctx.Outputs[_indexInt].Datum != null)
                            {
                                ctx.Outputs[_indexInt].DatumHash = ctx.Outputs[_indexInt].Datum.Hash();
                            }
                            byte[] data = (ctx.Outputs[_indexInt].DatumHash == null) ? null : ctx.Outputs[_indexInt].DatumHash.Value.Bytes;
                            _stack[_sp++] = new UInt256(data.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.OUTDATSZ:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "OUTDATSZ has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Outputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "OUTDATSZ index greater than number of inputs");
                            }

                            _stack[_sp++] = (UInt256)(ctx.Outputs[(int)_index].Datum?.Data?.Length ?? 0);
                        }
                        break;
                    case DVMOpcodeV1.NUMPIN:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)(ctx.PrivateInputs?.Length ?? 0);
                        }
                        break;
                    case DVMOpcodeV1.PINTAG:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "PINTAG has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.PrivateInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "PINTAG index greater than number of inputs");
                            }

                            _stack[_sp++] = new UInt256(ctx.PrivateInputs[(int)_index].KeyImage.bytes.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.PINIXA:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "PINIXA has insufficient stack arguments; expected 2");
                            }

                            var _index = _stack[--_sp];
                            var _jindex = _stack[--_sp];
                            if (_index >= (ctx.PrivateInputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "PINIXA index greater than number of inputs");
                            }

                            if (_jindex > 63)
                            {
                                return new VerifyException("DVM", "PINIXA mixin index greater than number of mixins");
                            }

                            _stack[_sp++] = (UInt256)ctx.PrivateInputs[(int)_index].Offsets[(int)_jindex];
                        }
                        break;
                    case DVMOpcodeV1.NUMPOUT:
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)(ctx.PrivateOutputs?.Length ?? 0);
                        }
                        break;
                    case DVMOpcodeV1.POUTUKEY:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "POUTUKEY has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.PrivateOutputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "POUTUKEY index greater than number of inputs");
                            }

                            _stack[_sp++] = new UInt256(ctx.PrivateOutputs[(int)_index].UXKey.bytes.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.POUTCOM:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "POUTCOM has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.PrivateOutputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "POUTCOM index greater than number of inputs");
                            }

                            _stack[_sp++] = new UInt256(ctx.PrivateOutputs[(int)_index].Commitment.bytes.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.POUTAMT:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "POUTAMT has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.PrivateOutputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "POUTAMT index greater than number of inputs");
                            }

                            _stack[_sp++] = (UInt256)ctx.PrivateOutputs[(int)_index].Amount;
                        }
                        break;
                    case DVMOpcodeV1.NUMSIG: // TODO: signatures are kind of strange
                        {
                            if (_sp >= MAX_STACK_V1)
                            {
                                return new VerifyException("DVM", "Stack overflow");
                            }

                            _stack[_sp++] = (UInt256)(ctx.Inputs?.Length ?? 0);
                        }
                        break;
                    case DVMOpcodeV1.SIGADDR:
                        {
                            if (_sp < 1)
                            {
                                return new VerifyException("DVM", "SIGADDR has insufficient stack arguments; expected 1");
                            }

                            var _index = _stack[--_sp];
                            if (_index >= (ctx.Outputs?.Length ?? 0))
                            {
                                return new VerifyException("DVM", "SIGADDR index greater than number of inputs");
                            }

                            byte[] dat = new byte[32];
                            Buffer.BlockCopy(ctx.Inputs[(int)_index].Resolved.Address.Bytes(), 0, dat, 7, 25);
                            _stack[_sp++] = new UInt256(dat, true);
                        }
                        break;
                    case DVMOpcodeV1.SHA2:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "SHA2 has insufficient stack arguments; expected 2");
                            }

                            var _offset = _stack[--_sp];
                            var _sz = _stack[--_sp];

                            if (_offset + _sz > MAX_MEMORY_V1)
                            {
                                return new VerifyException("DVM", "SHA2 offset out of bounds");
                            }

                            var _err = CheckMemory((int)_offset, -1, (int)_sz);
                            if (_err != null)
                            {
                                return _err;
                            }

                            byte[] data = new byte[(int)_sz];
                            Buffer.BlockCopy(_memory, (int)_offset, data, 0, (int)_sz);
                            _stack[_sp++] = new UInt256(SHA256.HashData(data).Bytes.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.KECC:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "KECC has insufficient stack arguments; expected 2");
                            }

                            var _offset = _stack[--_sp];
                            var _sz = _stack[--_sp];

                            if (_offset + _sz > MAX_MEMORY_V1)
                            {
                                return new VerifyException("DVM", "KECC offset out of bounds");
                            }

                            var _err = CheckMemory((int)_offset, -1, (int)_sz);
                            if (_err != null)
                            {
                                return _err;
                            }

                            byte[] data = new byte[(int)_sz];
                            Buffer.BlockCopy(_memory, (int)_offset, data, 0, (int)_sz);
                            _stack[_sp++] = new UInt256(Keccak.HashData(data).Bytes.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.RIPEMD:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "RIPEMD has insufficient stack arguments; expected 2");
                            }

                            var _offset = _stack[--_sp];
                            var _sz = _stack[--_sp];

                            if (_offset + _sz > MAX_MEMORY_V1)
                            {
                                return new VerifyException("DVM", "RIPEMD offset out of bounds");
                            }

                            var _err = CheckMemory((int)_offset, -1, (int)_sz);
                            if (_err != null)
                            {
                                return _err;
                            }

                            byte[] data = new byte[(int)_sz];
                            Buffer.BlockCopy(_memory, (int)_offset, data, 0, (int)_sz);
                            byte[] dat = new byte[32];
                            Buffer.BlockCopy(RIPEMD160.HashData(data).Bytes, 0, dat, 12, 20);
                            _stack[_sp++] = new UInt256(dat.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.EDVER:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "EDVER has insufficient stack arguments; expected 2");
                            }

                            var _h = _stack[--_sp];
                            var _offset = _stack[--_sp];

                            if (_offset + 96 > MAX_MEMORY_V1)
                            {
                                return new VerifyException("DVM", "EDVER offset out of bounds");
                            }

                            var _err = CheckMemory((int)_offset, -1, 96);
                            if (_err != null)
                            {
                                return _err;
                            }

                            byte[] sig = new byte[96];
                            Buffer.BlockCopy(_memory, (int)_offset, sig, 0, 96);
                            _stack[_sp++] = new Signature(sig).Verify(new SHA256(_h.ToBigEndian(), false)) ? UInt256.One : UInt256.Zero;
                        }
                        break;
                    case DVMOpcodeV1.EDSGN:
                        {
                            if (_sp < 4)
                            {
                                return new VerifyException("DVM", "EDSGN has insufficient stack arguments; expected 4");
                            }

                            var _h = _stack[--_sp];
                            var _sk = _stack[--_sp];
                            var _pk = _stack[--_sp];
                            var _dest = _stack[--_sp];

                            if (_dest + 96 > MAX_MEMORY_V1)
                            {
                                return new VerifyException("DVM", "EDSGN offset out of bounds");
                            }

                            var _err = CheckMemory(-1, (int)_dest, 96);
                            if (_err != null)
                            {
                                return _err;
                            }

                            var sig = new Signature(new Key(_sk.ToBigEndian()), new Key(_pk.ToBigEndian()), new SHA256(_h.ToBigEndian(), false));
                            var sigbytes = sig.ToBytes();
                            Buffer.BlockCopy(sigbytes, 0, _memory, (int)_dest, 96);
                        }
                        break;
                    case DVMOpcodeV1.COMM:
                        {
                            if (_sp < 2)
                            {
                                return new VerifyException("DVM", "COMM has insufficient stack arguments; expected 2");
                            }

                            var _mask = _stack[--_sp];
                            var _amt = _stack[--_sp];

                            if (_amt > ulong.MaxValue)
                            {
                                return new VerifyException("DVM", "COMM second argument must fit into 64-bit value");
                            }

                            Key mask = new Key(_mask.ToBigEndian());
                            var comm = KeyOps.Commit(ref mask, (ulong)_amt);
                            _stack[_sp++] = new UInt256(comm.bytes.AsSpan(), true);
                        }
                        break;
                    case DVMOpcodeV1.NOP:
                        {

                        }
                        break;
                    case DVMOpcodeV1.SETERR:
                        {
                            strict = true;
                        }
                        break;
                    case DVMOpcodeV1.SETSAFE:
                        {
                            strict = false;
                        }
                        break;
                    case DVMOpcodeV1.FAIL:
                        {
                            success = UInt256.Zero;
                        }
                        break;
                    case DVMOpcodeV1.SUCC:
                        {
                            success = UInt256.One;
                        }
                        break;
                    case DVMOpcodeV1.IREX:
                    case DVMOpcodeV1.BOOM:
                    case DVMOpcodeV1.INVLD:
                    default:
                        {
                            success = UInt256.Zero;
                        }
                        break;
                }

                if (debug)
                {
                    Console.WriteLine($"{((DVMOpcodeV1)op),-16} ; [{GetStackPrint()}]");
                }

                if (jmp)
                {
                    jmp = false;
                }
                else
                {
                    _pc++;
                }

                _cost++;
            }

            if (_cost >= MAX_INSTRUCTIONS_V1)
            {
                return new VerifyException("DVM", "Exceeded maximum execution cost");
            }

            return (success.Value > 0) ? null : new VerifyException("DVM", "Script did not validate");
        }

        protected string GetStackPrint()
        {
            string stk = "";
            for (int i = (int)_sp - 1; i >= 0; i--)
            {
                var val = _stack[i];
                if (val > int.MaxValue)
                {
                    var bytes = val.ToBigEndian();
                    var len = 31;
                    while (len >= 0 && bytes[31 - len] == 0) len--;
                    len++;
                    if (len == 25)
                    {
                        var addr = new TAddress(bytes[7..]);
                        stk += $"{addr.ToString()[..8]}{(_sp > 0 ? " " : "")}";
                    }
                    else
                    {
                        int b = 0;
                        while (bytes[b] == 0) b++;
                        var hex = Printable.Hexify(bytes[b..]);
                        hex = hex[..(hex.Length > 20 ? 20 : hex.Length)];
                        stk += $"{hex}{(_sp > 0 ? " " : "")}";
                    }
                }
                else
                {
                    stk += $"{(int)val}{(_sp > 0 ? " " : "")}";
                }
            }

            return stk;
        }

        public static VerifyException Verify(ScriptContext ctx, int index, ChainScript script, Datum datum, Datum redeemer)
        {
            DVM dvm = new DVM(script);
            try
            {
                return dvm.Execute(index, datum, redeemer, ctx);
            }
            catch (Exception e)
            {
                return new VerifyException("DVM", $"Internal error encountered validating script: {e.Message}");
            }
        }
    }
}
