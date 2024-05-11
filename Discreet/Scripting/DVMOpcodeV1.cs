using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Scripting
{
    public enum DVMOpcodeV1 : byte
    {
        /* halt */
        HALT = 0, // halts execution. if the current stack element is zero, succeeds. otherwise fails.

        /* arithmetic */
        ADD = 1, // addition
        SUB = 2, // subtraction
        MUL = 3, // multiplication (mod word size)
        DIV = 4, // integer division 
        SDIV = 5, // signed integer division
        MOD = 6, // modulo operator
        SMOD = 7, // signed modulo operator
        MADD = 8, // addition mod N
        MMUL = 9, // multiplication mod N
        MEXP = 10, // exponentiation mod N
        SGEX = 11, // sign extend
        LT = 12, // less-than comparison
        GT = 13, // greater-than comparison
        SLT = 14, // signed less-than comparison
        SGT = 15, // signed greater-than comparison
        EQ = 16, // 1 if left == right, 0 otherwise
        ISZ = 17, // 1 if zero; 0 otherwise
        AND = 18, // bitwise and
        OR = 19, // bitwise or
        XOR = 20, // exclusive or
        NOT = 21, // one's compliment
        BMSK = 22, // bytemask
        SHL = 23, // left shift
        SHR = 24, // right shift
        SAR = 25, // signed arithmetic right shift
        // codes 26-31 reserved, map to HLT

        /* core memory, data and call */
        LOAD = 32, // loads a word from memory
        STOR = 33, // stores the word to memory
        STOR8 = 34, // stores the least significant byte of position 0 in memory 
        JMP = 35, // jumps to stack position 0
        JMPC = 36, // jumps to stack position 0 iff position 1 is nonzero
        CALL = 37, // pops stack to get jump dest and pushes current PC+x to stack
        CALLI = 38, // pops stack to get jump destination, pushes current PC+x to stack, and marks Y entries of stack to be moved down before the PC+x push
        RET = 39, // pops the stack and jumps to the value popped
        RDML = 40, // loads "call data" from redeemer
        RDMCP = 41, // copies data from redeemer to memory
        CODCP = 42, // copies code to memory
        DATL = 43, // loads persistent data from our datum
        DATCP = 44, // copies data from our datum to memory
        PC = 45, // gets the current program counter prior to this instruction
        MCP = 46, // copies memory from one location to another

        /* stack operations */
        PUSH0 = 47, // pushes 0 to stack
        PUSH1 = 48, // pushes 1 byte immediate to stack
        PUSH2 = 49, // ...
        PUSH3 = 50,
        PUSH4 = 51,
        PUSH5 = 52,
        PUSH6 = 53,
        PUSH7 = 54,
        PUSH8 = 55,
        PUSH9 = 56,
        PUSH10 = 57,
        PUSH11 = 58,
        PUSH12 = 59,
        PUSH13 = 60,
        PUSH14 = 61,
        PUSH15 = 62,
        PUSH16 = 63,
        PUSH17 = 64,
        PUSH18 = 65,
        PUSH19 = 66,
        PUSH20 = 67,
        PUSH21 = 68,
        PUSH22 = 69,
        PUSH23 = 70,
        PUSH24 = 71,
        PUSH25 = 72,
        PUSH26 = 73,
        PUSH27 = 74,
        PUSH28 = 75,
        PUSH29 = 76,
        PUSH30 = 77,
        PUSH31 = 78,
        PUSH32 = 79,
        DUP1 = 80, // duplicates the stack word in position 1 (eg. stack(a b) -> stack(a a b))
        DUP2 = 81, // duplicates the stack word in position 2 (eg. stack(a b) -> stack(b a b))
        DUP3 = 82, // duplicates the stack word in position 3 (eg. stack(a b c) -> stack(c a b c))
        DUP4 = 83, // ...
        DUP5 = 84,
        DUP6 = 85,
        DUP7 = 86,
        DUP8 = 87,
        DUP9 = 88,
        DUP10 = 89,
        DUP11 = 90,
        DUP12 = 91,
        DUP13 = 92,
        DUP14 = 93,
        DUP15 = 94,
        DUP16 = 95,
        SWAP1 = 96, // swaps stack words in positions 1 and 2 (eg. stack(a b) -> stack(b a))
        SWAP2 = 97, // swaps stack words in positions 1 and 3 (eg. stack(a b c) -> stack(c b a))
        SWAP3 = 98, // swaps stack words in positions 1 and 4 (eg. stack(a b c d) -> stack(d b c a))
        SWAP4 = 99, // ...
        SWAP5 = 100,
        SWAP6 = 101,
        SWAP7 = 102,
        SWAP8 = 103,
        SWAP9 = 104,
        SWAP10 = 105,
        SWAP11 = 106,
        SWAP12 = 107,
        SWAP13 = 108,
        SWAP14 = 109,
        SWAP15 = 110,
        SWAP16 = 111,
        // 112-127 are reserved for future stack operations

        /* additional operations */
        POP = 128, // pops the stack
        JMPD = 129, // allows a region of memory to be marked as executable
        CODSZ = 130, // gets the current length of the code.
        DATSZ = 131, // gets the size of the datum attached to the script
        RDMSZ = 132, // gets the size of the redeemer used for the script
        // 133-159 are reserved for additional operations

        /* context operations */
        ADDR = 160, // gets the address of the contract
        VALUE = 161, // gets the value attached to the contract
        INDEX = 162, // gets the index of the contract in the transaction
        CTXFEE = 163, // gets the fee from the context
        CTXHASH = 164, // gets the InnerHash/SigningHash of the transaction from the context
        CTXINTL = 165, // gets the lower bound interval of the transaction from context
        CTXINTH = 166, // gets upper bound interval of the transaction from context
        CTXTKEY = 167, // gets the transaction key from the context, if present; otherwise 0
        NUMIN = 168, // gets number of transparent inputs from the context
        INREFID = 169, // gets the txid part of the txinput 
        INREFIX = 170, // gets the index part of the txinput
        INADDR = 171, // gets the address of the txinput
        INVAL = 172, // gets the value attached to the txinput
        INRSC = 173, // gets the reference script address attached to the txinput, if present; otherwise null address
        INDATL = 174, // loads a word from the specified offset in the txinput's datum, if possible. returns 0 if in safe-mode.
        INDATCP = 175, // copies data from txinput's datum into memory, if possible. NOP if in safe-mode.
        INDATH = 176, // gets the datum hash from the txinput's datum. returns null hash if no datum is present.
        INDATSZ = 177, // gets the size of the txinput's datum.
        NUMRIN = 178, // gets the number of reference inputs from the context.
        RINREFID = 179, // gets the txid part of the reference input.
        RINREFIX = 180, // gets the index part of the reference input.
        RINADDR = 181, // gets the address of the reference input.
        RINVAL = 182, // gets the value attached to the reference input.
        RINRSC = 183, // gets the reference script address attached to the reference input, if present; otherwise null address.
        RINDATL = 184, // loads a word from the specified offset in the reference input's datum, if possible. returns 0 in safe mode.
        RINDATCP = 185, // copies data from the reference input's datum into memory, if possible. NOP if in safe-mode.
        RINDATH = 186, // gets the datum hash from the reference input's datum. returns null hash if no datum is present.
        RINDATSZ = 187, // gets the size of the reference input's datum.
        NUMOUT = 188, // gets the number of transparent outputs.
        OUTADDR = 189, // gets the address of the txoutput.
        OUTVAL = 190, // gets the value attached to the txoutput.
        OUTRSC = 191, // gets the reference script attached to the txoutput, if present. otherwise null address.
        OUTDATL = 192, // loads a word from the txoutput's datum, if possible. otherwise returns 0 if in safe-mode.
        OUTDATCP = 193, // copies data from the txoutput's datum into memory, if possible. NOP in safe mode.
        OUTDATH = 194, // gets the hash of the txoutput's datum, if present. otherwise null hash.
        OUTDATSZ = 195, // gets the size of the txoutput's datum.
        NUMPIN = 196, // gets the number of private inputs.
        PINTAG = 197, // gets the linking tag of the private input.
        PINIXA = 198, // gets the private input's jth mixin index (restricted to 64; saturated in safe mode). 
        NUMPOUT = 199, // gets the number of private outputs.
        POUTUKEY = 200, // gets the private output's UXKey.
        POUTCOM = 201, // gets the private output's commitment.
        POUTAMT = 202, // gets the encrypted output amount in the private output.
        NUMSIG = 203, // gets the number of transparent signatures attached to the transaction.
        SIGADDR = 204, // gets the address corresponding to the ith signature. If no signature, returns the corresponding script address at that index.
        // 205-207 are reserved for new context operations

        /* builtin crypto operations */
        SHA2 = 208, // performs sha256 hash on specified memory region.
        KECC = 209, // performs keccak hash on specified memory region.
        RIPEMD = 210, // performs ripemd160 hash on specified memory region.
        EDVER = 211, // performs ed25519 verification on specified memory region.
        EDSGN = 212, // performs ed25519 signature on specified hash, private key, and public key.
        COMM = 213, // performs aG + bH pedersen commitment on specified mask a and value b.

        /* system operations */
        NOP = 248, // does nothing.
        SETERR = 249, // flags the DVM to run in strict errant mode. errant instructions will halt the DVM instead of returning 0 or NOP
        SETSAFE = 250, // un-flags the DVM and sets it to run in safe mode. errant instructions (eg. INDATL) will return 0 instead of halting the machine.
        FAIL = 251, // halts execution and forcefully returns a non-success to the validator.
        SUCC = 252, // halts execution and forcefully returns a success to the validator.
        IREX = 253, // reserved instruction intended for "instruction routine extensions". 
        INVLD = 254, // invalid instruction.
        BOOM = 255, // reserved instruction intended for miscellaneous operations.
    }
}
