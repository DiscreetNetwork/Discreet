using Discreet.Cipher;
using Discreet.Coin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Coin.Utilities
{
    public static class MockUtil
    {
        public static TXInput GenerateMockTXInput()
        {
            TXInput input = new TXInput();

            input.KeyImage = KeyOps.GeneratePubkey();
            input.Offsets = new uint[64];
            Random rng = new Random();

            for (int i = 0; i < 64; i++)
            {
                input.Offsets[i] = (uint)rng.Next(0, int.MaxValue);
            }

            return input;
        }

        public static TXOutput GenerateMockTXOutput()
        {
            TXOutput output = new();
            Random rng = new();

            ulong v1 = (ulong)rng.Next(0, int.MaxValue) << 32;
            ulong v2 = (ulong)rng.Next(0, int.MaxValue);
            output.Amount = v1 | v2;

            output.UXKey = KeyOps.GeneratePubkey();
            output.Commitment = KeyOps.GeneratePubkey();
            output.TransactionSrc = new SHA256(new byte[32], false);

            return output;
        }

        public static Models.Bulletproof GenerateMockBulletproof()
        {
            Models.Bulletproof bp = new Models.Bulletproof();
            bp.size = 7;
            bp.A = KeyOps.GeneratePubkey();
            bp.S = KeyOps.GeneratePubkey();
            bp.T1 = KeyOps.GeneratePubkey();
            bp.T2 = KeyOps.GeneratePubkey();
            bp.taux = KeyOps.GenerateSeckey();
            bp.mu = KeyOps.GenerateSeckey();

            bp.L = new Key[7];
            bp.R = new Key[7];

            for (int i = 0; i < 7; i++)
            {
                bp.L[i] = KeyOps.GeneratePubkey();
                bp.R[i] = KeyOps.GeneratePubkey();
            }

            bp.a = KeyOps.GenerateSeckey();
            bp.b = KeyOps.GenerateSeckey();
            bp.t = KeyOps.GenerateSeckey();

            return bp;
        }

        public static Models.Triptych GenerateMockTriptych()
        {
            var proof = new Models.Triptych();

            proof.K = KeyOps.GeneratePubkey();
            proof.A = KeyOps.GeneratePubkey();
            proof.B = KeyOps.GeneratePubkey();
            proof.C = KeyOps.GeneratePubkey();
            proof.D = KeyOps.GeneratePubkey();

            proof.X = new Key[6];
            proof.Y = new Key[6];
            proof.f = new Key[6];

            for (int i = 0; i < 6; i++)
            {
                proof.X[i] = KeyOps.GeneratePubkey();
                proof.Y[i] = KeyOps.GeneratePubkey();
                proof.f[i] = KeyOps.GenerateSeckey();
            }

            proof.zA = KeyOps.GenerateSeckey();
            proof.zC = KeyOps.GenerateSeckey();
            proof.z = KeyOps.GenerateSeckey();

            return proof;
        }

        public static Transaction GenerateMockTransaction()
        {
            Transaction tx = new();
            tx.Version = 1;
            tx.NumInputs = 2;
            tx.NumOutputs = 2;
            tx.NumSigs = 2;

            tx.Inputs = new TXInput[2];
            tx.Outputs = new TXOutput[2];
            tx.Signatures = new Models.Triptych[2];

            for (int i = 0; i < 2; i++)
            {
                tx.Inputs[i] = GenerateMockTXInput();
                tx.Outputs[i] = GenerateMockTXOutput();
                tx.Signatures[i] = GenerateMockTriptych();
            }

            tx.RangeProof =GenerateMockBulletproof();


            var buffer = new byte[8];
            new Random().NextBytes(buffer);
            tx.Fee = BitConverter.ToUInt64(buffer, 0);

            tx.TransactionKey = Cipher.KeyOps.GeneratePubkey();

            return tx;
        }
    }
}
